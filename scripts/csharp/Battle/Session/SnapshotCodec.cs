using System.IO;
using Fateforged.Cards;
using Fateforged.Simulation;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Units;

namespace Fateforged.Session;

/// <summary>
/// Serialize/deserialize MatchState for network transmission.
/// Uses BinaryWriter/BinaryReader for compact encoding.
/// Position quantized to millimeters, HP to tenths.
/// </summary>
public class SnapshotCodec
{
    private const float PositionScale = 1000f;
    private const float HpScale = 10f;

    public byte[] Encode(MatchState state)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);

        // Header
        w.Write(state.FrameNumber);
        w.Write(state.MatchTime);
        w.Write((int)state.Phase);
        w.Write(state.PrepTimeRemaining);
        w.Write(state.IsOvertime);
        w.Write(state.WinnerTeam ?? -1);

        // Summoners
        for (int i = 0; i < 2; i++)
        {
            var s = state.Summoners[i];
            EncodeSummoner(w, s);
        }

        // Units
        w.Write(state.Units.Count);
        foreach (var (unitId, unit) in state.Units)
        {
            w.Write(unitId);
            EncodeUnit(w, unit);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Decode a snapshot into a read-only MatchState for client-side rendering.
    /// Note: ID counters (_nextUnitId, etc.) are NOT restored — the client never
    /// generates IDs. All ID assignment is host-authoritative.
    /// </summary>
    public MatchState Decode(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var r = new BinaryReader(ms);

        var state = new MatchState();

        // Header
        state.FrameNumber = r.ReadInt64();
        state.MatchTime = r.ReadSingle();
        state.Phase = (GamePhase)r.ReadInt32();
        state.PrepTimeRemaining = r.ReadSingle();
        state.IsOvertime = r.ReadBoolean();
        int winnerTeam = r.ReadInt32();
        state.WinnerTeam = winnerTeam == -1 ? null : winnerTeam;

        // Summoners
        for (int i = 0; i < 2; i++)
        {
            DecodeSummoner(r, state.Summoners[i]);
        }

        // Units
        int unitCount = r.ReadInt32();
        for (int i = 0; i < unitCount; i++)
        {
            int unitId = r.ReadInt32();
            var unit = DecodeUnit(r);
            unit.UnitId = unitId;
            state.Units[unitId] = unit;
        }

        return state;
    }

    private static void EncodeSummoner(BinaryWriter w, SummonerData s)
    {
        w.Write((int)s.Team);
        w.Write(QuantizeHp(s.CurrentHp));
        w.Write(QuantizeHp(s.MaxHp));
        w.Write(s.IsAlive);
        w.Write(QuantizeHp(s.Mana));
        w.Write(QuantizeHp(s.MaxMana));
        w.Write(s.IsCasting);
        w.Write(s.CastingTimeRemaining);
        w.Write(s.CastingTimeTotal);
        w.Write(s.CastingCardIndex);
        w.Write(s.CastingCatalogId.Value);
        WriteSimVector3(w, s.CastingSpawnPosition);
        w.Write(s.CastingNetworkId);
        w.Write(s.DamageBonus);
        w.Write(s.DamageReduction);
        w.Write(s.SoulStrength);

        var elementalBonuses = s.EnumerateElementalDamageBonuses();
        int bonusCount = 0;
        foreach (var _ in elementalBonuses)
            bonusCount++;
        w.Write(bonusCount);
        foreach (var (element, bonus) in s.EnumerateElementalDamageBonuses())
        {
            w.Write((int)element);
            w.Write(bonus);
        }

        // Hand
        w.Write(s.Hand.Count);
        foreach (var card in s.Hand)
            w.Write(card.Value);

        // Deck
        w.Write(s.Deck.Count);
        foreach (var card in s.Deck)
            w.Write(card.Value);

        // Discard
        w.Write(s.DiscardPile.Count);
        foreach (var card in s.DiscardPile)
            w.Write(card.Value);
    }

    private static void DecodeSummoner(BinaryReader r, SummonerData s)
    {
        s.Team = (Team)r.ReadInt32();
        s.CurrentHp = DequantizeHp(r.ReadInt32());
        s.MaxHp = DequantizeHp(r.ReadInt32());
        s.IsAlive = r.ReadBoolean();
        s.Mana = DequantizeHp(r.ReadInt32());
        s.MaxMana = DequantizeHp(r.ReadInt32());
        s.IsCasting = r.ReadBoolean();
        s.CastingTimeRemaining = r.ReadSingle();
        s.CastingTimeTotal = r.ReadSingle();
        s.CastingCardIndex = r.ReadInt32();
        s.CastingCatalogId = new SimCardCatalogId(r.ReadString());
        s.CastingCardInstanceId = SimCardInstanceId.Empty;
        s.CastingSpawnPosition = ReadSimVector3(r);
        s.CastingNetworkId = r.ReadInt32();
        s.DamageBonus = r.ReadSingle();
        s.DamageReduction = r.ReadSingle();
        s.SoulStrength = r.ReadSingle();
        s.ClearElementalDamageBonuses();
        int bonusCount = r.ReadInt32();
        for (int i = 0; i < bonusCount; i++)
        {
            var element = (Element)r.ReadInt32();
            float bonus = r.ReadSingle();
            s.SetElementalDamageBonus(element, bonus);
        }

        int handCount = r.ReadInt32();
        s.Hand.Clear();
        s.HandRefs.Clear();
        for (int i = 0; i < handCount; i++)
            s.Hand.Add(new SimCardCatalogId(r.ReadString()));

        int deckCount = r.ReadInt32();
        s.Deck.Clear();
        s.DeckRefs.Clear();
        for (int i = 0; i < deckCount; i++)
            s.Deck.Add(new SimCardCatalogId(r.ReadString()));

        int discardCount = r.ReadInt32();
        s.DiscardPile.Clear();
        s.DiscardRefs.Clear();
        for (int i = 0; i < discardCount; i++)
            s.DiscardPile.Add(new SimCardCatalogId(r.ReadString()));
    }

    private static void EncodeUnit(BinaryWriter w, UnitData u)
    {
        w.Write(u.NetworkId);
        w.Write(u.CatalogId.Value);
        w.Write((int)u.Team);
        WriteQuantizedPosition(w, u.Position);
        w.Write(QuantizeHp(u.CurrentHp));
        w.Write(QuantizeHp(u.MaxHp));
        w.Write(u.IsAlive);
        w.Write((int)u.ActivationState);
        w.Write((int)u.BehaviorState);
        w.Write(u.IsFacingRight);
        w.Write(u.Engagement.TargetUnitId ?? -1);
    }

    private static UnitData DecodeUnit(BinaryReader r)
    {
        var u = new UnitData();
        u.NetworkId = r.ReadInt32();
        u.CatalogId = new SimUnitCatalogId(r.ReadString());
        u.Team = (Team)r.ReadInt32();
        u.Position = ReadQuantizedPosition(r);
        u.CurrentHp = DequantizeHp(r.ReadInt32());
        u.MaxHp = DequantizeHp(r.ReadInt32());
        u.IsAlive = r.ReadBoolean();
        u.ActivationState = (ActivationState)r.ReadInt32();
        u.BehaviorState = (BehaviorState)r.ReadInt32();
        u.IsFacingRight = r.ReadBoolean();
        int targetId = r.ReadInt32();
        u.Engagement.TargetUnitId = targetId == -1 ? null : targetId;
        return u;
    }

    private static int QuantizeHp(float value) => (int)(value * HpScale);

    private static float DequantizeHp(int value) => value / HpScale;

    private static void WriteQuantizedPosition(BinaryWriter w, SimVector3 v)
    {
        w.Write((int)(v.X * PositionScale));
        w.Write((int)(v.Y * PositionScale));
        w.Write((int)(v.Z * PositionScale));
    }

    private static SimVector3 ReadQuantizedPosition(BinaryReader r)
    {
        float x = r.ReadInt32() / PositionScale;
        float y = r.ReadInt32() / PositionScale;
        float z = r.ReadInt32() / PositionScale;
        return new SimVector3(x, y, z);
    }

    private static void WriteSimVector3(BinaryWriter w, SimVector3 v)
    {
        w.Write(v.X);
        w.Write(v.Y);
        w.Write(v.Z);
    }

    private static SimVector3 ReadSimVector3(BinaryReader r)
    {
        return new SimVector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
    }
}
