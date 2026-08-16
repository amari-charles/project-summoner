using Fateforged.Constants;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Godot;

namespace Fateforged.View.Debug;

/// <summary>
/// Greybox experiment for comparing stationary and WASD-controlled summoners
/// inside the same compact, fixed-camera battle room.
/// </summary>
[GlobalClass]
public partial class CompactRuinSkirmishScene : BattleScene
{
    [Export]
    public bool MovementEnabled { get; set; } = true;

    [Export]
    public float MovementSpeed { get; set; } = 12f;

    [Export]
    public Rect2 PlayerMovementBounds { get; set; } = new(-40f, -22f, 80f, 44f);

    [Export]
    public NodePath MovementTogglePath { get; set; } = "UI/MovementPanel/VBox/MovementToggle";

    [Export]
    public NodePath MovementStatusPath { get; set; } = "UI/MovementPanel/VBox/MovementStatus";

    private CheckButton? _movementToggle;
    private Label? _movementStatus;

    public override async void _Ready()
    {
        ConfigurePrototypeBattle();
        base._Ready();

        _movementToggle = GetNodeOrNull<CheckButton>(MovementTogglePath);
        _movementStatus = GetNodeOrNull<Label>(MovementStatusPath);
        if (_movementToggle != null)
        {
            _movementToggle.ButtonPressed = MovementEnabled;
            _movementToggle.Toggled += OnMovementToggled;
        }

        RefreshMovementStatus();

        await ToSignal(this, SignalName.InitializationComplete);
        ConfigureCompactEnemySpawnBounds();
    }

    public override void _ExitTree()
    {
        if (_movementToggle != null)
            _movementToggle.Toggled -= OnMovementToggled;
        base._ExitTree();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!MovementEnabled || CurrentState != GameState.Playing)
            return;

        var simNode = GetSimulationNode();
        if (simNode == null)
            return;

        var state = simNode.GetState();
        if (state.Phase == GamePhase.GameOver)
            return;

        var input = Godot.Input.GetVector(
            "move_left",
            "move_right",
            "move_up",
            "move_down"
        );
        if (input.IsZeroApprox())
            return;

        var current = state.Summoners[0].Position;
        var target = CalculateBoundedTarget(
            current,
            input,
            MovementSpeed,
            (float)delta,
            PlayerMovementBounds
        );
        simNode.SubmitCommand(new MoveSummonerCommand(0, target));
    }

    public void OnMovementToggled(bool enabled)
    {
        MovementEnabled = enabled;
        RefreshMovementStatus();
    }

    public static SimVector3 CalculateBoundedTarget(
        SimVector3 current,
        Vector2 input,
        float speed,
        float delta,
        Rect2 bounds
    )
    {
        var normalized = input.LengthSquared() > 1f ? input.Normalized() : input;
        float targetX = current.X + normalized.X * speed * delta;
        // Godot's input vector uses negative Y for "up"; positive world Z is
        // visually away from the camera in the battle view.
        float targetZ = current.Z - normalized.Y * speed * delta;
        float maxX = bounds.Position.X + bounds.Size.X;
        float maxZ = bounds.Position.Y + bounds.Size.Y;

        return new SimVector3(
            Mathf.Clamp(targetX, bounds.Position.X, maxX),
            current.Y,
            Mathf.Clamp(targetZ, bounds.Position.Y, maxZ)
        );
    }

    private void RefreshMovementStatus()
    {
        if (_movementStatus == null)
            return;

        _movementStatus.Text = MovementEnabled
            ? "WASD moves the summoner"
            : "Summoner remains stationary";
    }

    private void ConfigurePrototypeBattle()
    {
        var battleContext = GetNodeOrNull("/root/BattleContext");
        if (battleContext == null)
            return;

        battleContext.Call("configure_practice_battle", BuildPrototypeConfig());
    }

    private void ConfigureCompactEnemySpawnBounds()
    {
        var simNode = GetSimulationNode();
        var enemyAi = simNode?.GetState().Summoners[1].Ai;
        if (enemyAi == null)
            return;

        enemyAi.SpawnMinX = 4f;
        enemyAi.SpawnMaxX = 39f;
        enemyAi.SpawnMinZ = -20f;
        enemyAi.SpawnMaxZ = 20f;
    }

    private static Godot.Collections.Dictionary BuildPrototypeConfig()
    {
        return new Godot.Collections.Dictionary
        {
            ["prep_duration"] = 5f,
            ["summon_placement_mode"] = "card_range_from_summoner",
            ["summon_placement_bounds"] = new Godot.Collections.Dictionary
            {
                ["min_x"] = -42f,
                ["max_x"] = 42f,
                ["min_z"] = -24f,
                ["max_z"] = 24f,
            },
            ["player_side"] = BuildSide(
                team: 0,
                id: "ruin_student",
                displayName: "Academy Student",
                controllerKind: "player",
                aiType: "none",
                cards: new Godot.Collections.Array
                {
                    DeckEntry("pebbloom", 2),
                    DeckEntry("water_frog", 2),
                    DeckEntry("wind_diver", 1),
                    DeckEntry("earth_rock_thrower", 1),
                    DeckEntry("mana_bolt", 2),
                    DeckEntry("tail_wind", 1),
                    DeckEntry("healing_field", 1),
                    DeckEntry("stone_spike", 2),
                },
                mana: 1_000_000f
            ),
            ["enemy_side"] = BuildSide(
                team: 1,
                id: "ruin_defense_core",
                displayName: "Ruin Defense Core",
                controllerKind: "trainer_ai",
                aiType: "heuristic",
                cards: new Godot.Collections.Array
                {
                    DeckEntry("fire_wisp", 2),
                    DeckEntry("pebbloom", 2),
                    DeckEntry("fire_wolf", 1),
                    DeckEntry("cinder_caster", 1),
                    DeckEntry("fireball", 2),
                },
                mana: 1_000_000f
            ),
        };
    }

    private static Godot.Collections.Dictionary BuildSide(
        int team,
        string id,
        string displayName,
        string controllerKind,
        string aiType,
        Godot.Collections.Array cards,
        float mana = 12f
    )
    {
        return new Godot.Collections.Dictionary
        {
            ["team"] = team,
            ["source"] = "authored",
            ["summoner"] = new Godot.Collections.Dictionary
            {
                ["source"] = "authored",
                ["id"] = id,
                ["display_name"] = displayName,
                ["hp"] = 120f,
                ["max_hp"] = 120f,
                ["mana"] = mana,
                ["max_mana"] = mana,
                ["cast_speed"] = 1f,
            },
            ["deck"] = new Godot.Collections.Dictionary
            {
                ["source"] = "authored",
                ["cards"] = cards,
            },
            ["controller"] = new Godot.Collections.Dictionary
            {
                ["kind"] = controllerKind,
                ["ai_type"] = aiType,
                ["ai_config"] = new Godot.Collections.Dictionary
                {
                    ["play_interval_min"] = 2.5f,
                    ["play_interval_max"] = 4f,
                },
            },
        };
    }

    private static Godot.Collections.Dictionary DeckEntry(string catalogId, int count) =>
        new() { ["catalog_id"] = catalogId, ["count"] = count };

    private SimulationNode? GetSimulationNode() =>
        GetTree().GetFirstNodeInGroup(GroupIDs.SimulationNode) as SimulationNode;
}
