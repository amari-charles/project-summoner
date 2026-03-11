namespace Fateforged.Tests.Simulation;

using System.Collections.Generic;
using System.Linq;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Combat.Slots;
using GdUnit4;
using Fateforged.Units;
using static GdUnit4.Assertions;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;

[TestSuite]
public class SimulationIntegrationTest
{
    private const float Delta = 1f / 60f; // Fixed 60fps timestep

    private MatchState _state = null!;
    private Fateforged.Simulation.Simulation _sim = null!;

    [BeforeTest]
    public void Setup()
    {
        _state = SimTestHelper.CreateBattleState();
        _sim = new Fateforged.Simulation.Simulation(_state);
    }

    // =========================================================================
    // Tick Basics
    // =========================================================================

    [TestCase]
    public void Tick_IncrementsFrameNumber()
    {
        AssertThat(_state.FrameNumber).IsEqual(0);

        _sim.Tick(Delta);

        AssertThat(_state.FrameNumber).IsEqual(1);

        _sim.Tick(Delta);

        AssertThat(_state.FrameNumber).IsEqual(2);
    }

    [TestCase]
    public void Tick_BattlePhase_AdvancesMatchTime()
    {
        AssertThat(_state.MatchTime).IsEqual(0f);

        _sim.Tick(Delta);

        AssertThat(_state.MatchTime).IsGreater(0f);
    }

    [TestCase]
    public void Tick_GameOver_ReturnsEmpty()
    {
        _state.Phase = GamePhase.GameOver;

        var events = _sim.Tick(Delta);

        AssertThat(events.Count).IsEqual(0);
        AssertThat(_state.FrameNumber).IsEqual(0); // Frame not incremented
    }

    // =========================================================================
    // Command Processing
    // =========================================================================

    [TestCase]
    public void Tick_PlayCardCommand_DeductsMana()
    {
        var card = SimTestHelper.CreateSummonCard("test_unit", manaCost: 3);
        _state.CardDataMap["test_unit"] = card;
        _state.Summoners[0].Hand = new List<SimCardCatalogId> { "test_unit" };
        _state.Summoners[0].Deck = new List<SimCardCatalogId> { "test_unit", "test_unit" };
        _state.Summoners[0].Mana = 10f;

        var cmd = new PlayCardCommand(0, 0, new SimVector3(-5f, 0f, 0f))
        {
            ExecuteFrame = 1
        };
        _state.PendingCommandBuffer.Add(cmd);

        _sim.Tick(Delta); // Frame 1

        AssertThat(_state.Summoners[0].Mana).IsEqual(7f); // 10 - 3
    }

    [TestCase]
    public void Tick_PlayCardCommand_InsufficientMana_Rejected()
    {
        var card = SimTestHelper.CreateSummonCard("test_unit", manaCost: 15);
        _state.CardDataMap["test_unit"] = card;
        _state.Summoners[0].Hand = new List<SimCardCatalogId> { "test_unit" };
        _state.Summoners[0].Mana = 10f;

        var cmd = new PlayCardCommand(0, 0, new SimVector3(-5f, 0f, 0f))
        {
            ExecuteFrame = 1
        };
        _state.PendingCommandBuffer.Add(cmd);

        _sim.Tick(Delta);

        AssertThat(_state.Summoners[0].Mana).IsEqual(10f); // Unchanged
    }

    [TestCase]
    public void Tick_PlayCardCommand_SpawnsUnits()
    {
        var card = SimTestHelper.CreateSummonCard("test_unit", manaCost: 3, unitCount: 2);
        _state.CardDataMap["test_unit"] = card;
        _state.Summoners[0].Hand = new List<SimCardCatalogId> { "test_unit" };
        _state.Summoners[0].Deck = new List<SimCardCatalogId> { "test_unit", "test_unit" };

        var cmd = new PlayCardCommand(0, 0, new SimVector3(-5f, 0f, 0f))
        {
            ExecuteFrame = 1
        };
        _state.PendingCommandBuffer.Add(cmd);

        var events = _sim.Tick(Delta);

        // Two units spawned
        int registrations = SimTestHelper.CountEvents<UnitRegisteredEvent>(events);
        AssertThat(registrations).IsEqual(2);
        AssertThat(_state.Units.Count).IsEqual(2);
    }

    [TestCase]
    public void Tick_PlayCardCommand_SpawnCountReduction_RemovesAllUnits_WhenReductionExceedsTotal()
    {
        var multiTemplateCard = new SimCardData
        {
            CatalogId = "multi_template_card",
            ManaCost = 3,
            SummonTime = 1.0f,
            IsSpell = false,
            UnitTemplates = new List<SimUnitTemplate>
            {
                new()
                {
                    Count = 5,
                    MaxHp = 100f,
                    AttackDamage = 10f,
                    AttackSpeed = 1f,
                    MoveSpeed = 3f,
                    AttackRange = 2f,
                    AggroRadius = 20f,
                    UnitType = UnitType.Melee,
                    MovementLayer = MovementLayer.Ground
                },
                new()
                {
                    Count = 1,
                    MaxHp = 120f,
                    AttackDamage = 12f,
                    AttackSpeed = 0.9f,
                    MoveSpeed = 2.8f,
                    AttackRange = 2f,
                    AggroRadius = 20f,
                    UnitType = UnitType.Melee,
                    MovementLayer = MovementLayer.Ground
                }
            }
        };

        _state.CardDataMap["multi_template_card"] = multiTemplateCard;
        _state.Summoners[0].Hand = new List<SimCardCatalogId> { "multi_template_card" };
        _state.Summoners[0].Deck = new List<SimCardCatalogId> { "multi_template_card" };
        _state.Summoners[0].HandRefs = new List<SimCardRuntimeRef>
        {
            new()
            {
                CatalogId = "multi_template_card",
                InstanceId = "card_instance_trimmed"
            }
        };
        _state.TraitRuntimeState.SetCardInstanceSpawnCountAdd(
            new TraitRuntimeCardInstanceId("card_instance_trimmed"),
            -6);

        var cmd = new PlayCardCommand(0, 0, new SimVector3(-5f, 0f, 0f))
        {
            ExecuteFrame = 1
        };
        _state.PendingCommandBuffer.Add(cmd);

        var events = _sim.Tick(Delta);

        var registrations = SimTestHelper.CountEvents<UnitRegisteredEvent>(events);
        AssertThat(registrations).IsEqual(0);
        AssertThat(_state.Units.Count).IsEqual(0);
    }

