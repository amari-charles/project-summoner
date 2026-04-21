namespace Fateforged.Tests.Simulation;

using Fateforged.Simulation.Effects;
using Fateforged.Simulation.Enums;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class LegacyDurationAdapterTest
{
    [TestCase]
    public void LegacyDuration_Negative_MapsPersistent()
    {
        var resolved = EffectLifetimeResolver.Resolve(EffectLifetime.Timed(0f), -1f);

        AssertThat(resolved.IsPersistent).IsTrue();
        AssertThat(EffectLifetimeResolver.ResolveDuration(EffectLifetime.Timed(0f), -1f)).IsEqual(-1f);
    }

    [TestCase]
    public void Lifetime_RoundTrip_LegacyCompat()
    {
        var timed = EffectLifetime.Timed(2.5f);
        var persistent = EffectLifetime.Persistent();

        AssertThat(EffectLifetimeResolver.ResolveDuration(timed, 0f)).IsEqual(2.5f);
        AssertThat(EffectLifetimeResolver.ResolveDuration(persistent, 0f)).IsEqual(-1f);
        AssertThat(EffectLifetimeResolver.Resolve(EffectLifetime.Timed(0f), 3f).RemainingSeconds)
            .IsEqual(3f);
    }
}
