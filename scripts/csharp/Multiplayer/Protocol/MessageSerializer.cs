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
                dict["pos"] = m.Position;
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
                dict["pos"] = m.Position;
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
                dict["hash"] = m.StateHash;
                dict["overtime"] = m.IsOvertime;
                break;

            case UnitSpawned m:
                dict["type"] = (int)MessageType.UnitSpawned;
                dict["id"] = m.NetworkId;
                dict["unitType"] = m.UnitType;
                dict["team"] = m.Team;
                dict["pos"] = m.Position;
                dict["tick"] = m.MatchTick;
                dict["srcSeq"] = m.SourceSequence ?? -1;
                dict["srcPlayer"] = m.SourcePlayerIndex ?? -1;
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
                (Vector3)dict["pos"],
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
                (Vector3)dict["pos"],
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
                (int)dict["hash"],
                (bool)dict["overtime"]
            ),

            MessageType.UnitSpawned => new UnitSpawned(
                (int)dict["id"],
                (string)dict["unitType"],
                (int)dict["team"],
                (Vector3)dict["pos"],
                (long)dict["tick"],
                (int)dict["srcSeq"] == -1 ? null : (int)dict["srcSeq"],
                (int)dict["srcPlayer"] == -1 ? null : (int)dict["srcPlayer"]
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
                ["castPos"] = s.CastingSpawnPosition,
                ["castNetId"] = s.CastingNetworkId,
                ["cardHash"] = s.CardStateHash
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
                (Vector3)d["castPos"],
                (int)d["castNetId"],
                (int)d["cardHash"]
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
                ["pos"] = u.Position,
                ["hp"] = u.Hp,
                ["target"] = u.TargetNetworkId ?? -1,
                ["alive"] = u.IsAlive,
                ["activation"] = u.ActivationState
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
                (Vector3)d["pos"],
                (float)d["hp"],
                (int)d["target"] == -1 ? null : (int)d["target"],
                (bool)d["alive"],
                (int)d["activation"]
            );
        }
        return units;
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
