namespace ProjectSummoner.Tests.Multiplayer;

using System.Collections.Generic;
using System.Linq;
using Godot;
using GdUnit4;
using Fateforged.Multiplayer.Authority;
using Fateforged.Multiplayer.Core;
using Fateforged.Multiplayer.Protocol;
using Fateforged.Simulation;
using static GdUnit4.Assertions;

/// <summary>
/// For each Broadcast SimEvent, constructs realistic MatchState + event data,
/// runs the real HostEventBroadcaster, and asserts every protocol message field
/// has a non-default value.
///
/// Catches bugs like SpawnDuration being omitted from UnitSpawned because the
/// broadcaster forgot to read it from MatchState.
/// </summary>
[TestSuite]
public class BroadcastFieldTest
{
    /// <summary>
    /// Test double that captures broadcast messages for inspection.
    /// </summary>
    private class CapturingBroadcaster : IMessageBroadcaster
    {
        public List<object> Messages { get; } = new();
        public void Broadcast(object message) => Messages.Add(message);
    }

    private static MatchState CreateBaseState()
    {
        return new MatchState
        {
            FrameNumber = 100,
            MatchTime = 45.5f,
            Phase = GamePhase.Battle,
            Rng = new DeterministicRng(42)
        };
    }

    // ── UnitRegistered → UnitSpawned ──

    [TestCase]
    public void UnitRegistered_BroadcastPopulatesAllFields()
    {
        var state = CreateBaseState();
        var unit = new UnitData
        {
            UnitId = 1,
            NetworkId = 5,
            CatalogId = "skeleton_warrior",
            Team = 1,
            Position = new SimVector3(3.0f, 0.0f, 2.0f),
            SpawnTimer = 1.5f,
            IsAlive = true,
            MaxHp = 100f,
            CurrentHp = 100f
        };
        state.Units[unit.UnitId] = unit;

        var capturing = new CapturingBroadcaster();
        var broadcaster = new HostEventBroadcaster(capturing, state);

        var evt = new UnitRegisteredEvent(
            unitId: 1,
            networkId: 5,
            catalogId: "skeleton_warrior",
            team: 1,
            position: new SimVector3(3.0f, 0.0f, 2.0f)
        );
        evt.Accept(broadcaster);

        AssertThat(capturing.Messages).HasSize(1);
        var msg = (UnitSpawned)capturing.Messages[0];

        // Every field should be non-default
        AssertThat(msg.NetworkId).IsEqual(5);
        AssertThat(msg.UnitType).IsNotEmpty();
        AssertThat(msg.Team).IsEqual(1);
        AssertThat(msg.Position).IsNotEqual(Vector3.Zero);
        AssertThat(msg.MatchTick).IsGreater(0);
        AssertThat(msg.SpawnDuration).IsGreater(0f);
    }

    // ── UnitDied → UnitDied ──

    [TestCase]
    public void UnitDied_BroadcastPopulatesAllFields()
    {
        var state = CreateBaseState();

        var deadUnit = new UnitData
        {
            UnitId = 1,
            NetworkId = 5,
            Team = 0,
            IsAlive = false,
            CurrentHp = 0
        };
        var killerUnit = new UnitData
        {
            UnitId = 2,
            NetworkId = 8,
            Team = 1,
            IsAlive = true,
            CurrentHp = 50f,
            MaxHp = 100f
        };
        state.Units[deadUnit.UnitId] = deadUnit;
        state.Units[killerUnit.UnitId] = killerUnit;

        var capturing = new CapturingBroadcaster();
        var broadcaster = new HostEventBroadcaster(capturing, state);

        var evt = new UnitDiedSimEvent(unitId: 1, killerUnitId: 2);
        evt.Accept(broadcaster);

        AssertThat(capturing.Messages).HasSize(1);
        var msg = (UnitDied)capturing.Messages[0];

        AssertThat(msg.NetworkId).IsEqual(5);
        AssertThat(msg.KillerNetworkId.HasValue).IsTrue();
        AssertThat(msg.KillerNetworkId!.Value).IsEqual(8);
    }

    // ── UnitDamaged → DamageDealt ──

    [TestCase]
    public void UnitDamaged_BroadcastPopulatesAllFields()
    {
        var state = CreateBaseState();

        var targetUnit = new UnitData
        {
            UnitId = 1,
            NetworkId = 5,
            Team = 0,
            IsAlive = true,
            CurrentHp = 50f,
            MaxHp = 100f
        };
        var attackerUnit = new UnitData
        {
            UnitId = 2,
            NetworkId = 8,
            Team = 1,
            IsAlive = true,
            CurrentHp = 100f,
            MaxHp = 100f
        };
        state.Units[targetUnit.UnitId] = targetUnit;
        state.Units[attackerUnit.UnitId] = attackerUnit;

        var capturing = new CapturingBroadcaster();
        var broadcaster = new HostEventBroadcaster(capturing, state);

        var evt = new UnitDamagedEvent(targetUnitId: 1, attackerUnitId: 2, damage: 25.5f, isCrit: true);
        evt.Accept(broadcaster);

        AssertThat(capturing.Messages).HasSize(1);
        var msg = (DamageDealt)capturing.Messages[0];

        AssertThat(msg.TargetNetworkId).IsEqual(5);
        AssertThat(msg.Amount).IsGreater(0f);
        AssertThat(msg.IsCrit).IsTrue();
        AssertThat(msg.SourceNetworkId.HasValue).IsTrue();
        AssertThat(msg.SourceNetworkId!.Value).IsEqual(8);
    }

    // ── SummonerHpChanged → SummonerDamaged ──

    [TestCase]
    public void SummonerHpChanged_BroadcastPopulatesAllFields()
    {
        var state = CreateBaseState();

        // Set up summoner with known MaxHp so damage amount is non-zero
        state.Summoners[1].Team = 1;
        state.Summoners[1].MaxHp = 100f;
        state.Summoners[1].CurrentHp = 75f;
        state.Summoners[1].IsAlive = true;

        var capturing = new CapturingBroadcaster();
        var broadcaster = new HostEventBroadcaster(capturing, state);

        // Event: team 1 summoner HP changed to 75 (took 25 damage from 100 max)
        var evt = new SummonerHpChangedEvent(team: 1, hp: 75f, maxHp: 100f);
        evt.Accept(broadcaster);

        AssertThat(capturing.Messages).HasSize(1);
        var msg = (SummonerDamaged)capturing.Messages[0];

        AssertThat(msg.Team).IsEqual(1);
        AssertThat(msg.Amount).IsGreater(0f);
        AssertThat(msg.NewHp).IsGreater(0f);
    }

    // ── GameOver → MatchEnded ──

    [TestCase]
    public void GameOver_BroadcastPopulatesAllFields()
    {
        var state = CreateBaseState();

        var capturing = new CapturingBroadcaster();
        var broadcaster = new HostEventBroadcaster(capturing, state);

        var evt = new GameOverEvent(winnerTeam: 1, reason: "Summoner destroyed");
        evt.Accept(broadcaster);

        AssertThat(capturing.Messages).HasSize(1);
        var msg = (MatchEnded)capturing.Messages[0];

        AssertThat(msg.WinnerIndex).IsEqual(1);
        AssertThat(msg.Reason).IsNotEmpty();
        AssertThat(msg.Duration).IsGreater(0f);
    }
}
