namespace Fateforged.Tests.Meta.Progression;

using System.Linq;
using Fateforged.Meta.Progression;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
public class ProgressionAuthorityBoundaryTest
{
    [TestCase]
    public void BPA_C17_AuthorityContractHasNoGodotOrBackendTypes()
    {
        var methods = typeof(IProgressionAuthority).GetMethods();
        var boundaryTypes = methods
            .SelectMany(method =>
                method
                    .GetParameters()
                    .Select(parameter => parameter.ParameterType)
                    .Append(method.ReturnType)
            )
            .ToArray();

        AssertThat(boundaryTypes.Any(type => typeof(GodotObject).IsAssignableFrom(type))).IsFalse();
        AssertThat(boundaryTypes.Any(type => type.Namespace?.Contains("Backend") == true))
            .IsFalse();
        AssertThat(boundaryTypes.Any(type => type.Namespace?.Contains("Nakama") == true)).IsFalse();
    }
}
