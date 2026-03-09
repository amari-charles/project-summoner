namespace Fateforged.Tests.Session;

using System;
using System.Collections.Generic;
using Fateforged.Multiplayer.Protocol;
using Fateforged.Multiplayer.Transport;
using Fateforged.Projectiles;
using Fateforged.Session;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Tests.Simulation;
using Fateforged.Units;
using GdUnit4;
using Godot;
using Godot.Collections;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class NetworkSessionWiringTest
{
    [TestCase]
    public void ClientSession_SubmitPlayCard_SendsCardPlayRequest()
    {
        var state = SimTestHelper.CreateBattleState();
        var transport = new FakeTransport(isHost: false);
        var serializer = new MessageSerializer();
        var session = new ClientSession(state, transport, localPlayerIndex: 1);

        var cmd = new PlayCardCommand(team: 1, cardIndex: 2, spawnPosition: new SimVector3(3f, 0f, -4f));
        session.SubmitCommand(cmd);

        AssertThat(transport.SentMessages.Count).IsEqual(1);
        var messageType = serializer.GetMessageType(transport.SentMessages[0]);
        AssertThat((int)messageType).IsEqual((int)MessageType.CardPlayRequest);
    }

    [TestCase]
    public void ClientSession_ReceivesSnapshot_UpdatesLocalState_AndFiresFirstSnapshot()
    {
        var state = SimTestHelper.CreateBattleState();
        var transport = new FakeTransport(isHost: false);
        var serializer = new MessageSerializer();
        var session = new ClientSession(state, transport, localPlayerIndex: 1);

        bool firstSnapshotFired = false;
        session.FirstSnapshotApplied += () => firstSnapshotFired = true;

        var snapshot = new StateSnapshot(
            Frame: 42,
            MatchTime: 12.5f,
            Phase: (int)GamePhase.Battle,
            PrepTimeRemaining: 0f,
            Summoners:
            [
                new SummonerState(0, 95f, 100f, 8f, 10f, false, 0f, 0f, -1, Vector3.Zero, -1, 0, System.Array.Empty<SimCardCatalogId>(), System.Array.Empty<SimCardCatalogId>(), System.Array.Empty<SimCardCatalogId>()),
                new SummonerState(1, 90f, 100f, 7f, 10f, false, 0f, 0f, -1, Vector3.Zero, -1, 0, System.Array.Empty<SimCardCatalogId>(), System.Array.Empty<SimCardCatalogId>(), System.Array.Empty<SimCardCatalogId>(), "fire_wisp")
            ],
            Units:
            [
                new UnitState(10, 0, new Vector3(-5f, 0f, 0f), 100f, 100f, null, true, (int)ActivationState.Active, (int)BehaviorState.NoTarget, true)
            ],
            Projectiles: System.Array.Empty<ProjectileState>(),
            StateHash: 0,
            IsOvertime: false
        );

        transport.EmitMessage(1, serializer.Serialize(snapshot));
        session.Tick(0.016f);

        AssertThat(firstSnapshotFired).IsTrue();
        AssertThat(state.FrameNumber).IsEqual(42);
        AssertThat(state.Units.ContainsKey(10)).IsTrue();
        AssertThat(state.Units[10].Position.X).IsEqual(-5f);
        AssertThat(state.Summoners[1].CastingCatalogId.Value).IsEqual("fire_wisp");
        AssertThat(state.Projectiles.Count).IsEqual(0);
    }

    [TestCase]
    public void ClientSession_ProjectileLifecycleMessages_UpdateLocalProjectileState()
    {
        var state = SimTestHelper.CreateBattleState();
        var transport = new FakeTransport(isHost: false);
        var serializer = new MessageSerializer();
        var session = new ClientSession(state, transport, localPlayerIndex: 1);

        var spawned = new ProjectileSpawned(
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
            Lifetime: 4.0f,
            HitRadius: 0.9f,
            HitSpace: ProjectileHitSpace.Sphere3D
        );

        transport.EmitMessage(1, serializer.Serialize(spawned));
        session.Tick(0.016f);

        AssertThat(state.Projectiles.ContainsKey(99)).IsTrue();
        AssertThat(state.Projectiles[99].ProjectileCatalogId.Value).IsEqual("weaving_bolt");
        AssertThat(state.Projectiles[99].Acceleration).IsEqual(5f);
        AssertThat(state.Projectiles[99].HitRadius).IsEqual(0.9f);
        AssertThat(state.Projectiles[99].HitSpace).IsEqual(ProjectileHitSpace.Sphere3D);

        transport.EmitMessage(1, serializer.Serialize(new ProjectileImpact(99, 10)));
        session.Tick(0.016f);

        AssertThat(state.Projectiles.ContainsKey(99)).IsFalse();
    }

    [TestCase]
    public void ClientSession_ProjectileSeedSnapshot_RebuildsProjectileVisualState()
    {
        var state = SimTestHelper.CreateBattleState();
        state.Projectiles[1] = new SimProjectileData { ProjectileId = 1 };

        var transport = new FakeTransport(isHost: false);
        var serializer = new MessageSerializer();
        var session = new ClientSession(state, transport, localPlayerIndex: 1);

        var seed = new ProjectileSeedSnapshot(
            Frame: 50,
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
                    ProjectileCatalogId: "weaving_bolt",
                    HitRadius: 1.1f,
                    HitSpace: ProjectileHitSpace.Sphere3D)
            ]);

        transport.EmitMessage(1, serializer.Serialize(seed));
        session.Tick(0.016f);

        AssertThat(state.Projectiles.ContainsKey(1)).IsFalse();
        AssertThat(state.Projectiles.ContainsKey(99)).IsTrue();
        AssertThat(state.Projectiles[99].ProjectileCatalogId.Value).IsEqual("weaving_bolt");
        AssertThat(state.Projectiles[99].HitRadius).IsEqual(1.1f);
        AssertThat(state.Projectiles[99].HitSpace).IsEqual(ProjectileHitSpace.Sphere3D);
    }

    [TestCase]
    public void ClientSession_ProjectileSeedSnapshot_WeavingProjectile_RestoresElapsedPhase()
    {
        var state = SimTestHelper.CreateBattleState();
        var transport = new FakeTransport(isHost: false);
        var serializer = new MessageSerializer();
        var session = new ClientSession(state, transport, localPlayerIndex: 1);

        var seed = new ProjectileSeedSnapshot(
            Frame: 50,
            Projectiles:
            [
                new ActiveProjectileSeed(
                    ProjectileId: 99,
                    SourceUnitId: 10,
                    TargetUnitId: -2,
                    Team: 0,
                    MovementType: (int)ProjectileMovementType.WeavingHoming,
                    CurrentPosition: new Vector3(-4f, 0.5f, 1f),
                    Direction: new Vector3(1f, 0f, 0f),
                    TargetPosition: new Vector3(5f, 0f, 1f),
                    Speed: 9f,
                    ProjectileCatalogId: "weaving_bolt",
                    TimeAlive: 2.0f,
                    Lifetime: 4.0f)
            ]);

        transport.EmitMessage(1, serializer.Serialize(seed));
        session.Tick(0f);

        AssertThat(state.Projectiles.ContainsKey(99)).IsTrue();
        var projectile = state.Projectiles[99];
        AssertThat(projectile.WeavingPhase).IsEqual(WeavingPhase.Homing);
        AssertThat(projectile.PhaseTimer).IsGreater(0.8f);
    }

    [TestCase]
    public void ClientSession_SpellCastVisualMessage_EmitsSpellCastEvent()
    {
        var state = SimTestHelper.CreateBattleState();
        var transport = new FakeTransport(isHost: false);
        var serializer = new MessageSerializer();
        var session = new ClientSession(state, transport, localPlayerIndex: 1);

        SpellCastEvent? seenEvent = null;
        session.SimEventsEmitted += events =>
        {
            foreach (var evt in events)
            {
                if (evt is SpellCastEvent spellCast)
                    seenEvent = spellCast;
            }
        };

        transport.EmitMessage(1, serializer.Serialize(new SpellCastVisual(
            Team: 0,
            CatalogId: "fireball",
            Position: new Vector3(3f, 0f, -1f))));
        session.Tick(0.016f);

        AssertThat(seenEvent != null).IsTrue();
        AssertThat(seenEvent?.CatalogId.Value ?? "").IsEqual("fireball");
        AssertThat(seenEvent?.Position.X ?? 0f).IsEqual(3f);
        AssertThat(seenEvent?.Position.Z ?? 0f).IsEqual(-1f);
    }

    [TestCase]
    public void HostSession_RemoteCardPlayRequest_QueuesValidatedCommand()
    {
        var state = SimTestHelper.CreateBattleState();
        state.CardDataMap["test_unit"] = SimTestHelper.CreateSummonCard("test_unit");
        state.Summoners[1].Hand.Add("test_unit");
        state.Summoners[1].Mana = 10f;

        var simulation = new Fateforged.Simulation.Simulation(state);
        var router = new CommandRouter();
        var transport = new FakeTransport(isHost: true);
        var serializer = new MessageSerializer();
        var session = new HostSession(simulation, router, state, transport);

        var request = new CardPlayRequest(1, 1, 0, new Vector3(2f, 0f, 0f), 0);
        transport.EmitMessage(2, serializer.Serialize(request));

        AssertThat(state.PendingCommandBuffer.Count).IsEqual(1);
        AssertThat(state.PendingCommandBuffer[0]).IsInstanceOf<PlayCardCommand>();
        AssertThat(((PlayCardCommand)state.PendingCommandBuffer[0]).Team).IsEqual(1);
    }

    [TestCase]
    public void HostSession_RemoteCardPlayRequest_IgnoresSpoofedPlayerIndex()
    {
        var state = SimTestHelper.CreateBattleState();
        state.CardDataMap["test_unit"] = SimTestHelper.CreateSummonCard("test_unit");
        state.Summoners[1].Hand.Add("test_unit");
        state.Summoners[1].Mana = 10f;

        var simulation = new Fateforged.Simulation.Simulation(state);
        var router = new CommandRouter();
        var transport = new FakeTransport(isHost: true);
        var serializer = new MessageSerializer();
        var session = new HostSession(simulation, router, state, transport);

        // Sender is remote peer 2 but payload claims team 0.
        var request = new CardPlayRequest(1, 0, 0, new Vector3(2f, 0f, 0f), 0);
        transport.EmitMessage(2, serializer.Serialize(request));

        AssertThat(state.PendingCommandBuffer.Count).IsEqual(1);
        AssertThat(state.PendingCommandBuffer[0]).IsInstanceOf<PlayCardCommand>();
        AssertThat(((PlayCardCommand)state.PendingCommandBuffer[0]).Team).IsEqual(1);
    }

    [TestCase]
    public void HostSession_LocalPeerMessage_IsRejected()
    {
        var state = SimTestHelper.CreateBattleState();
        state.CardDataMap["test_unit"] = SimTestHelper.CreateSummonCard("test_unit");
        state.Summoners[1].Hand.Add("test_unit");
        state.Summoners[1].Mana = 10f;

        var simulation = new Fateforged.Simulation.Simulation(state);
        var router = new CommandRouter();
        var transport = new FakeTransport(isHost: true);
        var serializer = new MessageSerializer();
        var session = new HostSession(simulation, router, state, transport);

        var request = new CardPlayRequest(1, 0, 0, new Vector3(2f, 0f, 0f), 0);
        // Host local peer ID is 1 in this fake transport.
        transport.EmitMessage(1, serializer.Serialize(request));

        AssertThat(state.PendingCommandBuffer.Count).IsEqual(0);
    }

    [TestCase]
    public void HostSession_Tick_BroadcastsSnapshots_AndMatchEnd()
    {
        var state = SimTestHelper.CreateBattleState();
        var simulation = new Fateforged.Simulation.Simulation(state);
        var router = new CommandRouter();
        var transport = new FakeTransport(isHost: true);
        var serializer = new MessageSerializer();
        var session = new HostSession(simulation, router, state, transport);

        session.SubmitCommand(new ForfeitCommand(0));
        session.Tick(0.016f);
        session.Tick(0.12f);

        bool sawMatchEnded = false;
        bool sawSnapshot = false;
        foreach (var message in transport.BroadcastMessages)
        {
            var type = serializer.GetMessageType(message);
            if (type == MessageType.MatchEnded) sawMatchEnded = true;
            if (type == MessageType.StateSnapshot) sawSnapshot = true;
        }

        AssertThat(sawMatchEnded).IsTrue();
        AssertThat(sawSnapshot).IsTrue();
    }

    [TestCase]
    public void HostSession_PeerConnected_SendsProjectileSeedSnapshot()
    {
        var state = SimTestHelper.CreateBattleState();
        state.Projectiles[99] = new SimProjectileData
        {
            ProjectileId = 99,
            ProjectileCatalogId = "weaving_bolt",
            SourceUnitId = 10,
            TargetUnitId = -2,
            Team = Team.Player,
            MovementType = ProjectileMovementType.Straight,
            CurrentPosition = new SimVector3(-4f, 0.5f, 1f),
            Direction = new SimVector3(1f, 0f, 0f),
            TargetPosition = new SimVector3(5f, 0f, 1f),
            Speed = 9f,
            Lifetime = 4f,
            HitRadius = 1.3f,
            HitSpace = ProjectileHitSpace.Sphere3D
        };

        var simulation = new Fateforged.Simulation.Simulation(state);
        var router = new CommandRouter();
        var transport = new FakeTransport(isHost: true);
        var serializer = new MessageSerializer();
        var session = new HostSession(simulation, router, state, transport);

        transport.EmitPeerConnected(peerId: 2);

        AssertThat(transport.DirectMessages.Count).IsEqual(1);
        var (_, msg) = transport.DirectMessages[0];
        var type = serializer.GetMessageType(msg);
        AssertThat(type).IsEqual(MessageType.ProjectileSeedSnapshot);

        var seed = (ProjectileSeedSnapshot)serializer.Deserialize(msg);
        AssertThat(seed.Projectiles.Length).IsEqual(1);
        AssertThat(seed.Projectiles[0].ProjectileId).IsEqual(99);
        AssertThat(seed.Projectiles[0].ProjectileCatalogId.Value).IsEqual("weaving_bolt");
        AssertThat(seed.Projectiles[0].HitRadius).IsEqual(1.3f);
        AssertThat(seed.Projectiles[0].HitSpace).IsEqual(ProjectileHitSpace.Sphere3D);
    }

    [TestCase]
    public void ProjectileSpawned_VeerDirections_RoundTripThroughSerializer()
    {
        var serializer = new MessageSerializer();
        var original = new ProjectileSpawned(
            ProjectileId: 42,
            SourceUnitId: 1,
            TargetUnitId: 2,
            Team: 0,
            MovementType: (int)ProjectileMovementType.WeavingHoming,
            CurrentPosition: new Vector3(0f, 0f, 0f),
            Direction: new Vector3(1f, 0f, 0f),
            TargetPosition: new Vector3(10f, 0f, 0f),
            Speed: 28f,
            VeerDirection: new Vector3(0.7f, 0.1f, 0.5f),
            CounterVeerDirection: new Vector3(-0.3f, -0.05f, 0.8f));

        var dict = serializer.Serialize(original);
        var deserialized = (ProjectileSpawned)serializer.Deserialize(dict);

        AssertThat(deserialized.ProjectileId).IsEqual(42);
        AssertThat(deserialized.VeerDirection.X).IsEqualApprox(0.7f, 0.01f);
        AssertThat(deserialized.VeerDirection.Y).IsEqualApprox(0.1f, 0.01f);
        AssertThat(deserialized.VeerDirection.Z).IsEqualApprox(0.5f, 0.01f);
        AssertThat(deserialized.CounterVeerDirection.X).IsEqualApprox(-0.3f, 0.01f);
        AssertThat(deserialized.CounterVeerDirection.Y).IsEqualApprox(-0.05f, 0.01f);
        AssertThat(deserialized.CounterVeerDirection.Z).IsEqualApprox(0.8f, 0.01f);
    }

    [TestCase]
    public void ProjectileSeedSnapshot_VeerDirections_RoundTripThroughSerializer()
    {
        var state = SimTestHelper.CreateBattleState();
        state.Projectiles[77] = new SimProjectileData
        {
            ProjectileId = 77,
            ProjectileCatalogId = "weaving_bolt",
            SourceUnitId = 10,
            TargetUnitId = -2,
            Team = Team.Player,
            MovementType = ProjectileMovementType.WeavingHoming,
            CurrentPosition = new SimVector3(0f, 0f, 0f),
            Direction = new SimVector3(1f, 0f, 0f),
            TargetPosition = new SimVector3(5f, 0f, 1f),
            Speed = 28f,
            Lifetime = 4f,
            VeerDirection = new SimVector3(0.6f, 0.2f, -0.4f),
            CounterVeerDirection = new SimVector3(-0.5f, 0.1f, 0.7f)
        };

        var simulation = new Fateforged.Simulation.Simulation(state);
        var router = new CommandRouter();
        var transport = new FakeTransport(isHost: true);
        var serializer = new MessageSerializer();
        var session = new HostSession(simulation, router, state, transport);

        transport.EmitPeerConnected(peerId: 2);

        var (_, msg) = transport.DirectMessages[0];
        var seed = (ProjectileSeedSnapshot)serializer.Deserialize(msg);
        var proj = seed.Projectiles[0];

        AssertThat(proj.VeerDirection.X).IsEqualApprox(0.6f, 0.01f);
        AssertThat(proj.VeerDirection.Y).IsEqualApprox(0.2f, 0.01f);
        AssertThat(proj.VeerDirection.Z).IsEqualApprox(-0.4f, 0.01f);
        AssertThat(proj.CounterVeerDirection.X).IsEqualApprox(-0.5f, 0.01f);
        AssertThat(proj.CounterVeerDirection.Y).IsEqualApprox(0.1f, 0.01f);
        AssertThat(proj.CounterVeerDirection.Z).IsEqualApprox(0.7f, 0.01f);
    }

    [TestCase]
    public void ClientSession_ReconnectTimeout_PeerDisconnect_LocalWins()
    {
        var state = SimTestHelper.CreateBattleState();
        state.Phase = GamePhase.Battle;
        var transport = new FakeTransport(isHost: false);
        var session = new ClientSession(state, transport, localPlayerIndex: 1);

        transport.EmitPeerDisconnected(peerId: 1);
        session.Tick(31f);

        AssertThat(state.Phase).IsEqual(GamePhase.GameOver);
        AssertThat(state.WinnerTeam.HasValue).IsTrue();
        AssertThat(state.WinnerTeam ?? -1).IsEqual(1);
    }

    [TestCase]
    public void ClientSession_ReconnectTimeout_LocalSocketDisconnect_LocalLoses()
    {
        var state = SimTestHelper.CreateBattleState();
        state.Phase = GamePhase.Battle;
        var transport = new FakeTransport(isHost: false);
        var session = new ClientSession(state, transport, localPlayerIndex: 1);

        transport.EmitDisconnected("Socket disconnected");
        session.Tick(31f);

        AssertThat(state.Phase).IsEqual(GamePhase.GameOver);
        AssertThat(state.WinnerTeam.HasValue).IsTrue();
        AssertThat(state.WinnerTeam ?? -1).IsEqual(0);
    }

    [TestCase]
    public void HostSession_ReconnectTimeout_LocalSocketDisconnect_HostLoses()
    {
        var state = SimTestHelper.CreateBattleState();
        state.Phase = GamePhase.Battle;
        var simulation = new Fateforged.Simulation.Simulation(state);
        var router = new CommandRouter();
        var transport = new FakeTransport(isHost: true);
        var session = new HostSession(simulation, router, state, transport);

        transport.EmitDisconnected("Socket disconnected");
        session.Tick(31f);

        AssertThat(state.Phase).IsEqual(GamePhase.GameOver);
        AssertThat(state.WinnerTeam.HasValue).IsTrue();
        AssertThat(state.WinnerTeam ?? -1).IsEqual(1);
    }

    [TestCase]
    public void HostSession_ReconnectTimeout_PeerDisconnect_HostWins()
    {
        var state = SimTestHelper.CreateBattleState();
        state.Phase = GamePhase.Battle;
        var simulation = new Fateforged.Simulation.Simulation(state);
        var router = new CommandRouter();
        var transport = new FakeTransport(isHost: true);
        var session = new HostSession(simulation, router, state, transport);

        transport.EmitPeerDisconnected(peerId: 2);
        session.Tick(31f);

        AssertThat(state.Phase).IsEqual(GamePhase.GameOver);
        AssertThat(state.WinnerTeam.HasValue).IsTrue();
        AssertThat(state.WinnerTeam ?? -1).IsEqual(0);
    }

