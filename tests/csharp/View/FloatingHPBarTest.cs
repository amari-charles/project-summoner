namespace Fateforged.Tests.View;

using System.Reflection;
using Fateforged.UI;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class FloatingHPBarTest
{
    [TestCase]
    public void UpdateHpImmediate_SnapsTargetAndDisplayPercent()
    {
        var bar = new FloatingHPBar();

        bar.UpdateHp(50f, 100f);
        float targetBefore = GetPrivateField<float>(bar, "_targetHpPercent");
        float displayBefore = GetPrivateField<float>(bar, "_displayHpPercent");
        AssertThat(targetBefore).IsEqual(0.5f);
        AssertThat(displayBefore).IsEqual(1f);

        bar.UpdateHpImmediate(0f, 100f);

        float targetAfter = GetPrivateField<float>(bar, "_targetHpPercent");
        float displayAfter = GetPrivateField<float>(bar, "_displayHpPercent");
        AssertThat(targetAfter).IsEqual(0f);
        AssertThat(displayAfter).IsEqual(0f);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        var field = target
            .GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        return (T)field!.GetValue(target)!;
    }
}