    [TestCase]
    public void Tick_PlayCardCommand_HandManagement()
    {
        var card = SimTestHelper.CreateSummonCard("test_unit", manaCost: 3);
        _state.CardDataMap["test_unit"] = card;
        _state.CardDataMap["other_card"] = SimTestHelper.CreateSummonCard("other_card", manaCost: 2);
        _state.Summoners[0].Hand = new List<SimCardCatalogId> { "test_unit", "other_card" };
        _state.Summoners[0].Deck = new List<SimCardCatalogId> { "deck_card_1" };
        _state.CardDataMap["deck_card_1"] = SimTestHelper.CreateSummonCard("deck_card_1");

        var cmd = new PlayCardCommand(0, 0, new SimVector3(-5f, 0f, 0f))
        {
            ExecuteFrame = 1
        };
        _state.PendingCommandBuffer.Add(cmd);

        _sim.Tick(Delta);

        // Hand: played card removed, replacement drawn from deck
        AssertThat(_state.Summoners[0].Hand.Count).IsEqual(2);
        AssertThat(_state.Summoners[0].Hand).Contains("other_card");
        // Played card should be in discard pile
        AssertThat(_state.Summoners[0].DiscardPile).Contains("test_unit");
    }

    [TestCase]
    public void Tick_ForfeitCommand_GameOver()
    {
        var cmd = new ForfeitCommand(0) { ExecuteFrame = 1 };
        _state.PendingCommandBuffer.Add(cmd);

        var events = _sim.Tick(Delta);

        AssertThat(_state.Phase).IsEqual(GamePhase.GameOver);
        AssertThat(_state.WinnerTeam.HasValue).IsTrue();
        AssertThat(_state.WinnerTeam!.Value == 1).IsTrue(); // Team 0 forfeited → team 1 wins

        var gameOver = SimTestHelper.FindEvent<GameOverEvent>(events);
        AssertThat(gameOver).IsNotNull();
        AssertThat(gameOver!.WinnerTeam == 1).IsTrue();
    }

    [TestCase]
    public void Tick_FutureFrameCommand_NotExecuted()
    {
        var card = SimTestHelper.CreateSummonCard("test_unit", manaCost: 3);
        _state.CardDataMap["test_unit"] = card;
        _state.Summoners[0].Hand = new List<SimCardCatalogId> { "test_unit" };

        var cmd = new PlayCardCommand(0, 0, new SimVector3(-5f, 0f, 0f))
        {
            ExecuteFrame = 100 // Far in the future
        };
        _state.PendingCommandBuffer.Add(cmd);

        _sim.Tick(Delta); // Frame 1

        // Command not executed yet
        AssertThat(_state.Summoners[0].Mana).IsEqual(10f);
        AssertThat(_state.PendingCommandBuffer.Count).IsEqual(1);
    }

