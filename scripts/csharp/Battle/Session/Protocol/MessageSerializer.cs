using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace Fateforged.Multiplayer.Protocol;

/// <summary>
/// Serializes and deserializes protocol messages for network transmission.
/// Uses Godot Dictionary for RPC compatibility.
/// </summary>
public class MessageSerializer
{
    /// <summary>
    /// Serialize a message to a Dictionary for RPC transmission.
    /// </summary>
    public Dictionary Serialize(object message)
    {
        var dict = new Dictionary();

        switch (message)
        {
            case CardPlayRequest m:
                dict["type"] = (int)MessageType.CardPlayRequest;
                dict["seq"] = m.Sequence;
                dict["player"] = m.PlayerIndex;
                dict["card"] = m.CardIndex;
                dict["pos"] = SerializeVector3(m.Position);
                dict["ts"] = m.ClientTimestamp;
                break;

            case ForfeitRequest m:
                dict["type"] = (int)MessageType.ForfeitRequest;
                dict["player"] = m.PlayerIndex;
                break;

            case StateHashReport m:
                dict["type"] = (int)MessageType.StateHashReport;
                dict["player"] = m.PlayerIndex;
                dict["frame"] = m.Frame;
                dict["hash"] = m.Hash;
                break;

            case PlayerReady m:
                dict["type"] = (int)MessageType.PlayerReady;
                dict["player"] = m.PlayerIndex;
                dict["ready"] = m.IsReady;
                break;

            case CardPlayConfirmed m:
                dict["type"] = (int)MessageType.CardPlayConfirmed;
                dict["seq"] = m.Sequence;
                dict["player"] = m.PlayerIndex;
                dict["card"] = m.CardIndex;
                dict["pos"] = SerializeVector3(m.Position);
                dict["frame"] = m.ServerFrame;
                dict["unitId"] = m.SpawnedUnitNetworkId;
                break;

            case CardPlayRejected m:
                dict["type"] = (int)MessageType.CardPlayRejected;
                dict["seq"] = m.Sequence;
                dict["player"] = m.PlayerIndex;
                dict["reason"] = m.Reason;
                break;

            case StateSnapshot m:
                dict["type"] = (int)MessageType.StateSnapshot;
                dict["frame"] = m.Frame;
                dict["time"] = m.MatchTime;
                dict["phase"] = m.Phase;
                dict["prepTime"] = m.PrepTimeRemaining;
                dict["summoners"] = SerializeSummoners(m.Summoners);
                dict["units"] = SerializeUnits(m.Units);
                dict["projectiles"] = SerializeProjectiles(m.Projectiles);
                dict["hash"] = m.StateHash;
                dict["overtime"] = m.IsOvertime;
                break;

            case UnitSpawned m:
                dict["type"] = (int)MessageType.UnitSpawned;
                dict["id"] = m.NetworkId;
                dict["unitType"] = m.UnitType;
                dict["team"] = m.Team;
                dict["pos"] = SerializeVector3(m.Position);
                dict["tick"] = m.MatchTick;
                dict["srcSeq"] = m.SourceSequence ?? -1;
                dict["srcPlayer"] = m.SourcePlayerIndex ?? -1;
                dict["spawnDur"] = m.SpawnDuration;
                break;

            case UnitDied m:
                dict["type"] = (int)MessageType.UnitDied;
                dict["id"] = m.NetworkId;
                dict["killer"] = m.KillerNetworkId ?? -1;
                break;

            case DamageDealt m:
                dict["type"] = (int)MessageType.DamageDealt;
                dict["target"] = m.TargetNetworkId;
                dict["amount"] = m.Amount;
                dict["crit"] = m.IsCrit;
                dict["source"] = m.SourceNetworkId ?? -1;
                break;

            case SummonerDamaged m:
                dict["type"] = (int)MessageType.SummonerDamaged;
                dict["team"] = m.Team;
                dict["amount"] = m.Amount;
                dict["hp"] = m.NewHp;
                break;

            case SummonerDamageFlash m:
                dict["type"] = (int)MessageType.SummonerDamageFlash;
                dict["team"] = m.Team;
                dict["damage"] = m.Damage;
                dict["attacker"] = m.AttackerUnitId;
                break;

            case SummonerDestroyed m:
                dict["type"] = (int)MessageType.SummonerDestroyed;
                dict["team"] = m.Team;
                dict["killer"] = m.KillerUnitId;
                break;

            case MatchEnded m:
                dict["type"] = (int)MessageType.MatchEnded;
                dict["winner"] = m.WinnerIndex;
                dict["reason"] = m.Reason;
                dict["duration"] = m.Duration;
                break;

            case MatchStarted m:
                dict["type"] = (int)MessageType.MatchStarted;
                dict["seed"] = m.Seed;
                dict["matchId"] = m.MatchId;
                dict["players"] = ToGodotArray(m.PlayerIds);
                dict["summoners"] = ToGodotArray(m.SummonerIds);
                break;

            case PlayerInfo m:
                dict["type"] = (int)MessageType.PlayerInfo;
                dict["player"] = m.PlayerIndex;
                dict["playerId"] = m.PlayerId;
                dict["name"] = m.DisplayName;
                dict["summoner"] = m.SummonerId;
                break;

            case Ping m:
                dict["type"] = (int)MessageType.Ping;
                dict["ts"] = m.Timestamp;
                break;

            case Pong m:
                dict["type"] = (int)MessageType.Pong;
                dict["origTs"] = m.OriginalTimestamp;
                dict["serverTs"] = m.ServerTimestamp;
                break;

            default:
                throw new ArgumentException($"Unknown message type: {message.GetType()}");
        }

        return dict;
    }

