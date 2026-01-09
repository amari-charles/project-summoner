namespace ProjectSummoner.Tests.Summons;

using GdUnit4;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for SummonResult class.
/// Note: SummonResult inherits from RefCounted which requires Godot runtime.
/// These tests verify compile-time contracts and static factory signatures.
/// Full runtime tests should be run through Godot editor.
/// </summary>
[TestSuite]
public class SummonResultTest
{
    [TestCase]
    public void SummonResult_FactoryMethods_Exist()
    {
        // Verify the static factory methods exist and have correct signatures
        // This is a compile-time check that ensures the API contract is maintained
        AssertThat(typeof(ProjectSummoner.Summons.SummonResult).GetMethod("Ok")).IsNotNull();
        AssertThat(typeof(ProjectSummoner.Summons.SummonResult).GetMethod("Fail")).IsNotNull();
    }

    [TestCase]
    public void SummonResult_HasExpectedProperties()
    {
        // Verify properties exist via reflection (compile-time contract check)
        var type = typeof(ProjectSummoner.Summons.SummonResult);

        AssertThat(type.GetProperty("Success")).IsNotNull();
        AssertThat(type.GetProperty("Summon")).IsNotNull();
        AssertThat(type.GetProperty("Error")).IsNotNull();
    }

    [TestCase]
    public void SummonResult_HasToDictionaryMethod()
    {
        var type = typeof(ProjectSummoner.Summons.SummonResult);
        AssertThat(type.GetMethod("ToDictionary")).IsNotNull();
    }
}
