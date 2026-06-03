namespace Fateforged.Tests.View;

using System;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Constants;
using Fateforged.Session;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Effects;
using Fateforged.Simulation.Enums;
using Fateforged.View;
using Fateforged.View.Spells;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class SpellVisualReadabilityTest
{
    private readonly List<Node> _createdNodes = [];

    [AfterTest]
    public void Cleanup()
    {
        for (int i = _createdNodes.Count - 1; i >= 0; i--)
        {
            var node = _createdNodes[i];
            if (!GodotObject.IsInstanceValid(node))
                continue;

            node.GetParent()?.RemoveChild(node);
            node.Free();
        }

        _createdNodes.Clear();
    }

    [TestCase]
    public void ActiveElementalSpells_HaveImplementedVfxResources()
    {
        var elements = new[] { Element.Fire, Element.Water, Element.Earth, Element.Wind };
        var spells = CardCatalog
            .GetAllCards()
            .Where(card =>
                card.Type == CardType.Spell
                && elements.Contains(card.ElementalAffinity)
                && (card.Flags & (CardFlags.Archived | CardFlags.DevOnly)) == 0
            )
            .ToArray();

        AssertThat(spells.Length).IsGreater(0);
        foreach (var spell in spells)
        {
            AssertThat(spell.SpellVfx.HasValue).OverrideFailureMessage(
                $"{spell.Id} has no SpellVfx"
            ).IsTrue();
            AssertThat(ResourceLoader.Exists($"res://resources/vfx/{spell.SpellVfx}.tres"))
                .OverrideFailureMessage($"{spell.Id} references missing VFX {spell.SpellVfx}")
                .IsTrue();
            AssertThat(ResourceLoader.Load<Resource>($"res://resources/vfx/{spell.SpellVfx}.tres"))
                .OverrideFailureMessage($"{spell.Id} VFX resource did not load: {spell.SpellVfx}")
                .IsNotNull();
        }
    }

    [TestCase]
    public void SpellVisualMetadata_DerivesPreviewShapesFromGameplayDefinitions()
    {
        var fireball = SpellVisualMetadata.FromCardDefinition(CardDefinitions.Fireball);
        AssertThat(fireball.Shape).IsEqual(SpellVisualMetadata.Circle);
        AssertThat(fireball.Radius).IsEqual(10f);

        var tailWind = SpellVisualMetadata.FromCardDefinition(CardDefinitions.TailWind);
        AssertThat(tailWind.Shape).IsEqual(SpellVisualMetadata.Square);
        AssertThat(tailWind.Radius).IsEqual(6f);

        var windShear = SpellVisualMetadata.FromCardDefinition(CardDefinitions.WindShear);
        AssertThat(windShear.Shape).IsEqual(SpellVisualMetadata.Line);
        AssertThat(windShear.Radius).IsEqual(10f);
        AssertThat(windShear.LineWidth).IsEqual(2.5f);

        var stoneSpike = SpellVisualMetadata.FromCardDefinition(CardDefinitions.StoneSpike);
        AssertThat(stoneSpike.Shape).IsEqual(SpellVisualMetadata.SingleTarget);
        AssertThat(stoneSpike.Radius).IsEqual(0f);
    }

    [TestCase]
    public void EntityManager_SpellCastPassesReadableVfxMetadata()
    {
        var state = NewState();
        var service = new CapturingVfxService();
        var manager = CreateManager(state, service);

        manager.Visit(
            new SpellCastEvent(
                0,
                (string)CardIds.TailWind,
                new SimVector3(2f, 0f, 3f)
            )
        );

        AssertThat(service.EffectId).IsEqual("spell_area_field");
        AssertThat(service.Position).IsEqual(new Vector3(2f, 0f, 3f));
        AssertThat(StringValue(service.Data, "card_id")).IsEqual("tail_wind");
        AssertThat(StringValue(service.Data, "element")).IsEqual("wind");
        AssertThat(StringValue(service.Data, "shape")).IsEqual("square");
        AssertThat(FloatValue(service.Data, "radius")).IsEqual(6f);
        AssertThat(FloatValue(service.Data, "duration")).IsEqual(4f);
        AssertThat(FloatValue(service.Data, "line_width")).IsEqual(2.5f);
    }

    [TestCase]
    public void EntityManager_LineSpellPassesSourceAndTargetPositions()
    {
        var state = NewState();
        var service = new CapturingVfxService();
        var manager = CreateManager(state, service);

        manager.Visit(
            new SpellCastEvent(
                0,
                (string)CardIds.WindShear,
                new SimVector3(10f, 0f, 1f)
            )
        );

        AssertThat(service.EffectId).IsEqual("spell_line");
        AssertThat(service.Position).IsEqual(new Vector3(-20f, 0f, 0f));
        AssertThat(StringValue(service.Data, "shape")).IsEqual("line");
        AssertThat(FloatValue(service.Data, "radius")).IsEqual(10f);
        AssertThat(VectorValue(service.Data, "source_position")).IsEqual(new Vector3(-20f, 0f, 0f));
        AssertThat(VectorValue(service.Data, "target_position")).IsEqual(new Vector3(10f, 0f, 1f));
    }

    [TestCase]
    public void EntityManager_DelayedEffectFiredPassesPulseMetadata()
    {
        var state = NewState();
        var service = new CapturingVfxService();
        var manager = CreateManager(state, service);

        manager.Visit(
            new DelayedEffectFiredEvent(
                new SimVector3(4f, 0f, 5f),
                EffectType.Damage,
                6f,
                SpellAreaShape.Circle,
                new SimVector3(-20f, 0f, 0f),
                (string)CardIds.Overheat
            )
        );

        AssertThat(service.EffectId).IsEqual("spell_area_pulse");
        AssertThat(service.Position).IsEqual(new Vector3(4f, 0f, 5f));
        AssertThat(StringValue(service.Data, "mode")).IsEqual("pulse");
        AssertThat(StringValue(service.Data, "element")).IsEqual("fire");
        AssertThat(FloatValue(service.Data, "radius")).IsEqual(6f);
    }

    [TestCase]
    public void EntityManager_RemovedCueWithRemovalPayloadPassesPulseMetadata()
    {
        var state = NewState();
        var service = new CapturingVfxService();
        var manager = CreateManager(state, service);

        manager.Visit(
            new EffectCueEvent(
                "ignition_mark:StatModifier",
                EffectCuePhase.Removed,
                EffectType.StatModifier,
                -1,
                12,
                new SimVector3(7f, 0f, 2f)
            )
        );

        AssertThat(service.EffectId).IsEqual("spell_area_pulse");
        AssertThat(service.Position).IsEqual(new Vector3(7f, 0f, 2f));
        AssertThat(StringValue(service.Data, "card_id")).IsEqual("ignition_mark");
        AssertThat(StringValue(service.Data, "mode")).IsEqual("pulse");
        AssertThat(FloatValue(service.Data, "radius")).IsEqual(4.5f);
    }

    private EntityManager CreateManager(MatchState state, CapturingVfxService service)
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var manager = new EntityManager { Name = $"SpellVisualEntityManager_{_createdNodes.Count}" };
        tree.Root.AddChild(manager);
        _createdNodes.Add(manager);
        manager.Initialize(new StubSession(state), service);
        return manager;
    }

    private static MatchState NewState()
    {
        var state = new MatchState();
        state.Summoners[0].Position = new SimVector3(-20f, 0f, 0f);
        state.Summoners[1].Position = new SimVector3(20f, 0f, 0f);
        return state;
    }

    private static string StringValue(Godot.Collections.Dictionary data, string key) =>
        data[key].ToString() ?? "";

    private static float FloatValue(Godot.Collections.Dictionary data, string key) =>
        data[key].AsSingle();

    private static Vector3 VectorValue(Godot.Collections.Dictionary data, string key) =>
        data[key].AsVector3();

    private sealed class CapturingVfxService : IBattleVfxService
    {
        public string EffectId { get; private set; } = "";
        public Vector3 Position { get; private set; }
        public Godot.Collections.Dictionary Data { get; private set; } = new();

        public void PlayEffect(
            string effectId,
            Vector3 position,
            Godot.Collections.Dictionary? data = null
        )
        {
            EffectId = effectId;
            Position = position;
            Data = data ?? new Godot.Collections.Dictionary();
        }
    }

    private sealed class StubSession : IGameSession
    {
        private readonly MatchState _state;

        public StubSession(MatchState state)
        {
            _state = state;
        }

        public event Action<IReadOnlyList<SimEvent>>? SimEventsEmitted
        {
            add { }
            remove { }
        }

        public MatchState GetState() => _state;

        public void SubmitCommand(ICommand command) { }

        public void Tick(float delta) { }
    }
}
