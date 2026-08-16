using Fateforged.Cards;
using Fateforged.Constants;
using Fateforged.Simulation;
using Fateforged.View.Debug;
using Fateforged.View.Spells;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Fateforged.Input;

/// <summary>
/// Full-screen transparent Control that captures DnD drops from HandUI.
/// Validates drops (mana, summoner state), manages previews (SummonPreview,
/// SpellPreview, SpawnZoneOverlay), and submits commands via SimulationNode.
///
/// Replaces BattlefieldDropZone (GDScript).
/// </summary>
[GlobalClass]
public partial class InputCollector : Control
{
    // Team constants (mirrors UnitConstants.Team from GDScript)
    private const int TeamPlayer = 0;
    private const int TeamEnemy = 1;

    // Raycast / positioning constants
    private const float RaycastMaxDistance = 1000f;
    private const float DefaultSpellRadius = 5.0f;
    private const float DefaultDebugFormationSpacing = 2.0f;
    private const int DefaultDebugBurstCount = 3;
    private const string DebugSpawnModeSingle = "single";
    private const string DebugSpawnModeBurst = "burst";
    private const string DebugSpawnModePaint = "paint";
    private const string DebugFormationStack = "stack";
    private const string DebugFormationLine = "line";
    private const string DebugFormationArc = "arc";
    private const string DebugFormationRandom = "random";
    private const int NoTargetId = -1;

    // =========================================================================
    // STATE
    // =========================================================================

    private Node? _playerSummoner;
    private Camera3D? _camera3D;

    // Preview state
    private SummonPreview? _spawnPreview;
    private Node3D? _spellPreview; // GDScript SpellPreview — managed via .Call() interop
    private Node3D? _spawnZoneOverlay; // GDScript SpawnZoneOverlay
    private Card? _previewCard;
    private int _previewTeam;
    private readonly List<Vector3> _debugPaintPositions = [];

    // Public drag state (read by View layer)
    public int DraggedCardIndex { get; private set; } = -1;
    public Vector3 DragPosition { get; private set; }
    public bool IsDragging => DraggedCardIndex >= 0;

    // =========================================================================
    // INITIALIZATION
    // =========================================================================