    [TestCase]
    public void DeathCleanup_ReleasesSlots_BeforeReacquire()
    {
        var target = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 2f, z: 0f);
        var attacker = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0f);

        bool reserved = SimMeleeSlotManager.TryReserveSlot(attacker, _state, target.UnitId, out _);
        AssertThat(reserved).IsTrue();

        attacker.IsAlive = false;
        attacker.DeathCleanupTimer = 1f;

        _sim.Tick(Delta);

        var slotState = _state.TargetSlotStates[target.UnitId];
        foreach (var slot in slotState.Slots)
        {
            AssertThat(slot.ReservedUnitId == attacker.UnitId).IsFalse();
            AssertThat(slot.OccupiedUnitId == attacker.UnitId).IsFalse();
        }
    }

    [TestCase]
    public void FixedSeed_ReplayParity_ForCommitSlotFlow()
    {
        string runOne = RunCommitSlotReplayHash(seed: 424242u);
        string runTwo = RunCommitSlotReplayHash(seed: 424242u);

        AssertThat(runOne).IsEqual(runTwo);
    }

    [TestCase]
    public void FixedSeed_OverflowReplayParity_ForCommitSlotFlow()
    {
        string runOne = RunCommitSlotOverflowReplayHash(seed: 989898u);
        string runTwo = RunCommitSlotOverflowReplayHash(seed: 989898u);

        AssertThat(runOne).IsEqual(runTwo);
    }

    // =========================================================================
    // Spell Execution
    // =========================================================================

    [TestCase]
    public void Tick_SpellCard_DamagesEnemiesInRadius()
    {
        var spell = SimTestHelper.CreateSpellCard("test_spell", manaCost: 4, damage: 30f, radius: 10f);
        _state.CardDataMap["test_spell"] = spell;
        _state.Summoners[0].Hand = new List<SimCardCatalogId> { "test_spell" };
        _state.Summoners[0].Deck = new List<SimCardCatalogId> { "test_spell" };

        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        var farEnemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 30f);

        var cmd = new PlayCardCommand(0, 0, new SimVector3(5f, 0f, 0f))
        {
            ExecuteFrame = 1
        };
        _state.PendingCommandBuffer.Add(cmd);

        var events = _sim.Tick(Delta);

        // Near enemy should be damaged
        AssertThat(enemy.CurrentHp).IsLess(enemy.MaxHp);
        // Far enemy should be untouched
        AssertThat(farEnemy.CurrentHp).IsEqual(farEnemy.MaxHp);
    }

    [TestCase]
    public void Tick_SpellCard_RejectedDuringPrep()
    {
        _state.Phase = GamePhase.Preparation;
        _state.PrepTimeRemaining = 10f;

        var spell = SimTestHelper.CreateSpellCard("test_spell", manaCost: 4);
        _state.CardDataMap["test_spell"] = spell;
        _state.Summoners[0].Hand = new List<SimCardCatalogId> { "test_spell" };

        var cmd = new PlayCardCommand(0, 0, new SimVector3(0f, 0f, 0f))
        {
            ExecuteFrame = 1
        };
        _state.PendingCommandBuffer.Add(cmd);

        _sim.Tick(Delta);

        // Mana unchanged — spell rejected during prep
        AssertThat(_state.Summoners[0].Mana).IsEqual(10f);
    }

    [TestCase]
    public void Tick_SpellCard_PositionProjectile_SpawnsProjectile_AndDelaysDamageUntilImpact()
    {
        var spell = SimTestHelper.CreateSpellCard(
            "projectile_aoe_spell",
            manaCost: 4,
            damage: 40f,
            radius: 8f,
            targetingMode: SpellTargetingMode.Position,
            spellProjectileId: "fireball");
        _state.CardDataMap["projectile_aoe_spell"] = spell;
        _state.Summoners[0].Hand = new List<SimCardCatalogId> { "projectile_aoe_spell" };
        _state.Summoners[0].Deck = new List<SimCardCatalogId> { "projectile_aoe_spell" };

        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f, z: 0f);

        var cmd = new PlayCardCommand(0, 0, new SimVector3(5f, 0f, 0f))
        {
            ExecuteFrame = 1
        };
        _state.PendingCommandBuffer.Add(cmd);

        _sim.Tick(Delta);

        AssertThat(_state.Projectiles.Count).IsEqual(1);
        AssertThat(enemy.CurrentHp).IsEqual(enemy.MaxHp);

        for (int i = 0; i < 240 && enemy.CurrentHp >= enemy.MaxHp; i++)
            _sim.Tick(Delta);

        AssertThat(enemy.CurrentHp).IsLess(enemy.MaxHp);
    }

    [TestCase]
    public void Tick_SpellCard_NearestEnemyProjectile_SpawnsProjectile_AndDamagesOnHit()
    {
        var spell = SimTestHelper.CreateSpellCard(
            "projectile_single_spell",
            manaCost: 3,
            damage: 35f,
            radius: 0f,
            targetingMode: SpellTargetingMode.NearestEnemy,
            spellProjectileId: "mana_bolt");
        _state.CardDataMap["projectile_single_spell"] = spell;
        _state.Summoners[0].Hand = new List<SimCardCatalogId> { "projectile_single_spell" };
        _state.Summoners[0].Deck = new List<SimCardCatalogId> { "projectile_single_spell" };

        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f, z: 0f);

        var cmd = new PlayCardCommand(0, 0, new SimVector3(5f, 0f, 0f))
        {
            ExecuteFrame = 1
        };
        _state.PendingCommandBuffer.Add(cmd);

        _sim.Tick(Delta);

        AssertThat(_state.Projectiles.Count).IsEqual(1);
        AssertThat(enemy.CurrentHp).IsEqual(enemy.MaxHp);
        var initialProjectile = _state.Projectiles.Values.First();
        AssertThat(initialProjectile.Speed).IsEqual(18f); // ManaBolt SpeedStart from projectile defs

        _sim.Tick(Delta);
        var acceleratedProjectile = _state.Projectiles.Values.First();
        AssertThat(acceleratedProjectile.Speed).IsGreater(18f); // Speed easing ramps toward SpeedEnd

        for (int i = 0; i < 240 && enemy.CurrentHp >= enemy.MaxHp; i++)
            _sim.Tick(Delta);

        AssertThat(enemy.CurrentHp).IsLess(enemy.MaxHp);
    }

    [TestCase]
    public void Tick_SpellCard_NearestEnemyProjectile_ManaBolt_Uses3DWeave()
    {
        var spell = SimTestHelper.CreateSpellCard(
            "projectile_single_spell",
            manaCost: 3,
            damage: 35f,
            radius: 0f,
            targetingMode: SpellTargetingMode.NearestEnemy,
            spellProjectileId: "mana_bolt");
        _state.CardDataMap["projectile_single_spell"] = spell;
        _state.Summoners[0].Hand = new List<SimCardCatalogId> { "projectile_single_spell" };
        _state.Summoners[0].Deck = new List<SimCardCatalogId> { "projectile_single_spell" };

        SimTestHelper.CreateMeleeUnit(_state, 1, x: 18f, z: 0f);

        var cmd = new PlayCardCommand(0, 0, new SimVector3(18f, 0f, 0f))
        {
            ExecuteFrame = 1
        };
        _state.PendingCommandBuffer.Add(cmd);

        _sim.Tick(Delta);
        AssertThat(_state.Projectiles.Count).IsEqual(1);

        float maxVerticalOffset = 0f;
        for (int i = 0; i < 60 && _state.Projectiles.Count > 0; i++)
        {
            _sim.Tick(Delta);
            if (_state.Projectiles.Count == 0)
                break;

            var projectile = _state.Projectiles.Values.First();
            float verticalOffset = projectile.CurrentPosition.Y - projectile.StartPosition.Y;
            if (verticalOffset < 0f)
                verticalOffset = -verticalOffset;
            if (verticalOffset > maxVerticalOffset)
                maxVerticalOffset = verticalOffset;
        }

        AssertThat(maxVerticalOffset).IsGreater(0.35f);
    }

    [TestCase]
    public void Tick_SpellCard_NearestEnemyProjectile_AutoTargetsNearestEnemyToCaster()
    {
        var spell = SimTestHelper.CreateSpellCard(
            "projectile_single_spell",
            manaCost: 3,
            damage: 35f,
            radius: 0f,
            targetingMode: SpellTargetingMode.NearestEnemy,
            spellProjectileId: "mana_bolt");
        _state.CardDataMap["projectile_single_spell"] = spell;
        _state.Summoners[0].Hand = new List<SimCardCatalogId> { "projectile_single_spell" };
        _state.Summoners[0].Deck = new List<SimCardCatalogId> { "projectile_single_spell" };

        var nearToCaster = SimTestHelper.CreateMeleeUnit(_state, 1, x: -7f, z: 0f);
        var nearToCursor = SimTestHelper.CreateMeleeUnit(_state, 1, x: 26f, z: 0f);

        var cmd = new PlayCardCommand(0, 0, new SimVector3(26f, 0f, 0f))
        {
            ExecuteFrame = 1
        };
        _state.PendingCommandBuffer.Add(cmd);

        _sim.Tick(Delta);
        AssertThat(_state.Projectiles.Count).IsEqual(1);

        for (int i = 0; i < 240 && nearToCaster.CurrentHp >= nearToCaster.MaxHp; i++)
            _sim.Tick(Delta);

        AssertThat(nearToCaster.CurrentHp).IsLess(nearToCaster.MaxHp);
        AssertThat(nearToCursor.CurrentHp).IsEqual(nearToCursor.MaxHp);
    }

    [TestCase]
    public void Tick_SpellCard_NearestEnemyProjectile_ExplicitTarget_OverridesAutoTarget()
    {
        var spell = SimTestHelper.CreateSpellCard(
            "projectile_single_spell",
            manaCost: 3,
            damage: 35f,
            radius: 0f,
            targetingMode: SpellTargetingMode.NearestEnemy,
            spellProjectileId: "mana_bolt");
        _state.CardDataMap["projectile_single_spell"] = spell;
        _state.Summoners[0].Hand = new List<SimCardCatalogId> { "projectile_single_spell" };
        _state.Summoners[0].Deck = new List<SimCardCatalogId> { "projectile_single_spell" };

        var nearToCaster = SimTestHelper.CreateMeleeUnit(_state, 1, x: -7f, z: 6f);
        var explicitTarget = SimTestHelper.CreateMeleeUnit(_state, 1, x: 26f, z: 0f);

        var cmd = new PlayCardCommand(0, 0, new SimVector3(-7f, 0f, 6f))
        {
            ExecuteFrame = 1,
            TargetUnitId = explicitTarget.UnitId
        };
        _state.PendingCommandBuffer.Add(cmd);

        _sim.Tick(Delta);
        AssertThat(_state.Projectiles.Count).IsEqual(1);

        for (int i = 0; i < 480 && explicitTarget.CurrentHp >= explicitTarget.MaxHp; i++)
            _sim.Tick(Delta);

        AssertThat(explicitTarget.CurrentHp).IsLess(explicitTarget.MaxHp);
        AssertThat(nearToCaster.CurrentHp).IsEqual(nearToCaster.MaxHp);
    }

    [TestCase]
    public void Tick_SpellCard_WeavingBolt_UsesSpeedEasingCurve()
    {
        var spell = SimTestHelper.CreateSpellCard(
            "projectile_weaving_spell",
            manaCost: 3,
            damage: 35f,
            radius: 0f,
            targetingMode: SpellTargetingMode.NearestEnemy,
            spellProjectileId: "weaving_bolt");
        _state.CardDataMap["projectile_weaving_spell"] = spell;
        _state.Summoners[0].Hand = new List<SimCardCatalogId> { "projectile_weaving_spell" };
        _state.Summoners[0].Deck = new List<SimCardCatalogId> { "projectile_weaving_spell" };

        // Keep target far enough so projectile remains alive while easing ramps.
        SimTestHelper.CreateMeleeUnit(_state, 1, x: 45f, z: 0f);

        var cmd = new PlayCardCommand(0, 0, new SimVector3(45f, 0f, 0f))
        {
            ExecuteFrame = 1
        };
        _state.PendingCommandBuffer.Add(cmd);

        _sim.Tick(Delta);
        AssertThat(_state.Projectiles.Count).IsEqual(1);
        var projectile = _state.Projectiles.Values.First();
        AssertThat(projectile.Speed).IsEqual(28f); // WeavingBolt SpeedStart

        float maxObservedSpeed = projectile.Speed;
        for (int i = 0; i < 80; i++)
        {
            _sim.Tick(Delta);
            if (_state.Projectiles.Count == 0)
                break;
            var p = _state.Projectiles.Values.First();
            if (p.Speed > maxObservedSpeed)
                maxObservedSpeed = p.Speed;
        }

        // Ease-in curve should accelerate above start speed toward SpeedEnd (60).
        AssertThat(maxObservedSpeed).IsGreater(40f);
        AssertThat(maxObservedSpeed).IsLessEqual(60f);
    }

    [TestCase]
    public void Tick_SpellCard_InvalidProjectileId_DoesNotFallbackToInstantDamage()
    {
        var spell = SimTestHelper.CreateSpellCard(
            "invalid_projectile_spell",
            manaCost: 4,
            damage: 40f,
            radius: 8f,
            targetingMode: SpellTargetingMode.Position,
            spellProjectileId: "missing_projectile");
        _state.CardDataMap["invalid_projectile_spell"] = spell;
        _state.Summoners[0].Hand = new List<SimCardCatalogId> { "invalid_projectile_spell" };
        _state.Summoners[0].Deck = new List<SimCardCatalogId> { "invalid_projectile_spell" };

        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f, z: 0f);

        var cmd = new PlayCardCommand(0, 0, new SimVector3(5f, 0f, 0f))
        {
            ExecuteFrame = 1
        };
        _state.PendingCommandBuffer.Add(cmd);

        _sim.Tick(Delta);

        AssertThat(_state.Projectiles.Count).IsEqual(0);
        AssertThat(enemy.CurrentHp).IsEqual(enemy.MaxHp);
    }

    // =========================================================================
    // Phase Transitions
    // =========================================================================

    [TestCase]
    public void Tick_PrepTimerExpires_TransitionsToBattle()
    {
        _state.Phase = GamePhase.Preparation;
        _state.PrepTimeRemaining = Delta * 0.5f; // Less than one tick

        var events = _sim.Tick(Delta);

        AssertThat(_state.Phase).IsEqual(GamePhase.Battle);

        var phaseEvent = SimTestHelper.FindEvent<PhaseChangedEvent>(events);
        AssertThat(phaseEvent).IsNotNull();
        AssertThat(phaseEvent!.NewPhase).IsEqual(GamePhase.Battle);
    }

    [TestCase]
    public void Tick_PhaseTransition_ActivatesUnits()
    {
        _state.Phase = GamePhase.Preparation;
        _state.PrepTimeRemaining = Delta * 0.5f;

        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        unit.ActivationState = 0; // Inactive (spawned during prep)

        _sim.Tick(Delta);

        AssertThat(unit.ActivationState).IsEqual(ActivationState.Active); // Active
    }

    [TestCase]
    public void Tick_PhaseTransition_RefreshesHands()
    {
        _state.Phase = GamePhase.Preparation;
        _state.PrepTimeRemaining = Delta * 0.5f;

        _state.Summoners[0].Hand = new List<SimCardCatalogId> { "old_card" };
        _state.Summoners[0].Deck = new List<SimCardCatalogId> { "card1", "card2", "card3", "card4" };
        _state.Summoners[0].MaxHandSize = 4;

        var events = _sim.Tick(Delta);

        // old_card goes to discard, 4 new cards drawn
        AssertThat(_state.Summoners[0].DiscardPile).Contains("old_card");
        AssertThat(_state.Summoners[0].Hand.Count).IsEqual(4);
    }

    // =========================================================================
    // Casting
    // =========================================================================

    [TestCase]
    public void Tick_CastingTimer_Decrements()
    {
        _state.Summoners[0].IsCasting = true;
        _state.Summoners[0].CastingTimeRemaining = 1.0f;
        _state.Summoners[0].CastingTimeTotal = 1.0f;

        _sim.Tick(Delta);

        AssertThat(_state.Summoners[0].CastingTimeRemaining).IsLess(1.0f);
        AssertThat(_state.Summoners[0].IsCasting).IsTrue(); // Still casting
    }

    [TestCase]
    public void Tick_CastingComplete_UnlocksSummoner()
    {
        _state.Summoners[0].IsCasting = true;
        _state.Summoners[0].CastingTimeRemaining = Delta * 0.5f; // Will complete this tick

        var events = _sim.Tick(Delta);

        AssertThat(_state.Summoners[0].IsCasting).IsFalse();

        var completed = SimTestHelper.FindEvent<CastingCompletedEvent>(events);
        AssertThat(completed).IsNotNull();
    }

    // =========================================================================
    // Death Cleanup
    // =========================================================================

    [TestCase]
    public void Tick_DeathCleanup_RemovesAfterTimer()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 0f);
        unit.IsAlive = false;
        unit.DeathCleanupTimer = Delta * 0.5f; // Will expire this tick

        var events = _sim.Tick(Delta);

        // Unit should be removed from state
        AssertThat(_state.Units.ContainsKey(unit.UnitId)).IsFalse();

        var removed = SimTestHelper.FindEvent<UnitRemovedEvent>(events);
        AssertThat(removed).IsNotNull();
        AssertThat(removed!.UnitId).IsEqual(unit.UnitId);
    }

    // =========================================================================
    // Win Conditions in Tick
    // =========================================================================

    [TestCase]
    public void Tick_SummonerDies_EmitsGameOverEvent()
    {
        _state.Summoners[1].IsAlive = false;
        _state.Summoners[1].CurrentHp = 0;

        var events = _sim.Tick(Delta);

        var gameOver = SimTestHelper.FindEvent<GameOverEvent>(events);
        AssertThat(gameOver).IsNotNull();
        AssertThat(gameOver!.WinnerTeam).IsEqual(0); // Team 0 wins
        AssertThat(_state.Phase).IsEqual(GamePhase.GameOver);
    }

    // =========================================================================
    // Deck Recycling
    // =========================================================================

    [TestCase]
    public void Tick_DeckExhausted_RecyclesDiscard()
    {
        var card = SimTestHelper.CreateSummonCard("test_unit", manaCost: 1);
        _state.CardDataMap["test_unit"] = card;

        // Hand has 1 card, deck is empty, discard has cards
        _state.Summoners[0].Hand = new List<SimCardCatalogId> { "test_unit" };
        _state.Summoners[0].Deck = new List<SimCardCatalogId>();
        _state.Summoners[0].DiscardPile = new List<SimCardCatalogId> { "test_unit", "test_unit", "test_unit" };

        var cmd = new PlayCardCommand(0, 0, new SimVector3(-5f, 0f, 0f))
        {
            ExecuteFrame = 1
        };
        _state.PendingCommandBuffer.Add(cmd);

        var events = _sim.Tick(Delta);

        var recycled = SimTestHelper.FindEvent<DeckRecycledEvent>(events);
        AssertThat(recycled).IsNotNull();
    }

    [TestCase]
    public void Tick_RecycleShuffle_Deterministic()
    {
        var card = SimTestHelper.CreateSummonCard("test_unit", manaCost: 1);
        _state.CardDataMap["test_unit"] = card;
        _state.CardDataMap["card_a"] = SimTestHelper.CreateSummonCard("card_a");
        _state.CardDataMap["card_b"] = SimTestHelper.CreateSummonCard("card_b");
        _state.CardDataMap["card_c"] = SimTestHelper.CreateSummonCard("card_c");

        // Setup state for first run
        _state.Summoners[0].Hand = new List<SimCardCatalogId> { "test_unit" };
        _state.Summoners[0].Deck = new List<SimCardCatalogId>();
        _state.Summoners[0].DiscardPile = new List<SimCardCatalogId> { "card_a", "card_b", "card_c" };

        var cmd1 = new PlayCardCommand(0, 0, new SimVector3(-5f, 0f, 0f)) { ExecuteFrame = 1 };
        _state.PendingCommandBuffer.Add(cmd1);

        _sim.Tick(Delta);
        var hand1 = _state.Summoners[0].Hand.ToList();

        // Setup identical state for second run
        var state2 = SimTestHelper.CreateBattleState();
        state2.CardDataMap["test_unit"] = card;
        state2.CardDataMap["card_a"] = SimTestHelper.CreateSummonCard("card_a");
        state2.CardDataMap["card_b"] = SimTestHelper.CreateSummonCard("card_b");
        state2.CardDataMap["card_c"] = SimTestHelper.CreateSummonCard("card_c");
        state2.Summoners[0].Hand = new List<SimCardCatalogId> { "test_unit" };
        state2.Summoners[0].Deck = new List<SimCardCatalogId>();
        state2.Summoners[0].DiscardPile = new List<SimCardCatalogId> { "card_a", "card_b", "card_c" };

        var sim2 = new Fateforged.Simulation.Simulation(state2);
        var cmd2 = new PlayCardCommand(0, 0, new SimVector3(-5f, 0f, 0f)) { ExecuteFrame = 1 };
        state2.PendingCommandBuffer.Add(cmd2);

        sim2.Tick(Delta);
        var hand2 = state2.Summoners[0].Hand.ToList();

        // Same seed → same shuffle → same hand
        AssertThat(hand1.Count).IsEqual(hand2.Count);
        for (int i = 0; i < hand1.Count; i++)
        {
            AssertThat(hand1[i]).IsEqual(hand2[i]);
        }
    }

    // =========================================================================
    // Determinism
    // =========================================================================

    [TestCase]
    public void Tick_SameInputs_SameOutputs()
    {
        // Setup two identical simulations
        SetupCombatScenario(_state);
        var sim1 = new Fateforged.Simulation.Simulation(_state);

        var state2 = SimTestHelper.CreateBattleState();
        SetupCombatScenario(state2);
        var sim2 = new Fateforged.Simulation.Simulation(state2);

        // Run one tick on each
        var events1 = sim1.Tick(Delta);
        var events2 = sim2.Tick(Delta);

        // Same number of events
        AssertThat(events1.Count).IsEqual(events2.Count);

        // Same state after tick
        AssertThat(_state.FrameNumber).IsEqual(state2.FrameNumber);
        AssertThat(_state.MatchTime).IsEqual(state2.MatchTime);
    }

    [TestCase]
    public void Tick_MultiTick_DeterministicSequence()
    {
        SetupCombatScenario(_state);
        var sim1 = new Fateforged.Simulation.Simulation(_state);

        var state2 = SimTestHelper.CreateBattleState();
        SetupCombatScenario(state2);
        var sim2 = new Fateforged.Simulation.Simulation(state2);

        // Run 100 ticks on each
        for (int i = 0; i < 100; i++)
        {
            sim1.Tick(Delta);
            sim2.Tick(Delta);
        }

        // States must match
        AssertThat(_state.FrameNumber).IsEqual(state2.FrameNumber);
        AssertThat(_state.MatchTime).IsEqual(state2.MatchTime);
        AssertThat(_state.Phase).IsEqual(state2.Phase);

        // Unit states match (compare alive units)
        foreach (var (unitId, unit1) in _state.Units)
        {
            AssertThat(state2.Units.ContainsKey(unitId)).IsTrue();
            var unit2 = state2.Units[unitId];
            AssertThat(unit1.CurrentHp).IsEqual(unit2.CurrentHp);
            AssertThat(unit1.IsAlive).IsEqual(unit2.IsAlive);
            AssertThat(unit1.Position.X).IsEqual(unit2.Position.X);
            AssertThat(unit1.Position.Z).IsEqual(unit2.Position.Z);
        }
    }

    // =========================================================================
    // Spawn Activation
    // =========================================================================

    [TestCase]
    public void Tick_SpawnDuringPrep_UnitInactive()
    {
        _state.Phase = GamePhase.Preparation;
        _state.PrepTimeRemaining = 10f;

        var card = SimTestHelper.CreateSummonCard("test_unit", manaCost: 3);
        _state.CardDataMap["test_unit"] = card;
        _state.Summoners[0].Hand = new List<SimCardCatalogId> { "test_unit" };
        _state.Summoners[0].Deck = new List<SimCardCatalogId> { "test_unit" };

        var cmd = new PlayCardCommand(0, 0, new SimVector3(-5f, 0f, 0f))
        {
            ExecuteFrame = 1
        };
        _state.PendingCommandBuffer.Add(cmd);

        _sim.Tick(Delta);

        // Units spawned during prep should be inactive
        var spawnedUnit = _state.Units.Values.First();
        AssertThat(spawnedUnit.ActivationState).IsEqual(ActivationState.Inactive); // Inactive
    }

    [TestCase]
    public void Tick_SpawnDuringBattle_UnitInactiveUntilSpawnTimerExpires()
    {
        var card = SimTestHelper.CreateSummonCard("test_unit", manaCost: 3, summonTime: 0.5f);
        _state.CardDataMap["test_unit"] = card;
        _state.Summoners[0].Hand = new List<SimCardCatalogId> { "test_unit" };
        _state.Summoners[0].Deck = new List<SimCardCatalogId> { "test_unit" };

        var cmd = new PlayCardCommand(0, 0, new SimVector3(-5f, 0f, 0f))
        {
            ExecuteFrame = 1
        };
        _state.PendingCommandBuffer.Add(cmd);

        _sim.Tick(Delta); // Frame 1: spawns unit

        // Unit exists but is inactive (spawn timer still counting down)
        var spawnedUnit = _state.Units.Values.First();
        AssertThat(spawnedUnit.ActivationState).IsEqual(ActivationState.Inactive); // Inactive
        AssertThat(spawnedUnit.SpawnTimer).IsGreater(0f);

        // Tick until spawn timer expires (0.5s = 30 ticks at 60fps)
        for (int i = 0; i < 30; i++)
            _sim.Tick(Delta);

        // Now the unit should be active
        AssertThat(spawnedUnit.ActivationState).IsEqual(ActivationState.Active); // Active
    }

    // =========================================================================
    // Cast Speed
    // =========================================================================

    [TestCase]
    public void Tick_CastSpeedBonus_ReducesCastTime()
    {
        var card = SimTestHelper.CreateSummonCard("test_unit", manaCost: 3, summonTime: 2.0f);
        _state.CardDataMap["test_unit"] = card;
        _state.Summoners[0].Hand = new List<SimCardCatalogId> { "test_unit" };
        _state.Summoners[0].Deck = new List<SimCardCatalogId> { "test_unit" };
        _state.Summoners[0].CastSpeed = 2.0f; // 2x cast speed

        var cmd = new PlayCardCommand(0, 0, new SimVector3(-5f, 0f, 0f))
        {
            ExecuteFrame = 1
        };
        _state.PendingCommandBuffer.Add(cmd);

        _sim.Tick(Delta);

        // Effective summon time = 2.0 / 2.0 = 1.0
        AssertThat(_state.Summoners[0].CastingTimeTotal).IsEqual(1.0f);
    }

    // =========================================================================
    // Prep Skip Regression
    // =========================================================================

    [TestCase]
    public void Tick_PrepSkip_ThenSpawn_UnitsActiveAndTargeting()
    {
        // Start in Prep, skip (set timer to 0)
        _state.Phase = GamePhase.Preparation;
        _state.PrepTimeRemaining = 0f;

        // Tick once → transitions to Battle
        _sim.Tick(Delta);
        AssertThat(_state.Phase).IsEqual(GamePhase.Battle);

        // Now spawn a unit via command (short summon time so spawn timer expires quickly)
        var card = SimTestHelper.CreateSummonCard("test_unit", manaCost: 3, summonTime: 0.1f);
        _state.CardDataMap["test_unit"] = card;
        _state.Summoners[0].Hand = new List<SimCardCatalogId> { "test_unit" };
        _state.Summoners[0].Deck = new List<SimCardCatalogId> { "test_unit" };

        var cmd = new PlayCardCommand(0, 0, new SimVector3(-5f, 0f, 0f))
        {
            ExecuteFrame = _state.FrameNumber + 1
        };
        _state.PendingCommandBuffer.Add(cmd);

        _sim.Tick(Delta); // Spawns unit (inactive, spawn timer = 0.1s)

        var unit = _state.Units.Values.First();
        AssertThat(unit.IsAlive).IsTrue();
        AssertThat(unit.ActivationState).IsEqual(ActivationState.Inactive); // Inactive during spawn timer

        // Tick until spawn timer expires (0.1s = 6 ticks at 60fps)
        for (int i = 0; i < 6; i++)
            _sim.Tick(Delta);

        AssertThat(unit.ActivationState).IsEqual(ActivationState.Active); // Active

        // Tick again — unit should be processed (targeting, behavior, movement)
        var startPos = unit.Position;
        _sim.Tick(Delta);

        // Unit should have moved (NoTarget → MoveForward toward enemy base)
        AssertThat(unit.Position.X).IsNotEqual(startPos.X);
    }

    [TestCase]
    public void Tick_NoEnemyUnits_UnitTargetsSummoner()
    {
        // Place unit with no enemies — should fall back to enemy summoner
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0, x: 18f, attackRange: 3f);

        // Tick enough for targeting
        for (int i = 0; i < 60; i++)
            _sim.Tick(Delta);

        // Unit should be targeting the enemy summoner (target ID = -2 for team 1)
        AssertThat(unit.TargetUnitId.HasValue).IsTrue();
        AssertThat(unit.TargetUnitId!.Value).IsEqual(-2);
    }

    [TestCase]
    public void Tick_MeleeUnit_DamagesEnemySummoner()
    {
        // Place unit at enemy summoner position (in attack range)
        var summonerPos = _state.Summoners[1].Position;
        var unit = SimTestHelper.CreateMeleeUnit(_state, 0,
            x: summonerPos.X - 1f, z: summonerPos.Z,
            damage: 10f, attackRange: 3f);

        float initialHp = _state.Summoners[1].CurrentHp;

        // Tick enough for targeting + attack
        for (int i = 0; i < 120; i++)
            _sim.Tick(Delta);

        // Enemy summoner should have taken damage
        AssertThat(_state.Summoners[1].CurrentHp).IsLess(initialHp);
    }

    [TestCase]
    public void SpawnedUnit_RetainsDamageProfileFields()
    {
        var card = SimTestHelper.CreateSummonCard("profile_spawn_card", manaCost: 2, unitCount: 1);
        card.UnitTemplates[0].AttackType = DamageType.Physical;
        card.UnitTemplates[0].PhysicalDamageRatio = 0.35f;
        card.UnitTemplates[0].ElementalDamageRatio = 0.65f;

        _state.CardDataMap["profile_spawn_card"] = card;
        _state.Summoners[0].Hand = new List<SimCardCatalogId> { "profile_spawn_card" };
        _state.Summoners[0].Deck = new List<SimCardCatalogId> { "profile_spawn_card" };

        var cmd = new PlayCardCommand(0, 0, new SimVector3(-5f, 0f, 0f))
        {
            ExecuteFrame = 1
        };
        _state.PendingCommandBuffer.Add(cmd);

        _sim.Tick(Delta);

        AssertThat(_state.Units.Count).IsEqual(1);
        var spawned = _state.Units.Values.First();
        AssertThat(spawned.PhysicalDamageRatio).IsEqual(0.35f);
        AssertThat(spawned.ElementalDamageRatio).IsEqual(0.65f);
        AssertThat(spawned.AttackType).IsEqual(DamageType.Physical);
    }

    [TestCase]
    public void SpawnedUnit_RetainsAttackVectorFields()
    {
        var card = SimTestHelper.CreateSummonCard("vector_spawn_card", manaCost: 2, unitCount: 1);
        var template = card.UnitTemplates[0];
        template.Attack.Preset = AttackPreset.Custom;
        template.Attack.Selection.Mode = AttackSelectionMode.LineCollect;
        template.Attack.Area.Shape = AttackAreaShape.Line;
        template.Attack.Area.Size = new SimVector3(2f, 1f, 6f);
        template.Attack.Selection.TargetLimit = 3;
        template.Attack.Propagation.Mode = AttackPropagationMode.Pierce;
        template.Attack.Area.LineLength = 6f;
        template.Attack.Area.LineHalfWidth = 1.25f;
        template.Attack.Propagation.ChainMaxJumps = 2;
        template.Attack.Propagation.ChainJumpRadius = 4.5f;
        template.Attack.Timing.WindupSeconds = 0.1f;
        template.Attack.Timing.ActiveSeconds = 0.05f;
        template.Attack.Timing.RecoverySeconds = 0.2f;
        template.Attack.Timing.TickIntervalSeconds = 0.05f;
        template.Attack.Rules.IncludeSummonerTargets = false;
        template.Attack.Rules.AllowRepeatHits = false;
        template.Attack.Rules.TriggerMode = AttackTriggerMode.PrimaryOnly;

        _state.CardDataMap["vector_spawn_card"] = card;
        _state.Summoners[0].Hand = new List<SimCardCatalogId> { "vector_spawn_card" };
        _state.Summoners[0].Deck = new List<SimCardCatalogId> { "vector_spawn_card" };

        var cmd = new PlayCardCommand(0, 0, new SimVector3(-5f, 0f, 0f))
        {
            ExecuteFrame = 1
        };
        _state.PendingCommandBuffer.Add(cmd);

        _sim.Tick(Delta);

        AssertThat(_state.Units.Count).IsEqual(1);
        var spawned = _state.Units.Values.First();
        AssertThat(spawned.Attack.Preset).IsEqual(AttackPreset.Custom);
        AssertThat(spawned.Attack.Selection.Mode).IsEqual(AttackSelectionMode.LineCollect);
        AssertThat(spawned.Attack.Area.Shape).IsEqual(AttackAreaShape.Line);
        AssertThat(spawned.Attack.Area.Size.X).IsEqual(2f);
        AssertThat(spawned.Attack.Selection.TargetLimit).IsEqual(3);
        AssertThat(spawned.Attack.Propagation.Mode).IsEqual(AttackPropagationMode.Pierce);
        AssertThat(spawned.Attack.Area.LineLength).IsEqual(6f);
        AssertThat(spawned.Attack.Area.LineHalfWidth).IsEqual(1.25f);
        AssertThat(spawned.Attack.Propagation.ChainMaxJumps).IsEqual(2);
        AssertThat(spawned.Attack.Propagation.ChainJumpRadius).IsEqual(4.5f);
        AssertThat(spawned.Attack.Timing.WindupSeconds).IsEqual(0.1f);
        AssertThat(spawned.Attack.Timing.TickIntervalSeconds).IsEqual(0.05f);
        AssertThat(spawned.Attack.Rules.TriggerMode).IsEqual(AttackTriggerMode.PrimaryOnly);
    }

    // =========================================================================
    // Helper
    // =========================================================================

    private static string RunCommitSlotReplayHash(uint seed)
    {
        var state = SimTestHelper.CreateBattleState(seed);
        var sim = new Fateforged.Simulation.Simulation(state);

        var target = SimTestHelper.CreateMeleeUnit(
            state,
            team: 1,
            x: 2f,
            z: 0f,
            hp: 500f,
            moveSpeed: 0f,
            attackSpeed: 0f
        );

        var attackerOne = SimTestHelper.CreateMeleeUnit(state, team: 0, x: -2f, z: -0.25f, attackRange: 2.5f);
        var attackerTwo = SimTestHelper.CreateMeleeUnit(state, team: 0, x: -2f, z: 0.25f, attackRange: 2.5f);
        attackerOne.CombatLifecycleState = CombatLifecycleState.AcquireTarget;
        attackerTwo.CombatLifecycleState = CombatLifecycleState.AcquireTarget;
        attackerOne.LockedTargetUnitId = target.UnitId;
        attackerTwo.LockedTargetUnitId = target.UnitId;
        attackerOne.TargetUnitId = target.UnitId;
        attackerTwo.TargetUnitId = target.UnitId;

        var snapshots = new List<string>();
        for (int i = 0; i < 240; i++)
        {
            sim.Tick(Delta);
            if ((i + 1) % 60 != 0)
                continue;

            snapshots.Add(
                $"{i + 1}|{target.CurrentHp:F2}|{attackerOne.TargetUnitId}|{attackerTwo.TargetUnitId}|{attackerOne.Position.X:F3}|{attackerTwo.Position.X:F3}"
            );
        }

        return string.Join(";", snapshots);
    }

    private static string RunCommitSlotOverflowReplayHash(uint seed)
    {
        var state = SimTestHelper.CreateBattleState(seed);
        var sim = new Fateforged.Simulation.Simulation(state);

        var primaryTarget = SimTestHelper.CreateMeleeUnit(
            state,
            team: 1,
            x: 2f,
            z: 0f,
            hp: 3000f,
            moveSpeed: 0f,
            attackSpeed: 0f
        );
        primaryTarget.NavigationRadius = 0.2f;

        var fallbackTarget = SimTestHelper.CreateMeleeUnit(
            state,
            team: 1,
            x: 5.5f,
            z: 0f,
            hp: 3000f,
            moveSpeed: 0f,
            attackSpeed: 0f
        );

        var attackers = new List<UnitData>();
        for (int i = 0; i < 15; i++)
        {
            float z = -1.4f + (i * 0.2f);
            var attacker = SimTestHelper.CreateMeleeUnit(
                state,
                team: 0,
                x: -2.5f - (0.05f * i),
                z: z,
                attackRange: 2.4f,
                aggroRadius: 30f
            );

            attacker.CombatLifecycleState = CombatLifecycleState.AcquireTarget;
            attacker.LockedTargetUnitId = primaryTarget.UnitId;
            attacker.TargetUnitId = primaryTarget.UnitId;
            attackers.Add(attacker);
        }

        var snapshots = new List<string>();
        for (int frame = 1; frame <= 300; frame++)
        {
            sim.Tick(Delta);
            if (frame % 60 != 0)
                continue;

            int onPrimary = 0;
            int onFallback = 0;
            int onSummoner = 0;
            int droppedPrimary = 0;
            int slottingPrimary = 0;

            foreach (var attacker in attackers)
            {
                int? targetId = attacker.LockedTargetUnitId ?? attacker.TargetUnitId;
                if (targetId.HasValue && targetId.Value == primaryTarget.UnitId)
                    onPrimary++;
                else if (targetId.HasValue && targetId.Value == fallbackTarget.UnitId)
                    onFallback++;
                else if (MatchState.IsSummonerTarget(targetId))
                    onSummoner++;

                if (attacker.DroppedTargetUnitId.HasValue && attacker.DroppedTargetUnitId.Value == primaryTarget.UnitId)
                    droppedPrimary++;
                if (attacker.SlotTargetId.HasValue && attacker.SlotTargetId.Value == primaryTarget.UnitId)
                    slottingPrimary++;
            }

            snapshots.Add(
                $"{frame}|hp={primaryTarget.CurrentHp:F1}|p={onPrimary}|f={onFallback}|s={onSummoner}|d={droppedPrimary}|sp={slottingPrimary}|sw={state.CombatTargetSwitchCount}|bt={state.CombatBlockedTimeoutRetargetCount}"
            );
        }

        return string.Join(";", snapshots);
    }

    private static void SetupCombatScenario(MatchState state)
    {
        var attacker = SimTestHelper.CreateMeleeUnit(state, 0, x: -2f, damage: 10f, attackRange: 3f);
        attacker.CritChance = 0f;
        attacker.ElementId = 0;

        var defender = SimTestHelper.CreateMeleeUnit(state, 1, x: 2f, damage: 5f, attackRange: 3f);
        defender.CritChance = 0f;
        defender.Evasion = 0f;
        defender.ElementId = 0;
    }
}
