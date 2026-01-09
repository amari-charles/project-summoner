using Godot;

namespace ProjectSummoner.Summons;

/// <summary>
/// Result of a summon operation.
/// Contains either a successful UnitSummon or an error message.
/// </summary>
public partial class SummonResult : RefCounted
{
    /// <summary>Whether the summon succeeded.</summary>
    public bool Success { get; }

    /// <summary>The summon tracker (null if failed).</summary>
    public UnitSummon? Summon { get; }

    /// <summary>Error message if failed (null if succeeded).</summary>
    public string? Error { get; }

    private SummonResult(bool success, UnitSummon? summon, string? error)
    {
        Success = success;
        Summon = summon;
        Error = error;
    }

    /// <summary>Creates a successful result.</summary>
    public static SummonResult Ok(UnitSummon summon)
    {
        return new SummonResult(true, summon, null);
    }

    /// <summary>Creates a failed result.</summary>
    public static SummonResult Fail(string error)
    {
        GD.PrintErr($"[SummonResult] Summon failed: {error}");
        return new SummonResult(false, null, error);
    }

    /// <summary>
    /// Converts to a Godot Dictionary for GDScript interop.
    /// </summary>
    public Godot.Collections.Dictionary ToDictionary()
    {
        var dict = new Godot.Collections.Dictionary
        {
            ["success"] = Success
        };

        if (Success && Summon != null)
        {
            dict["summon"] = Summon;
            dict["alive_count"] = Summon.AliveCount;
            dict["units"] = Summon.GetAliveUnitsArray();
        }
        else
        {
            dict["error"] = Error ?? "Unknown error";
        }

        return dict;
    }

    public override string ToString() =>
        Success
            ? $"SummonResult(success, {Summon})"
            : $"SummonResult(failed: {Error})";
}