    /// <summary>
    /// Deserialize a Dictionary back to a typed message.
    /// </summary>
    public object Deserialize(Dictionary dict)
    {
        var type = (MessageType)(int)dict["type"];

        return type switch
        {
            MessageType.CardPlayRequest => new CardPlayRequest(
                (int)dict["seq"],
                (int)dict["player"],
                (int)dict["card"],
                DeserializeVector3(dict["pos"]),
                (long)dict["ts"]
            ),

            MessageType.ForfeitRequest => new ForfeitRequest((int)dict["player"]),

            MessageType.StateHashReport => new StateHashReport(
                (int)dict["player"],
                (long)dict["frame"],
                (int)dict["hash"]
            ),

            MessageType.PlayerReady => new PlayerReady(
                (int)dict["player"],
                (bool)dict["ready"]
            ),

            MessageType.CardPlayConfirmed => new CardPlayConfirmed(
                (int)dict["seq"],
                (int)dict["player"],
                (int)dict["card"],
                DeserializeVector3(dict["pos"]),
                (long)dict["frame"],
                (int)dict["unitId"]
            ),

            MessageType.CardPlayRejected => new CardPlayRejected(
                (int)dict["seq"],
                (int)dict["player"],
                (string)dict["reason"]
            ),

            MessageType.StateSnapshot => new StateSnapshot(
                (long)dict["frame"],
                (float)dict["time"],
                (int)dict["phase"],
                (float)dict["prepTime"],
                DeserializeSummoners((Godot.Collections.Array)dict["summoners"]),
                DeserializeUnits((Godot.Collections.Array)dict["units"]),
                dict.ContainsKey("projectiles")
                    ? DeserializeProjectiles((Godot.Collections.Array)dict["projectiles"])
                    : System.Array.Empty<ProjectileState>(),
                (int)dict["hash"],
                (bool)dict["overtime"]
            ),

            MessageType.UnitSpawned => new UnitSpawned(
                (int)dict["id"],
                (string)dict["unitType"],
                (int)dict["team"],
                DeserializeVector3(dict["pos"]),
                (long)dict["tick"],
                (int)dict["srcSeq"] == -1 ? null : (int)dict["srcSeq"],
                (int)dict["srcPlayer"] == -1 ? null : (int)dict["srcPlayer"],
                dict.ContainsKey("spawnDur") ? (float)dict["spawnDur"] : 0f
            ),

            MessageType.UnitDied => new UnitDied(
                (int)dict["id"],
                (int)dict["killer"] == -1 ? null : (int)dict["killer"]
            ),

            MessageType.DamageDealt => new DamageDealt(
                (int)dict["target"],
                (float)dict["amount"],
                (bool)dict["crit"],
                (int)dict["source"] == -1 ? null : (int)dict["source"]
            ),

            MessageType.SummonerDamaged => new SummonerDamaged(
                (int)dict["team"],
                (float)dict["amount"],
                (float)dict["hp"]
            ),

            MessageType.SummonerDamageFlash => new SummonerDamageFlash(
                (int)dict["team"],
                (float)dict["damage"],
                (int)dict["attacker"]
            ),

            MessageType.SummonerDestroyed => new SummonerDestroyed(
                (int)dict["team"],
                (int)dict["killer"]
            ),

            MessageType.MatchEnded => new MatchEnded(
                (int)dict["winner"],
                (string)dict["reason"],
                (float)dict["duration"]
            ),

            MessageType.MatchStarted => new MatchStarted(
                (long)dict["seed"],
                (string)dict["matchId"],
                ToStringArray((Godot.Collections.Array)dict["players"]),
                ToStringArray((Godot.Collections.Array)dict["summoners"])
            ),

            MessageType.PlayerInfo => new PlayerInfo(
                (int)dict["player"],
                (string)dict["playerId"],
                (string)dict["name"],
                (string)dict["summoner"]
            ),

            MessageType.Ping => new Ping((long)dict["ts"]),

            MessageType.Pong => new Pong(
                (long)dict["origTs"],
                (long)dict["serverTs"]
            ),

            _ => throw new ArgumentException($"Unknown message type: {type}")
        };
    }

