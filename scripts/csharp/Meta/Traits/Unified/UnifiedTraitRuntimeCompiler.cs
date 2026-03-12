using Fateforged.Simulation.Data;

namespace Fateforged.Meta.Traits.Unified;

/// <summary>
/// Pass 2 stub compiler for unified trait runtime state.
/// Pass 3 replaces this with full deterministic compilation logic.
/// </summary>
public static class UnifiedTraitRuntimeCompiler
{
    public static MatchTraitRuntimeState CompileStub()
    {
        var state = MatchTraitRuntimeState.Empty();
        state.Diagnostics.Add(
            new TraitRuntimeDiagnostic
            {
                Severity = TraitRuntimeDiagnosticSeverity.Info,
                Code = "PASS2_STUB",
                Message = "UnifiedTraitRuntimeCompiler: pass2 stub state",
            }
        );
        return state;
    }
}
