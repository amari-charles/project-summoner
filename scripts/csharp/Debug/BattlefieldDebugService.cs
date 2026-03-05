using Godot;
using Fateforged.Constants;

namespace Fateforged.Infrastructure.Debug;

/// <summary>
/// Small debug bridge exposed as an autoload so GDScript debug tools can
/// control BattlefieldBounds static debug flags.
/// </summary>
[GlobalClass]
public partial class BattlefieldDebugService : Node
{
    public static BattlefieldDebugService? Instance { get; private set; }

    // Unit visualization flags (read by UnitVisual, toggled by DebugMenu).
    public bool HurtboxEnabled { get; set; }
    public bool TargetPointEnabled { get; set; }
    public bool AttackRangeEnabled { get; set; }
    public bool SeparationRadiusEnabled { get; set; }

    public bool AnyUnitDebugEnabled =>
        HurtboxEnabled || TargetPointEnabled || AttackRangeEnabled || SeparationRadiusEnabled;

    public override void _Ready()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    // Spawn boundary debug toggle.
    public bool GetBypassSpawnBoundary()
    {
        return BattlefieldBounds.IsDebugBypassSpawnBoundaryEnabled();
    }

    public void SetBypassSpawnBoundary(bool enabled)
    {
        BattlefieldBounds.SetDebugBypassSpawnBoundary(enabled);
    }

    public void ToggleBypassSpawnBoundary()
    {
        BattlefieldBounds.ToggleDebugBypassSpawnBoundary();
    }

    // Unit debug visualization API (GDScript-callable).
    public bool IsDebugHurtboxEnabled() => HurtboxEnabled;
    public bool IsDebugTargetPointEnabled() => TargetPointEnabled;
    public bool IsDebugAttackRangeEnabled() => AttackRangeEnabled;
    public bool IsDebugSeparationRadiusEnabled() => SeparationRadiusEnabled;

    public void SetDebugHurtboxEnabled(bool enabled) => HurtboxEnabled = enabled;
    public void SetDebugTargetPointEnabled(bool enabled) => TargetPointEnabled = enabled;
    public void SetDebugAttackRangeEnabled(bool enabled) => AttackRangeEnabled = enabled;
    public void SetDebugSeparationRadiusEnabled(bool enabled) => SeparationRadiusEnabled = enabled;

    public void ToggleDebugHurtbox() => HurtboxEnabled = !HurtboxEnabled;
    public void ToggleDebugTargetPoint() => TargetPointEnabled = !TargetPointEnabled;
    public void ToggleDebugAttackRange() => AttackRangeEnabled = !AttackRangeEnabled;
    public void ToggleDebugSeparationRadius() => SeparationRadiusEnabled = !SeparationRadiusEnabled;
}