    /// <summary>
    /// Get the message type from a serialized dictionary without full deserialization.
    /// </summary>
    public MessageType GetMessageType(Dictionary dict)
    {
        return (MessageType)(int)dict["type"];
    }

    #region Helper Methods

    private static Dictionary SerializeVector3(Vector3 v)
        => new() { ["x"] = v.X, ["y"] = v.Y, ["z"] = v.Z };

    private static Vector3 DeserializeVector3(Variant v)
    {
        var d = v.AsGodotDictionary();
        return new Vector3((float)(double)d["x"], (float)(double)d["y"], (float)(double)d["z"]);
    }

    private Godot.Collections.Array SerializeSummoners(SummonerState[] summoners)
    {
        var arr = new Godot.Collections.Array();
        foreach (var s in summoners)
        {
            var d = new Dictionary
            {
                ["team"] = s.Team,
                ["hp"] = s.Hp,
                ["maxHp"] = s.MaxHp,
                ["mana"] = s.Mana,
                ["maxMana"] = s.MaxMana,
                ["casting"] = s.IsCasting,
                ["castTime"] = s.CastingTimeRemaining,
                ["castTotal"] = s.CastingTimeTotal,
                ["castCard"] = s.CastingCardIndex,
                ["castPos"] = SerializeVector3(s.CastingSpawnPosition),
                ["castNetId"] = s.CastingNetworkId,
                ["cardHash"] = s.CardStateHash,
                ["hand"] = ToGodotArray(s.Hand ?? System.Array.Empty<string>()),
                ["deck"] = ToGodotArray(s.Deck ?? System.Array.Empty<string>()),
                ["discard"] = ToGodotArray(s.DiscardPile ?? System.Array.Empty<string>())
            };
            arr.Add(d);
        }
        return arr;
    }

    private SummonerState[] DeserializeSummoners(Godot.Collections.Array arr)
    {
        var summoners = new SummonerState[arr.Count];
        for (int i = 0; i < arr.Count; i++)
        {
            var d = (Dictionary)arr[i];
            summoners[i] = new SummonerState(
                (int)d["team"],
                (float)d["hp"],
                (float)d["maxHp"],
                (float)d["mana"],
                (float)d["maxMana"],
                (bool)d["casting"],
                (float)d["castTime"],
                (float)d["castTotal"],
                (int)d["castCard"],
                DeserializeVector3(d["castPos"]),
                (int)d["castNetId"],
                (int)d["cardHash"],
                d.ContainsKey("hand") ? ToStringArray((Godot.Collections.Array)d["hand"]) : System.Array.Empty<string>(),
                d.ContainsKey("deck") ? ToStringArray((Godot.Collections.Array)d["deck"]) : System.Array.Empty<string>(),
                d.ContainsKey("discard") ? ToStringArray((Godot.Collections.Array)d["discard"]) : System.Array.Empty<string>()
            );
        }
        return summoners;
    }

