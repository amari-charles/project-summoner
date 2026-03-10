namespace Fateforged.Tests.Session;

using Fateforged.Session;
using Fateforged.Simulation;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Cards;
using GdUnit4;
using Fateforged.Tests.Simulation;
using Fateforged.Units;
using static GdUnit4.Assertions;

[TestSuite]
public class SnapshotCodecTest
{
    private SnapshotCodec _codec = null!;

    [BeforeTest]
    public void Setup()
    {
        _codec = new SnapshotCodec();
    }

    [TestCase]
    public void RoundTrip_EmptyState_PreservesFields()
    {
        var state = new MatchState
        {
            FrameNumber = 42,
            MatchTime = 10.5f,
            Phase = GamePhase.Battle,
            PrepTimeRemaining = 0f,
            IsOvertime = true
        };
        state.WinnerTeam = null;

        var encoded = _codec.Encode(state);
        var decoded = _codec.Decode(encoded);

        AssertThat(decoded.FrameNumber).IsEqual(42L);
        AssertThat(decoded.MatchTime).IsEqual(10.5f);
        AssertThat(decoded.Phase).IsEqual(GamePhase.Battle);
        AssertThat(decoded.IsOvertime).IsTrue();
        AssertThat(decoded.WinnerTeam).IsNull();
    }

    [TestCase]
    public void RoundTrip_SummonerData_Preserved()
    {
        var state = SimTestHelper.CreateBattleState();
        state.Summoners[0].Mana = 7.5f;
        state.Summoners[0].IsCasting = true;
        state.Summoners[0].CastingCardIndex = 2;
        state.Summoners[0].CastingCatalogId = "fire_wisp";
        state.Summoners[0].Hand.Add("card_a");
        state.Summoners[0].Hand.Add("card_b");
        state.Summoners[0].Deck.Add("card_c");
        state.Summoners[0].DiscardPile.Add("card_d");
        state.Summoners[0].DamageBonus = 12.5f;
        state.Summoners[0].DamageReduction = 3f;
        state.Summoners[0].SoulGuard = 1.5f;
        state.Summoners[0].SetElementalDamageBonus(Element.Fire, 20f);
        state.Summoners[0].SetElementalDamageBonus(Element.Water, 5f);

        var decoded = _codec.Decode(_codec.Encode(state));

        AssertThat(decoded.Summoners[0].Mana).IsEqualApprox(7.5f, 0.2f);
        AssertThat(decoded.Summoners[0].IsCasting).IsTrue();
        AssertThat(decoded.Summoners[0].CastingCardIndex).IsEqual(2);
        AssertThat(decoded.Summoners[0].CastingCatalogId.Value).IsEqual("fire_wisp");
        AssertThat(decoded.Summoners[0].Hand).HasSize(2);
        AssertThat(decoded.Summoners[0].Hand[0].Value).IsEqual("card_a");
        AssertThat(decoded.Summoners[0].Deck).HasSize(1);
        AssertThat(decoded.Summoners[0].DiscardPile).HasSize(1);
        AssertThat(decoded.Summoners[0].DamageBonus).IsEqualApprox(12.5f, 0.001f);
        AssertThat(decoded.Summoners[0].DamageReduction).IsEqualApprox(3f, 0.001f);
        AssertThat(decoded.Summoners[0].SoulGuard).IsEqualApprox(1.5f, 0.001f);
        AssertThat(decoded.Summoners[0].GetElementalDamageBonus(Element.Fire)).IsEqualApprox(20f, 0.001f);
        AssertThat(decoded.Summoners[0].GetElementalDamageBonus(Element.Water)).IsEqualApprox(5f, 0.001f);
    }

    [TestCase]
    public void RoundTrip_Units_Preserved()
    {
        var state = SimTestHelper.CreateBattleState();
        var unit = SimTestHelper.CreateMeleeUnit(state, 0, x: 5.123f, z: -3.456f, hp: 75.5f);
        unit.NetworkId = 42;
        unit.BehaviorState = BehaviorState.Attacking;
        unit.IsFacingRight = true;
        unit.TargetUnitId = null;

        var decoded = _codec.Decode(_codec.Encode(state));

        AssertThat(decoded.Units.Count).IsEqual(1);
        var decodedUnit = decoded.Units[unit.UnitId];
        AssertThat(decodedUnit.NetworkId).IsEqual(42);
        AssertThat(decodedUnit.Position.X).IsEqualApprox(5.123f, 0.002f);
        AssertThat(decodedUnit.Position.Z).IsEqualApprox(-3.456f, 0.002f);
        AssertThat(decodedUnit.CurrentHp).IsEqualApprox(75.5f, 0.2f);
        AssertThat(decodedUnit.BehaviorState).IsEqual(BehaviorState.Attacking);
        AssertThat(decodedUnit.IsFacingRight).IsTrue();
    }

    [TestCase]
    public void RoundTrip_WinnerTeam_Preserved()
    {
        var state = SimTestHelper.CreateBattleState();
        state.Phase = GamePhase.GameOver;
        state.WinnerTeam = 1;

        var decoded = _codec.Decode(_codec.Encode(state));

        AssertThat(decoded.Phase).IsEqual(GamePhase.GameOver);
        AssertThat(decoded.WinnerTeam.HasValue).IsTrue();
        AssertThat(decoded.WinnerTeam!.Value).IsEqual(1);
    }
}