#pragma warning disable CS0067
    private sealed class FakeTransport : IMatchTransport
    {
        public FakeTransport(bool isHost)
        {
            IsHost = isHost;
        }

        public bool IsConnected { get; private set; } = true;
        public int LocalPeerId => IsHost ? 1 : 2;
        public bool IsHost { get; }

        public readonly List<Dictionary> SentMessages = new();
        public readonly List<Dictionary> BroadcastMessages = new();
        public readonly List<(int peerId, Dictionary message)> DirectMessages = new();

        public event Action<int, Dictionary>? OnMessageReceived;
        public event Action<int>? OnPeerConnected;
        public event Action<int>? OnPeerDisconnected;
        public event Action? OnConnected;
        public event Action<string>? OnDisconnected;

        public void Send(Dictionary message) => SentMessages.Add(message);
        public void Broadcast(Dictionary message) => BroadcastMessages.Add(message);
        public void SendTo(int peerId, Dictionary message) => DirectMessages.Add((peerId, message));
        public void Host(int port) => IsConnected = true;
        public void Connect(string address, int port) => IsConnected = true;
        public void Disconnect()
        {
            IsConnected = false;
            OnDisconnected?.Invoke("Disconnected");
        }

        public void EmitMessage(int senderId, Dictionary message)
        {
            OnMessageReceived?.Invoke(senderId, message);
        }

        public void EmitPeerDisconnected(int peerId)
        {
            OnPeerDisconnected?.Invoke(peerId);
        }

        public void EmitPeerConnected(int peerId)
        {
            OnPeerConnected?.Invoke(peerId);
        }

        public void EmitDisconnected(string reason)
        {
            IsConnected = false;
            OnDisconnected?.Invoke(reason);
        }
    }
#pragma warning restore CS0067
}