    private Godot.Collections.Array SerializeUnits(UnitState[] units)
    {
        var arr = new Godot.Collections.Array();
        foreach (var u in units)
        {
            var d = new Dictionary
            {
                ["id"] = u.NetworkId,
                ["team"] = u.Team,
                ["pos"] = SerializeVector3(u.Position),
                ["hp"] = u.Hp,
                ["mhp"] = u.MaxHp,
                ["target"] = u.TargetNetworkId ?? -1,
                ["alive"] = u.IsAlive,
                ["activation"] = u.ActivationState,
                ["behavior"] = u.BehaviorState,
                ["facing"] = u.IsFacingRight,
                ["catalogId"] = u.CatalogId ?? "",
                ["spawnTimer"] = u.SpawnTimer,
                ["attackAnim"] = u.AttackAnimationTimer
            };
            arr.Add(d);
        }
        return arr;
    }

    private UnitState[] DeserializeUnits(Godot.Collections.Array arr)
    {
        var units = new UnitState[arr.Count];
        for (int i = 0; i < arr.Count; i++)
        {
            var d = (Dictionary)arr[i];
            units[i] = new UnitState(
                (int)d["id"],
                d.ContainsKey("team") ? (int)d["team"] : -1,
                DeserializeVector3(d["pos"]),
                (float)d["hp"],
                d.ContainsKey("mhp") ? (float)d["mhp"] : (float)d["hp"],
                (int)d["target"] == -1 ? null : (int)d["target"],
                (bool)d["alive"],
                (int)d["activation"],
                d.ContainsKey("behavior") ? (int)d["behavior"] : 0,
                d.ContainsKey("facing") ? (bool)d["facing"] : true,
                d.ContainsKey("catalogId") ? (string)d["catalogId"] : "",
                d.ContainsKey("spawnTimer") ? (float)d["spawnTimer"] : 0f,
                d.ContainsKey("attackAnim") ? (float)d["attackAnim"] : 0f
            );
        }
        return units;
    }

    private Godot.Collections.Array SerializeProjectiles(ProjectileState[] projectiles)
    {
        var arr = new Godot.Collections.Array();
        foreach (var p in projectiles)
        {
            var d = new Dictionary
            {
                ["id"] = p.ProjectileId,
                ["catalogId"] = p.ProjectileCatalogId,
                ["src"] = p.SourceUnitId,
                ["target"] = p.TargetUnitId,
                ["team"] = p.Team,
                ["move"] = p.MovementType,
                ["pos"] = SerializeVector3(p.CurrentPosition),
                ["dir"] = SerializeVector3(p.Direction),
                ["targetPos"] = SerializeVector3(p.TargetPosition),
                ["progress"] = p.Progress,
                ["speed"] = p.Speed,
                ["dead"] = p.IsDead
            };
            arr.Add(d);
        }
        return arr;
    }

    private ProjectileState[] DeserializeProjectiles(Godot.Collections.Array arr)
    {
        var projectiles = new ProjectileState[arr.Count];
        for (int i = 0; i < arr.Count; i++)
        {
            var d = (Dictionary)arr[i];
            projectiles[i] = new ProjectileState(
                ProjectileId: (int)d["id"],
                ProjectileCatalogId: d.ContainsKey("catalogId") ? (string)d["catalogId"] : "",
                SourceUnitId: d.ContainsKey("src") ? (int)d["src"] : -1,
                TargetUnitId: d.ContainsKey("target") ? (int)d["target"] : -1,
                Team: d.ContainsKey("team") ? (int)d["team"] : 0,
                MovementType: d.ContainsKey("move") ? (int)d["move"] : 0,
                CurrentPosition: DeserializeVector3(d["pos"]),
                Direction: d.ContainsKey("dir") ? DeserializeVector3(d["dir"]) : Vector3.Zero,
                TargetPosition: d.ContainsKey("targetPos") ? DeserializeVector3(d["targetPos"]) : Vector3.Zero,
                Progress: d.ContainsKey("progress") ? (float)d["progress"] : 0f,
                Speed: d.ContainsKey("speed") ? (float)d["speed"] : 0f,
                IsDead: d.ContainsKey("dead") && (bool)d["dead"]
            );
        }
        return projectiles;
    }

    private string[] ToStringArray(Godot.Collections.Array arr)
    {
        var result = new string[arr.Count];
        for (int i = 0; i < arr.Count; i++)
        {
            result[i] = (string)arr[i];
        }
        return result;
    }

    private Godot.Collections.Array ToGodotArray(string[] arr)
    {
        var result = new Godot.Collections.Array();
        foreach (var item in arr)
        {
            result.Add(item);
        }
        return result;
    }

    #endregion
}
