namespace Fateforged.Tests.Multiplayer;

using System;
using GdUnit4;
using Godot;
using Fateforged.Multiplayer.Protocol;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for MessageSerializer round-trip serialization.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class MessageSerializerTest
{
    private MessageSerializer _serializer = null!;

    [BeforeTest]
    public void Setup()
    {
        _serializer = new MessageSerializer();
    }

    [TestCase]
    public void CardPlayRequest_RoundTrip()
    {
        var original = new CardPlayRequest(
            Sequence: 42,
            PlayerIndex: 1,
            CardIndex: 3,
            Position: new Vector3(5.5f, 0, 10.2f),
            ClientTimestamp: 12345678L
        );

        var dict = _serializer.Serialize(original);
        var result = _serializer.Deserialize(dict);

        AssertThat(result).IsInstanceOf<CardPlayRequest>();
        var typed = (CardPlayRequest)result;
        AssertThat(typed.Sequence).IsEqual(42);
        AssertThat(typed.PlayerIndex).IsEqual(1);
        AssertThat(typed.CardIndex).IsEqual(3);
        AssertThat(typed.Position.X).IsEqual(5.5f);
        AssertThat(typed.Position.Z).IsEqual(10.2f);
        AssertThat(typed.ClientTimestamp).IsEqual(12345678L);
    }

    [TestCase]
    public void ForfeitRequest_RoundTrip()
    {
        var original = new ForfeitRequest(PlayerIndex: 0);

        var dict = _serializer.Serialize(original);
        var result = _serializer.Deserialize(dict);

        AssertThat(result).IsInstanceOf<ForfeitRequest>();
        var typed = (ForfeitRequest)result;
        AssertThat(typed.PlayerIndex).IsEqual(0);
    }

    [TestCase]
    public void StateHashReport_RoundTrip()
    {
        var original = new StateHashReport(
            PlayerIndex: 1,
            Frame: 1000L,
            Hash: 987654
        );

        var dict = _serializer.Serialize(original);
        var result = _serializer.Deserialize(dict);

        AssertThat(result).IsInstanceOf<StateHashReport>();
        var typed = (StateHashReport)result;
        AssertThat(typed.PlayerIndex).IsEqual(1);
        AssertThat(typed.Frame).IsEqual(1000L);
        AssertThat(typed.Hash).IsEqual(987654);
    }

    [TestCase]
    public void PlayerReady_RoundTrip()
    {
        var original = new PlayerReady(PlayerIndex: 0, IsReady: true);

        var dict = _serializer.Serialize(original);
        var result = _serializer.Deserialize(dict);

        AssertThat(result).IsInstanceOf<PlayerReady>();
        var typed = (PlayerReady)result;
        AssertThat(typed.PlayerIndex).IsEqual(0);
        AssertThat(typed.IsReady).IsTrue();
    }

    [TestCase]
    public void CardPlayConfirmed_RoundTrip()
    {
        var original = new CardPlayConfirmed(
            Sequence: 5,
            PlayerIndex: 0,
            CardIndex: 2,
            Position: new Vector3(1, 0, 2),
            ServerFrame: 500L,
            SpawnedUnitNetworkId: 101
        );

        var dict = _serializer.Serialize(original);
        var result = _serializer.Deserialize(dict);

        AssertThat(result).IsInstanceOf<CardPlayConfirmed>();
        var typed = (CardPlayConfirmed)result;
        AssertThat(typed.Sequence).IsEqual(5);
        AssertThat(typed.PlayerIndex).IsEqual(0);
        AssertThat(typed.CardIndex).IsEqual(2);
        AssertThat(typed.ServerFrame).IsEqual(500L);
        AssertThat(typed.SpawnedUnitNetworkId).IsEqual(101);
    }

    [TestCase]
    public void CardPlayRejected_RoundTrip()
    {
        var original = new CardPlayRejected(
            Sequence: 10,
            PlayerIndex: 1,
            Reason: "Insufficient mana"
        );

        var dict = _serializer.Serialize(original);
        var result = _serializer.Deserialize(dict);

        AssertThat(result).IsInstanceOf<CardPlayRejected>();
        var typed = (CardPlayRejected)result;
        AssertThat(typed.Sequence).IsEqual(10);
        AssertThat(typed.PlayerIndex).IsEqual(1);
        AssertThat(typed.Reason).IsEqual("Insufficient mana");
    }

    [TestCase]
    public void UnitSpawned_RoundTrip()
    {
        var original = new UnitSpawned(
            NetworkId: 200,
            UnitType: "fire_wisp",
            Team: 0,
            Position: new Vector3(3, 0, 5),
            MatchTick: 100L,
            SourceSequence: 1,
            SourcePlayerIndex: 0,
            SpawnDuration: 1.5f
        );

        var dict = _serializer.Serialize(original);
        var result = _serializer.Deserialize(dict);

        AssertThat(result).IsInstanceOf<UnitSpawned>();
        var typed = (UnitSpawned)result;
        AssertThat(typed.NetworkId).IsEqual(200);
        AssertThat(typed.UnitType).IsEqual("fire_wisp");
        AssertThat(typed.Team).IsEqual(0);
        AssertThat(typed.Position.X).IsEqual(3f);
        AssertThat(typed.Position.Y).IsEqual(0f);
        AssertThat(typed.Position.Z).IsEqual(5f);
        AssertThat(typed.MatchTick).IsEqual(100L);
        AssertThat(typed.SourceSequence.HasValue).IsTrue();
        AssertThat(typed.SourcePlayerIndex.HasValue).IsTrue();
        AssertThat(typed.SourceSequence.GetValueOrDefault()).IsEqual(1);
        AssertThat(typed.SourcePlayerIndex.GetValueOrDefault()).IsEqual(0);
        AssertThat(typed.SpawnDuration).IsEqual(1.5f);
    }

    [TestCase]
    public void UnitDied_RoundTrip_WithKiller()
    {
        var original = new UnitDied(NetworkId: 100, KillerNetworkId: 50);

        var dict = _serializer.Serialize(original);
        var result = _serializer.Deserialize(dict);

        AssertThat(result).IsInstanceOf<UnitDied>();
        var typed = (UnitDied)result;
        AssertThat(typed.NetworkId).IsEqual(100);
        AssertThat(typed.KillerNetworkId.HasValue).IsTrue();
        AssertThat(typed.KillerNetworkId.GetValueOrDefault()).IsEqual(50);
    }

    [TestCase]
    public void UnitDied_RoundTrip_NullKiller()
    {
        var original = new UnitDied(NetworkId: 100, KillerNetworkId: null);

        var dict = _serializer.Serialize(original);
        var result = _serializer.Deserialize(dict);

        AssertThat(result).IsInstanceOf<UnitDied>();
        var typed = (UnitDied)result;
        AssertThat(typed.NetworkId).IsEqual(100);
        AssertThat(typed.KillerNetworkId).IsNull();
    }

    [TestCase]
    public void ProjectileSpawned_RoundTrip()
    {
        var original = new ProjectileSpawned(
            ProjectileId: 99,
            SourceUnitId: 10,
            TargetUnitId: -2,
            Team: 0,
            MovementType: 0,
            CurrentPosition: new Vector3(-4f, 0.5f, 1f),
            Direction: new Vector3(1f, 0f, 0f),
            TargetPosition: new Vector3(5f, 0f, 1f),
            Speed: 9f,
            ProjectileCatalogId: "weaving_bolt",
            Acceleration: 5f,
            MinSpeed: 2f,
            UseSpeedEasing: true,
            SpeedStart: 7f,
            SpeedEnd: 13f,
            SpeedTransitionDuration: 0.75f,
            SpeedEasing: 2,
            SpeedEaseExponent: 2.2f,
            TimeAlive: 0.33f,
            Lifetime: 4.0f
        );

        var dict = _serializer.Serialize(original);
        var result = _serializer.Deserialize(dict);

        AssertThat(result).IsInstanceOf<ProjectileSpawned>();
        var typed = (ProjectileSpawned)result;
        AssertThat(typed.ProjectileId).IsEqual(99);
        AssertThat(typed.ProjectileCatalogId).IsEqual("weaving_bolt");
        AssertThat(typed.CurrentPosition.X).IsEqual(-4f);
        AssertThat(typed.Speed).IsEqual(9f);
        AssertThat(typed.UseSpeedEasing).IsTrue();
        AssertThat(typed.SpeedEnd).IsEqual(13f);
    }

    [TestCase]
    public void ProjectileImpact_RoundTrip()
    {
        var original = new ProjectileImpact(ProjectileId: 88, TargetUnitId: 123);

        var dict = _serializer.Serialize(original);
        var result = _serializer.Deserialize(dict);

        AssertThat(result).IsInstanceOf<ProjectileImpact>();
        var typed = (ProjectileImpact)result;
        AssertThat(typed.ProjectileId).IsEqual(88);
        AssertThat(typed.TargetUnitId).IsEqual(123);
    }

    [TestCase]
    public void ProjectileDespawned_RoundTrip()
    {
        var original = new ProjectileDespawned(ProjectileId: 77);

        var dict = _serializer.Serialize(original);
        var result = _serializer.Deserialize(dict);

        AssertThat(result).IsInstanceOf<ProjectileDespawned>();
        var typed = (ProjectileDespawned)result;
        AssertThat(typed.ProjectileId).IsEqual(77);
    }

    [TestCase]
    public void ProjectileSeedSnapshot_RoundTrip()
    {
        var original = new ProjectileSeedSnapshot(
            Frame: 123L,
            Projectiles:
            [
                new ActiveProjectileSeed(
                    ProjectileId: 99,
                    SourceUnitId: 10,
                    TargetUnitId: -2,
                    Team: 0,
                    MovementType: 0,
                    CurrentPosition: new Vector3(-4f, 0.5f, 1f),
                    Direction: new Vector3(1f, 0f, 0f),
                    TargetPosition: new Vector3(5f, 0f, 1f),
                    Speed: 9f,
                    ProjectileCatalogId: "weaving_bolt")
            ]);

        var dict = _serializer.Serialize(original);
        var result = _serializer.Deserialize(dict);

        AssertThat(result).IsInstanceOf<ProjectileSeedSnapshot>();
        var typed = (ProjectileSeedSnapshot)result;
        AssertThat(typed.Frame).IsEqual(123L);
        AssertThat(typed.Projectiles.Length).IsEqual(1);
        AssertThat(typed.Projectiles[0].ProjectileId).IsEqual(99);
        AssertThat(typed.Projectiles[0].ProjectileCatalogId).IsEqual("weaving_bolt");
    }

    [TestCase]
    public void DamageDealt_RoundTrip()
    {
        var original = new DamageDealt(
            TargetNetworkId: 150,
            Amount: 25.5f,
            IsCrit: true,
            SourceNetworkId: 75
        );

        var dict = _serializer.Serialize(original);
        var result = _serializer.Deserialize(dict);

        AssertThat(result).IsInstanceOf<DamageDealt>();
        var typed = (DamageDealt)result;
        AssertThat(typed.TargetNetworkId).IsEqual(150);
        AssertThat(typed.Amount).IsEqual(25.5f);
        AssertThat(typed.IsCrit).IsTrue();
        AssertThat(typed.SourceNetworkId.HasValue).IsTrue();
        AssertThat(typed.SourceNetworkId.GetValueOrDefault()).IsEqual(75);
    }

    [TestCase]
    public void SummonerDamaged_RoundTrip()
    {
        var original = new SummonerDamaged(Team: 1, Amount: 10f, NewHp: 90f);

        var dict = _serializer.Serialize(original);
        var result = _serializer.Deserialize(dict);

        AssertThat(result).IsInstanceOf<SummonerDamaged>();
        var typed = (SummonerDamaged)result;
        AssertThat(typed.Team).IsEqual(1);
        AssertThat(typed.Amount).IsEqual(10f);
        AssertThat(typed.NewHp).IsEqual(90f);
    }

    [TestCase]
    public void MatchEnded_RoundTrip()
    {
        var original = new MatchEnded(
            WinnerIndex: 0,
            Reason: "Summoner destroyed",
            Duration: 125.5f
        );

        var dict = _serializer.Serialize(original);
        var result = _serializer.Deserialize(dict);

        AssertThat(result).IsInstanceOf<MatchEnded>();
        var typed = (MatchEnded)result;
        AssertThat(typed.WinnerIndex).IsEqual(0);
        AssertThat(typed.Reason).IsEqual("Summoner destroyed");
        AssertThat(typed.Duration).IsEqual(125.5f);
    }

    [TestCase]
    public void MatchStarted_RoundTrip()
    {
        var original = new MatchStarted(
            Seed: 123456789L,
            MatchId: "match-abc-123",
            PlayerIds: new[] { "player-1", "player-2" },
            SummonerIds: new[] { "ignis", "aqua" }
        );

        var dict = _serializer.Serialize(original);
        var result = _serializer.Deserialize(dict);

        AssertThat(result).IsInstanceOf<MatchStarted>();
        var typed = (MatchStarted)result;
        AssertThat(typed.Seed).IsEqual(123456789L);
        AssertThat(typed.MatchId).IsEqual("match-abc-123");
        AssertThat(typed.PlayerIds.Length).IsEqual(2);
        AssertThat(typed.PlayerIds[0]).IsEqual("player-1");
        AssertThat(typed.PlayerIds[1]).IsEqual("player-2");
        AssertThat(typed.SummonerIds[0]).IsEqual("ignis");
    }

    [TestCase]
    public void PlayerInfo_RoundTrip()
    {
        var original = new PlayerInfo(
            PlayerIndex: 0,
            PlayerId: "user-123",
            DisplayName: "TestPlayer",
            SummonerId: "ignis"
        );

        var dict = _serializer.Serialize(original);
        var result = _serializer.Deserialize(dict);

        AssertThat(result).IsInstanceOf<PlayerInfo>();
        var typed = (PlayerInfo)result;
        AssertThat(typed.PlayerIndex).IsEqual(0);
        AssertThat(typed.PlayerId).IsEqual("user-123");
        AssertThat(typed.DisplayName).IsEqual("TestPlayer");
        AssertThat(typed.SummonerId).IsEqual("ignis");
    }

    [TestCase]
    public void Ping_RoundTrip()
    {
        var original = new Ping(Timestamp: 9999999L);

        var dict = _serializer.Serialize(original);
        var result = _serializer.Deserialize(dict);

        AssertThat(result).IsInstanceOf<Ping>();
        var typed = (Ping)result;
        AssertThat(typed.Timestamp).IsEqual(9999999L);
    }

    [TestCase]
    public void Pong_RoundTrip()
    {
        var original = new Pong(
            OriginalTimestamp: 1000L,
            ServerTimestamp: 1005L
        );

        var dict = _serializer.Serialize(original);
        var result = _serializer.Deserialize(dict);

        AssertThat(result).IsInstanceOf<Pong>();
        var typed = (Pong)result;
        AssertThat(typed.OriginalTimestamp).IsEqual(1000L);
        AssertThat(typed.ServerTimestamp).IsEqual(1005L);
    }

    [TestCase]
    public void StateSnapshot_RoundTrip()
    {
        var summoners = new[]
        {
            new SummonerState(Team: 0, Hp: 100, MaxHp: 100, Mana: 5, MaxMana: 10,
                IsCasting: false, CastingTimeRemaining: 0f, CastingTimeTotal: 0f,
                CastingCardIndex: -1, CastingSpawnPosition: Vector3.Zero, CastingNetworkId: -1, CardStateHash: 0,
                Hand: new[] { "fire_wisp", "pebbloom" }, Deck: new[] { "aqua_sprite" }, DiscardPile: System.Array.Empty<string>()),
            new SummonerState(Team: 1, Hp: 80, MaxHp: 100, Mana: 7, MaxMana: 10,
                IsCasting: true, CastingTimeRemaining: 1.5f, CastingTimeTotal: 2f,
                CastingCardIndex: 2, CastingSpawnPosition: new Vector3(3, 0, 3), CastingNetworkId: 5, CardStateHash: 42,
                Hand: new[] { "stone_golem" }, Deck: new[] { "fire_wisp", "pebbloom" }, DiscardPile: new[] { "aqua_sprite" })
        };
        var units = new[]
        {
            new UnitState(NetworkId: 1, Team: 0, Position: new Vector3(1, 0, 1), Hp: 50, MaxHp: 100, TargetNetworkId: 2, IsAlive: true, ActivationState: 1, BehaviorState: 1, IsFacingRight: true),
            new UnitState(NetworkId: 2, Team: 1, Position: new Vector3(5, 0, 5), Hp: 30, MaxHp: 80, TargetNetworkId: null, IsAlive: true, ActivationState: 1, BehaviorState: 3, IsFacingRight: false)
        };
        var projectiles = new[]
        {
            new ProjectileState(
                ProjectileId: 10,
                ProjectileCatalogId: "weaving_bolt",
                SourceUnitId: 1,
                TargetUnitId: 2,
                Team: 0,
                MovementType: 0,
                CurrentPosition: new Vector3(2f, 1f, 2f),
                Direction: new Vector3(1f, 0f, 0f),
                TargetPosition: new Vector3(5f, 0f, 5f),
                Progress: 0.25f,
                Speed: 7f,
                IsDead: false,
                Acceleration: 6f,
                MinSpeed: 2f,
                UseSpeedEasing: true,
                SpeedStart: 5f,
                SpeedEnd: 20f,
                SpeedTransitionDuration: 0.8f,
                SpeedEasing: 1,
                SpeedEaseExponent: 2.25f,
                TimeAlive: 0.3f,
                Lifetime: 3f)
        };

        var original = new StateSnapshot(
            Frame: 1000L,
            MatchTime: 60.5f,
            Phase: 1,
            PrepTimeRemaining: 0f,
            Summoners: summoners,
            Units: units,
            Projectiles: projectiles,
            StateHash: 12345,
            IsOvertime: false
        );

        var dict = _serializer.Serialize(original);
        var result = _serializer.Deserialize(dict);

        AssertThat(result).IsInstanceOf<StateSnapshot>();
        var typed = (StateSnapshot)result;
        AssertThat(typed.Frame).IsEqual(1000L);
        AssertThat(typed.MatchTime).IsEqual(60.5f);
        AssertThat(typed.StateHash).IsEqual(12345);
        AssertThat(typed.Summoners.Length).IsEqual(2);
        AssertThat(typed.Summoners[0].Hp).IsEqual(100);
        AssertThat(typed.Summoners[0].Hand).ContainsExactly(new[] { "fire_wisp", "pebbloom" });
        AssertThat(typed.Summoners[0].Deck).ContainsExactly(new[] { "aqua_sprite" });
        AssertThat(typed.Summoners[0].DiscardPile).IsEmpty();
        AssertThat(typed.Summoners[1].Hp).IsEqual(80);
        AssertThat(typed.Summoners[1].Hand).ContainsExactly(new[] { "stone_golem" });
        AssertThat(typed.Summoners[1].Deck).ContainsExactly(new[] { "fire_wisp", "pebbloom" });
        AssertThat(typed.Summoners[1].DiscardPile).ContainsExactly(new[] { "aqua_sprite" });
        AssertThat(typed.Summoners[1].CastingSpawnPosition.X).IsEqual(3f);
        AssertThat(typed.Summoners[1].CastingSpawnPosition.Z).IsEqual(3f);
        AssertThat(typed.Units.Length).IsEqual(2);
        AssertThat(typed.Units[0].NetworkId).IsEqual(1);
        AssertThat(typed.Units[0].Team).IsEqual(0);
        AssertThat(typed.Units[0].Position.X).IsEqual(1f);
        AssertThat(typed.Units[0].Position.Z).IsEqual(1f);
        AssertThat(typed.Units[1].NetworkId).IsEqual(2);
        AssertThat(typed.Units[1].Team).IsEqual(1);
        AssertThat(typed.Units[1].Position.X).IsEqual(5f);
        AssertThat(typed.Units[1].Position.Z).IsEqual(5f);
        AssertThat(typed.Units[0].TargetNetworkId.HasValue).IsTrue();
        AssertThat(typed.Units[0].TargetNetworkId.GetValueOrDefault()).IsEqual(2);
        AssertThat(typed.Units[1].TargetNetworkId).IsNull();
        AssertThat(typed.Units[0].BehaviorState).IsEqual(1);
        AssertThat(typed.Units[0].IsFacingRight).IsTrue();
        AssertThat(typed.Units[1].BehaviorState).IsEqual(3);
        AssertThat(typed.Units[1].IsFacingRight).IsFalse();
        AssertThat(typed.Projectiles.Length).IsEqual(0);
    }

    [TestCase]
    public void GetMessageType_ReturnsCorrectType()
    {
        var request = new CardPlayRequest(1, 0, 2, Vector3.Zero, 0);
        var dict = _serializer.Serialize(request);

        var type = _serializer.GetMessageType(dict);

        AssertThat(type).IsEqual(MessageType.CardPlayRequest);
    }

    [TestCase]
    public void Serialize_ThrowsForUnknownType()
    {
        var unknownObj = (IProtocolMessage)new UnknownProtocolMessage();

        AssertThrown(() => _serializer.Serialize(unknownObj))
            .IsInstanceOf<ArgumentException>();
    }

    [TestCase]
    public void Vector3_SurvivesJsonRoundTrip()
    {
        var original = new UnitSpawned(
            NetworkId: 10,
            UnitType: "test_unit",
            Team: 1,
            Position: new Vector3(7.25f, 0.5f, -3.75f),
            MatchTick: 50L,
            SourceSequence: 2,
            SourcePlayerIndex: 1,
            SpawnDuration: 2.0f
        );

        // Serialize → JSON string → parse back → Deserialize (full transport path)
        var dict = _serializer.Serialize(original);
        var json = Json.Stringify(dict);
        var parsed = Json.ParseString(json).AsGodotDictionary();
        var result = (UnitSpawned)_serializer.Deserialize(parsed);

        AssertThat(result.NetworkId).IsEqual(10);
        AssertThat(result.UnitType).IsEqual("test_unit");
        AssertThat(result.Team).IsEqual(1);
        AssertThat(result.Position.X).IsEqual(7.25f);
        AssertThat(result.Position.Y).IsEqual(0.5f);
        AssertThat(result.Position.Z).IsEqual(-3.75f);
        AssertThat(result.MatchTick).IsEqual(50L);
        AssertThat(result.SpawnDuration).IsEqual(2.0f);
    }

    [TestCase]
    public void StateSnapshot_Vector3_SurvivesJsonRoundTrip()
    {
        var summoners = new[]
        {
            new SummonerState(Team: 0, Hp: 100, MaxHp: 100, Mana: 5, MaxMana: 10,
                IsCasting: false, CastingTimeRemaining: 0f, CastingTimeTotal: 0f,
                CastingCardIndex: -1, CastingSpawnPosition: Vector3.Zero, CastingNetworkId: -1, CardStateHash: 0,
                Hand: System.Array.Empty<string>(), Deck: System.Array.Empty<string>(), DiscardPile: System.Array.Empty<string>())
        };
        var units = new[]
        {
            new UnitState(NetworkId: 1, Team: 0, Position: new Vector3(4.5f, 0, 8.25f), Hp: 50, MaxHp: 100, TargetNetworkId: null, IsAlive: true, ActivationState: 1, BehaviorState: 0, IsFacingRight: true)
        };
        var projectiles = new[]
        {
            new ProjectileState(
                ProjectileId: 4,
                ProjectileCatalogId: "mana_bolt",
                SourceUnitId: 1,
                TargetUnitId: -2,
                Team: 0,
                MovementType: 0,
                CurrentPosition: new Vector3(1.5f, 0.2f, -2.25f),
                Direction: new Vector3(0f, 0f, 1f),
                TargetPosition: new Vector3(1.5f, 0.2f, 4f),
                Progress: 0.6f,
                Speed: 12f,
                IsDead: false,
                Acceleration: -3f,
                MinSpeed: 4f,
                UseSpeedEasing: false,
                SpeedStart: 12f,
                SpeedEnd: 12f,
                SpeedTransitionDuration: 1f,
                SpeedEasing: 0,
                SpeedEaseExponent: 2f,
                TimeAlive: 1.2f,
                Lifetime: 2.5f)
        };

        var original = new StateSnapshot(
            Frame: 100L, MatchTime: 10f, Phase: 1, PrepTimeRemaining: 0f,
            Summoners: summoners, Units: units, Projectiles: projectiles, StateHash: 999, IsOvertime: false
        );

        var dict = _serializer.Serialize(original);
        var json = Json.Stringify(dict);
        var parsed = Json.ParseString(json).AsGodotDictionary();
        var result = (StateSnapshot)_serializer.Deserialize(parsed);

        AssertThat(result.Units[0].Position.X).IsEqual(4.5f);
        AssertThat(result.Units[0].Position.Y).IsEqual(0f);
        AssertThat(result.Units[0].Position.Z).IsEqual(8.25f);
        AssertThat(result.Projectiles.Length).IsEqual(0);
    }
}

public readonly record struct UnknownProtocolMessage : IProtocolMessage;