    public void Initialize(Node playerSummoner)
    {
        _playerSummoner = playerSummoner;
        _camera3D = GetViewport().GetCamera3D();
        AddToGroup(GroupIDs.InputCollector);
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationDragEnd)
        {
            CleanupSpawnPreview();
            CleanupSpellPreview();
            CleanupSpawnZoneOverlay();
            _debugPaintPositions.Clear();
            DraggedCardIndex = -1;
            DragPosition = Vector3.Zero;
        }
    }

    // =========================================================================
    // DND PROTOCOL
    // =========================================================================

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.Dictionary)
        {
            CleanupSpawnPreview();
            return false;
        }

        var dict = data.AsGodotDictionary();

        // Handle debug spawn from UnitSpawnerPanel
        if (DictGetString(dict, "type") == "debug_spawn")
            return CanDropDebugSpawn(atPosition, dict);

        // Validate required keys
        if (
            !dict.ContainsKey("card_index")
            || !dict.ContainsKey("card")
            || !dict.ContainsKey("source")
        )
        {
            CleanupSpawnPreview();
            return false;
        }

        // Validate source
        if (DictGetString(dict, "source") != "hand")
        {
            CleanupSpawnPreview();
            return false;
        }

        // Validate summoner
        if (_playerSummoner == null || !IsInstanceValid(_playerSummoner))
        {
            CleanupSpawnPreview();
            return false;
        }

        var isEnabledVar = _playerSummoner.Get("IsEnabled");
        if (isEnabledVar.VariantType != Variant.Type.Bool || !(bool)isEnabledVar)
        {
            CleanupSpawnPreview();
            return false;
        }

        // Validate card index
        var cardIndexVar = dict["card_index"];
        if (cardIndexVar.VariantType != Variant.Type.Int)
        {
            CleanupSpawnPreview();
            return false;
        }
        int cardIndex = (int)cardIndexVar;

        var handVar = _playerSummoner.Get("Hand");
        var hand = handVar.AsGodotArray();
        if (cardIndex < 0 || cardIndex >= hand.Count)
        {
            CleanupSpawnPreview();
            return false;
        }

        // Validate card object
        if (dict["card"].AsGodotObject() is not Card card)
        {
            CleanupSpawnPreview();
            return false;
        }

        // Mana check
        var manaVar = _playerSummoner.Get("Mana");
        float mana = manaVar.VariantType == Variant.Type.Float ? (float)manaVar : 0f;
        if (mana < card.ManaCost)
        {
            CleanupSpawnPreview();
            return false;
        }

        // Update drag state
        DraggedCardIndex = cardIndex;

        // Preview based on card type
        var worldPos = ScreenToWorld3D(atPosition);
        DragPosition = worldPos;

        if (card.Type == (int)CardType.Summon)
        {
            CleanupSpellPreview();
            var previewPos = UsesCardRangePlacement()
                ? ResolvePlayerSummonPosition(worldPos, card)
                : ClampSpawnPosition(worldPos, TeamPlayer);
            UpdateSpawnPreview(previewPos, card, isValidZone: true);
            ShowSpawnZoneOverlay(card);
        }
        else if (card.Type == (int)CardType.Spell)
        {
            CleanupSpawnPreview();
            CleanupSpawnZoneOverlay();
            var previewPos = IsAutoTargetSpell(card)
                ? GetAutoTargetSpellPosition(worldPos)
                : worldPos;
            UpdateSpellPreview(previewPos, card);
        }

        return true;
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.Dictionary)
            return;

        var dict = data.AsGodotDictionary();

        // Handle debug spawn
        if (DictGetString(dict, "type") == "debug_spawn")
        {
            DropDebugSpawn(atPosition, dict);
            return;
        }

        var cardIndexVar = dict["card_index"];
        if (cardIndexVar.VariantType != Variant.Type.Int)
            return;
        int cardIndex = (int)cardIndexVar;

        if (dict["card"].AsGodotObject() is not Card card)
            return;

        // Submit via SimulationNode.QueuePlayCard (handles team remap + coordinate conversion)
        var worldPos = ScreenToWorld3D(atPosition);

        // Standard battles preserve half-map clamping. Card-range battles snap
        // out-of-range aiming to the closest point on the card's radius.
        if (card.Type == (int)CardType.Summon)
        {
            if (UsesCardRangePlacement())
                worldPos = ResolvePlayerSummonPosition(worldPos, card);
            else
                worldPos = ClampSpawnPosition(worldPos, TeamPlayer);
        }
        else if (card.Type == (int)CardType.Spell && IsAutoTargetSpell(card))
            worldPos = GetAutoTargetSpellPosition(worldPos);

        var sim = GetSimNode();
        sim?.QueuePlayCard(0, cardIndex, worldPos, NoTargetId);
    }

    // =========================================================================
    // SCREEN → WORLD CONVERSION
    // =========================================================================

    private Vector3 ScreenToWorld3D(Vector2 screenPos)
    {
        if (_camera3D == null || !IsInstanceValid(_camera3D))
        {
            // Re-acquire camera (may have been set after init)
            _camera3D = GetViewport().GetCamera3D();
            if (_camera3D == null)
                return Vector3.Zero;
        }

        var from = _camera3D.ProjectRayOrigin(screenPos);
        var to = from + _camera3D.ProjectRayNormal(screenPos) * RaycastMaxDistance;

        const float SpawnPlaneHeight = 0f;
        float spawnY = SpawnPlaneHeight;
        float t = (spawnY - from.Y) / (to.Y - from.Y);
        if (t < 0 || t > 1)
            return Vector3.Zero;

        return from + (to - from) * t;
    }

    // =========================================================================
    // SPAWN ZONE HELPERS (mirror BattlefieldConstants GDScript)
    // =========================================================================

    private static bool IsValidSpawnPosition(Vector3 pos, int team)
    {
        return BattlefieldBounds.IsInBounds(pos)
            && BattlefieldBounds.IsValidSpawnPositionForTeam(pos, team);
    }

    private static Vector3 ClampSpawnPosition(Vector3 pos, int team)
    {
        return BattlefieldBounds.ClampToValidSpawnZone(pos, team);
    }

    private bool UsesCardRangePlacement()
    {
        var sim = GetSimNode();
        return sim?.GetState().SummonPlacementMode == SummonPlacementMode.CardRangeFromSummoner;
    }

    private Vector3 ResolvePlayerSummonPosition(Vector3 position, Card card)
    {
        if (_playerSummoner is not Node3D summoner)
            return position;

        var state = GetSimNode()?.GetState();
        if (state == null)
            return position;

        var clampedToRadius = SummonPlacementRules.ClampToCardRange(
            ToSimVector(summoner.GlobalPosition),
            ToSimVector(position),
            card.SummonRange
        );
        var resolved = SummonPlacementRules.ClampToBattlefield(state, clampedToRadius);
        return new Vector3(resolved.X, resolved.Y, resolved.Z);
    }

    private static SimVector3 ToSimVector(Vector3 position) =>
        new(position.X, position.Y, position.Z);

    // =========================================================================
    // SUMMON PREVIEW
    // =========================================================================

    private void UpdateSpawnPreview(Vector3 worldPos, Card card, bool isValidZone, int team = 0)
    {
        if (worldPos == Vector3.Zero)
        {
            CleanupSpawnPreview();
            return;
        }

        // Recreate if card or team changed
        if (
            _spawnPreview == null
            || !IsInstanceValid(_spawnPreview)
            || _previewCard != card
            || _previewTeam != team
        )
        {
            CleanupSpawnPreview();
            CreateSpawnPreview(card, team);
            _previewTeam = team;
        }

        if (_spawnPreview == null)
            return;

        var positions = CalculateSafeSpawnPositions(worldPos, card, team);
        _spawnPreview.UpdatePositions(positions);
        _spawnPreview.SetValid(isValidZone);
    }

    private void CreateSpawnPreview(Card card, int team)
    {
        if (card.SpawnCount <= 0)
            return;

        var catalogData = CardCatalog.GetCardAsDict(card.CatalogId);
        var scenePath = DictGetString(catalogData, "unit_scene_path");
        if (string.IsNullOrEmpty(scenePath))
            return;

        var unitScene = GD.Load<PackedScene>(scenePath);
        if (unitScene == null)
            return;

        _spawnPreview = new SummonPreview();
        _previewCard = card;

        var root3D = Find3DRoot();
        if (root3D != null)
        {
            root3D.AddChild(_spawnPreview);
            _spawnPreview.Initialize(unitScene, card.SpawnCount, team, card.CatalogId);
        }
    }

    private Godot.Collections.Array<Vector3> CalculateSafeSpawnPositions(
        Vector3 centerPos,
        Card card,
        int team
    )
    {
        if (UsesCardRangePlacement())
        {
            var rangedPositions = new Godot.Collections.Array<Vector3>();
            for (int i = 0; i < card.SpawnCount; i++)
                rangedPositions.Add(centerPos + card.GetFormationOffset(i));
            return rangedPositions;
        }

        var battlefield = GetNodeOrNull("/root/Main/Battlefield");
        battlefield ??= GetTree().CurrentScene;

        var result = card.GetSafeSpawnPositions(centerPos, battlefield, 0.5f, team);
        if (result.Count > 0)
            return result;

        // Fallback: formation offsets
        var positions = new Godot.Collections.Array<Vector3>();
        for (int i = 0; i < card.SpawnCount; i++)
            positions.Add(centerPos + card.GetFormationOffset(i));
        return positions;
    }

    private void CleanupSpawnPreview()
    {
        if (_spawnPreview != null && IsInstanceValid(_spawnPreview))
            _spawnPreview.Cleanup();
        _spawnPreview = null;
        _previewCard = null;
        _previewTeam = 0;
    }

    // =========================================================================
    // SPELL PREVIEW (GDScript interop)
    // =========================================================================

    private void UpdateSpellPreview(Vector3 worldPos, Card card)
    {
        if (worldPos == Vector3.Zero)
        {
            CleanupSpellPreview();
            return;
        }

        if (_spellPreview == null || !IsInstanceValid(_spellPreview) || _previewCard != card)
        {
            CleanupSpellPreview();
            CreateSpellPreview(card);
        }

        if (_spellPreview == null)
            return;

        _spellPreview.Call("update_position", worldPos);
        _spellPreview.Call("update_points", GetSpellSourcePosition(), worldPos);
        _spellPreview.Call("set_valid", true);
    }

    private void CreateSpellPreview(Card card)
    {
        var script = GD.Load<Script>("res://scripts/battle/ui/spell_preview.gd");
        if (script == null)
            return;

        // SpellPreview extends Node3D — create via script attachment
        var preview = new Node3D();
        preview.SetScript(script);
        _previewCard = card;

        var root3D = Find3DRoot();
        if (root3D != null)
        {
            root3D.AddChild(preview);
            var def = CardCatalog.GetCard(card.CatalogId);
            var metadata =
                def != null
                    ? SpellVisualMetadata.FromCardDefinition(def)
                    : new SpellVisualMetadata(
                        SpellVisualMetadata.Circle,
                        card.SpellRadius > 0 ? card.SpellRadius : DefaultSpellRadius,
                        SpellAreaLineWidth.FullWidth,
                        "neutral"
                    );
            preview.Call("setup", metadata.Radius, metadata.Shape, metadata.LineWidth, metadata.Element);
            _spellPreview = preview;
        }
        else
        {
            preview.Free();
        }
    }

    private void CleanupSpellPreview()
    {
        if (_spellPreview != null && IsInstanceValid(_spellPreview))
            _spellPreview.Call("cleanup");
        _spellPreview = null;
    }

    // =========================================================================
    // SPAWN ZONE OVERLAY (GDScript interop)
    // =========================================================================

    private void ShowSpawnZoneOverlay(Card card)
    {
        if (BattlefieldBounds.IsDebugBypassSpawnBoundaryEnabled())
        {
            CleanupSpawnZoneOverlay();
            return;
        }

        if (_spawnZoneOverlay != null && IsInstanceValid(_spawnZoneOverlay))
        {
            ConfigureSpawnZoneOverlay(card);
            return;
        }

        var script = GD.Load<Script>("res://scripts/battle/ui/spawn_zone_overlay.gd");
        if (script == null)
            return;

        var overlay = new Node3D();
        overlay.SetScript(script);

        var root3D = Find3DRoot();
        if (root3D != null)
        {
            root3D.AddChild(overlay);
            _spawnZoneOverlay = overlay;
            ConfigureSpawnZoneOverlay(card);
        }
        else
        {
            overlay.Free();
        }
    }

    private void ConfigureSpawnZoneOverlay(Card card)
    {
        if (_spawnZoneOverlay == null || !IsInstanceValid(_spawnZoneOverlay))
            return;

        if (UsesCardRangePlacement() && _playerSummoner is Node3D summoner)
        {
            _spawnZoneOverlay.Call(
                "show_card_range",
                summoner.GlobalPosition,
                card.SummonRange
            );
        }
        else
        {
            _spawnZoneOverlay.Call("show_team_half");
        }
    }

    private void CleanupSpawnZoneOverlay()
    {
        if (_spawnZoneOverlay != null && IsInstanceValid(_spawnZoneOverlay))
            _spawnZoneOverlay.Call("cleanup");
        _spawnZoneOverlay = null;
    }

    // =========================================================================
    // DEBUG SPAWN
    // =========================================================================

    private bool CanDropDebugSpawn(Vector2 atPosition, Godot.Collections.Dictionary data)
    {
        var arena = FindDebugArenaController();
        if (arena == null)
            return false;

        if (!data.ContainsKey("card") || data["card"].AsGodotObject() is not Card card)
            return false;

        var worldPos = ScreenToWorld3D(atPosition);
        if (worldPos != Vector3.Zero)
        {
            int team = DictGetInt(data, "team", 1);
            bool isValidZone = IsValidSpawnPosition(worldPos, team);
            var clampedPos = ClampSpawnPosition(worldPos, team);
            UpdateSpawnPreview(clampedPos, card, isValidZone, team);

            string spawnMode = DictGetString(data, "spawn_mode", DebugSpawnModeSingle);
            float formationSpacing = DictGetFloat(
                data,
                "formation_spacing",
                DefaultDebugFormationSpacing
            );
            if (spawnMode == DebugSpawnModePaint)
                RegisterPaintPoint(clampedPos, formationSpacing);
        }

        return true;
    }

    private async void DropDebugSpawn(Vector2 atPosition, Godot.Collections.Dictionary data)
    {
        CleanupSpawnPreview();
        CleanupSpawnZoneOverlay();

        if (!data.ContainsKey("card") || data["card"].AsGodotObject() is not Card card)
        {
            GD.PushError("InputCollector: No card in debug spawn data");
            return;
        }

        int team = DictGetInt(data, "team", 1);
        var worldPos = ScreenToWorld3D(atPosition);
        if (worldPos == Vector3.Zero)
        {
            GD.PushError("InputCollector: Failed to convert screen position to world");
            return;
        }

        int unitTeam = team == TeamPlayer ? TeamPlayer : TeamEnemy;
        worldPos = ClampSpawnPosition(worldPos, unitTeam);
        var arena = FindDebugArenaController();
        var simNode = SimulationNode.Current;
        var preSpawnUnitIds = CaptureCurrentUnitIds(simNode);
        string spawnMode = DictGetString(data, "spawn_mode", DebugSpawnModeSingle);
        int burstCount = Mathf.Max(1, DictGetInt(data, "burst_count", DefaultDebugBurstCount));
        string formationMode = DictGetString(data, "formation_mode", DebugFormationStack);
        float formationSpacing = Mathf.Max(
            0.1f,
            DictGetFloat(data, "formation_spacing", DefaultDebugFormationSpacing)
        );

        var spawnPositions = BuildDebugSpawnPositions(
            worldPos,
            unitTeam,
            spawnMode,
            burstCount,
            formationMode,
            formationSpacing
        );
        if (spawnPositions.Count == 0)
            spawnPositions.Add(worldPos);

        foreach (var spawnPosition in spawnPositions)
        {
            if (card.Type == (int)CardType.Spell)
                card.CastAt(spawnPosition, unitTeam);
            else
                card.SpawnAt(spawnPosition, unitTeam);
        }

        var spawnedUnitIds = card.Type == (int)CardType.Spell
            ? []
            : CaptureSpawnedUnitIds(simNode, preSpawnUnitIds, unitTeam);
        int expectedUnitCount = card.Type == (int)CardType.Spell
            ? 0
            : (
                spawnedUnitIds.Count > 0
                    ? spawnedUnitIds.Count
                    : card.SpawnCount * spawnPositions.Count
            );
        arena?.RegisterDebugSpawnBatch(unitTeam, expectedUnitCount, card.CardName, spawnedUnitIds.ToArray());
        _debugPaintPositions.Clear();

        // Activate newly spawned units immediately (debug mode bypasses prep phase)
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        ActivateRecentSpawns();
    }

    private Godot.Collections.Array<Vector3> BuildDebugSpawnPositions(
        Vector3 center,
        int team,
        string spawnMode,
        int burstCount,
        string formationMode,
        float spacing
    )
    {
        var result = new Godot.Collections.Array<Vector3>();

        if (spawnMode == DebugSpawnModePaint && _debugPaintPositions.Count > 0)
        {
            foreach (var pos in _debugPaintPositions)
                result.Add(ClampSpawnPosition(pos, team));
            return result;
        }

        int count = spawnMode == DebugSpawnModeBurst ? Mathf.Max(1, burstCount) : 1;
        if (count == 1)
        {
            result.Add(center);
            return result;
        }

        float half = (count - 1) * 0.5f;
        float forwardSign = team == TeamPlayer ? 1f : -1f;
        float radius = Mathf.Max(spacing, 0.5f);
        var rng = new RandomNumberGenerator();
        rng.Seed = (ulong)Time.GetTicksMsec();

        for (int i = 0; i < count; i++)
        {
            Vector3 offset = Vector3.Zero;
            float t = count > 1 ? (i - half) / half : 0f;
            switch (formationMode)
            {
                case DebugFormationLine:
                    offset = new Vector3(0f, 0f, (i - half) * spacing);
                    break;
                case DebugFormationArc:
                {
                    float angle = Mathf.DegToRad(Mathf.Lerp(-70f, 70f, (t + 1f) * 0.5f));
                    float x = Mathf.Cos(angle) * radius * forwardSign;
                    float z = Mathf.Sin(angle) * radius;
                    offset = new Vector3(x, 0f, z);
                    break;
                }
                case DebugFormationRandom:
                {
                    float randomRadius = rng.RandfRange(0.2f * radius, radius);
                    float randomAngle = rng.RandfRange(0f, Mathf.Tau);
                    offset = new Vector3(
                        Mathf.Cos(randomAngle) * randomRadius,
                        0f,
                        Mathf.Sin(randomAngle) * randomRadius
                    );
                    break;
                }
                case DebugFormationStack:
                default:
                    offset = Vector3.Zero;
                    break;
            }

            var pos = center + offset;
            result.Add(ClampSpawnPosition(pos, team));
        }

        return result;
    }

    private void RegisterPaintPoint(Vector3 position, float spacing)
    {
        float minSpacing = Mathf.Max(spacing * 0.45f, 0.2f);
        if (_debugPaintPositions.Count == 0)
        {
            _debugPaintPositions.Add(position);
            return;
        }

        Vector3 last = _debugPaintPositions[^1];
        if ((position - last).Length() < minSpacing)
            return;
        _debugPaintPositions.Add(position);
    }

    private static HashSet<int> CaptureCurrentUnitIds(SimulationNode? simNode)
    {
        if (simNode == null)
            return [];
        return simNode.GetState().Units.Keys.ToHashSet();
    }

    private static List<int> CaptureSpawnedUnitIds(
        SimulationNode? simNode,
        HashSet<int> preSpawnUnitIds,
        int unitTeam
    )
    {
        if (simNode == null)
            return [];

        return simNode
            .GetState()
            .Units.Values.Where(unit => (int)unit.Team == unitTeam && !preSpawnUnitIds.Contains(unit.UnitId))
            .Select(unit => unit.UnitId)
            .ToList();
    }

    private DebugArenaScene? FindDebugArenaController()
    {
        var controllers = GetTree().GetNodesInGroup(GroupIDs.GameController);
        foreach (var controller in controllers)
        {
            if (controller is DebugArenaScene arena)
                return arena;
        }
        return null;
    }

    private void ActivateRecentSpawns()
    {
        var units = GetTree().GetNodesInGroup(GroupIDs.Units);
        foreach (var unit in units)
        {
            if (unit.HasMethod("IsActive") && !(bool)unit.Call("IsActive"))
                unit.Call("Activate");
        }
    }

    // =========================================================================
    // UTILITY
    // =========================================================================

    private Node3D? Find3DRoot()
    {
        var root = GetTree().CurrentScene;
        if (root == null)
            return null;

        // Look for battlefield
        var battlefield = root.FindChild("Battlefield3D", true, false);
        if (battlefield is Node3D bf3d)
            return bf3d;

        var battlefieldMatch = root.FindChild("Battlefield*", true, false);
        if (battlefieldMatch is Node3D bfMatch)
            return bfMatch;

        if (root is Node3D root3D)
            return root3D;

        foreach (var child in root.GetChildren())
        {
            if (child is Node3D child3D)
                return child3D;
        }

        return null;
    }

    private static SimulationNode? GetSimNode() => SimulationNode.Current;

    private static bool IsAutoTargetSpell(Card card)
    {
        var def = CardCatalog.GetCard(card.CatalogId);
        return def != null
            && def.Type == CardType.Spell
            && def.SpellTargeting == SpellTargeting.SingleTarget;
    }

    private Vector3 GetAutoTargetSpellPosition(Vector3 fallback)
    {
        if (_playerSummoner is Node3D summonerNode && IsInstanceValid(summonerNode))
            return summonerNode.GlobalPosition;

        return fallback;
    }

    private Vector3 GetSpellSourcePosition()
    {
        if (_playerSummoner is Node3D summonerNode && IsInstanceValid(summonerNode))
            return summonerNode.GlobalPosition;

        return Vector3.Zero;
    }

    /// <summary>
    /// Safely get a string value from a Godot Dictionary.
    /// </summary>
    private static string DictGetString(
        Godot.Collections.Dictionary dict,
        string key,
        string defaultValue = ""
    )
    {
        if (!dict.ContainsKey(key))
            return defaultValue;
        return dict[key].ToString() ?? defaultValue;
    }

    /// <summary>
    /// Safely get an int value from a Godot Dictionary.
    /// </summary>
    private static int DictGetInt(
        Godot.Collections.Dictionary dict,
        string key,
        int defaultValue = 0
    )
    {
        if (!dict.ContainsKey(key))
            return defaultValue;
        var v = dict[key];
        return v.VariantType == Variant.Type.Int ? (int)v : defaultValue;
    }

    /// <summary>
    /// Safely get a float value from a Godot Dictionary.
    /// </summary>
    private static float DictGetFloat(
        Godot.Collections.Dictionary dict,
        string key,
        float defaultValue = 0f
    )
    {
        if (!dict.ContainsKey(key))
            return defaultValue;
        var v = dict[key];
        return v.VariantType switch
        {
            Variant.Type.Float => (float)v,
            Variant.Type.Int => (int)v,
            _ => defaultValue,
        };
    }
}
