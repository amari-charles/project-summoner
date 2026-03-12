namespace Fateforged.Tests.Simulation;

using System;
using Fateforged.Simulation.Combat.Targeting;
using Fateforged.Simulation.Enums;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class TargetPolicyRegistryTest
{
    [TestCase]
    public void Resolve_KnownIds_ReturnsPolicyInstances()
    {
        AssertThat(TargetPolicyRegistry.Resolve(TargetPolicyId.PreferAttackable))
            .IsInstanceOf<PreferAttackableTargetPolicy>();
        AssertThat(TargetPolicyRegistry.Resolve(TargetPolicyId.PreferAttackableAndStick))
            .IsInstanceOf<PreferAttackableAndStickTargetPolicy>();
    }

    [TestCase]
    public void Resolve_SameId_ReturnsSameSingletonInstance()
    {
        var first = TargetPolicyRegistry.Resolve(TargetPolicyId.PreferAttackableAndStick);
        var second = TargetPolicyRegistry.Resolve(TargetPolicyId.PreferAttackableAndStick);

        AssertThat(ReferenceEquals(first, second)).IsTrue();
    }

    [TestCase]
    public void Resolve_UnknownId_ThrowsArgumentOutOfRange()
    {
        var unknown = (TargetPolicyId)999;
        AssertThrown(() => TargetPolicyRegistry.Resolve(unknown))
            .IsInstanceOf<ArgumentOutOfRangeException>();
    }
}
