using Godot;

namespace ProjectSummoner.Units;

/// <summary>
/// Singleton service that holds unit debug visualization state.
/// Exposed as an autoload so GDScript can toggle debug overlays.
/// Unit3D reads from Instance to render debug markers.
/// </summary>
[GlobalClass]
public partial class UnitDebugService : Node
{
    public static UnitDebugService? Instance { get; private set; }

    public bool HurtboxEnabled { get; set; }
    public bool TargetPointEnabled { get; set; }
    public bool AttackRangeEnabled { get; set; }
    public bool SeparationRadiusEnabled { get; set; }

    public bool AnyEnabled => HurtboxEnabled || TargetPointEnabled || AttackRangeEnabled || SeparationRadiusEnabled;

    public override void _Ready()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    // GDScript-callable methods (instance methods, not static)

    public bool IsDebugHurtboxEnabled() => HurtboxEnabled;
    public bool IsDebugTargetPointEnabled() => TargetPointEnabled;
    public bool IsDebugAttackRangeEnabled() => AttackRangeEnabled;
    public bool IsDebugSeparationRadiusEnabled() => SeparationRadiusEnabled;

    public void SetDebugHurtboxEnabled(bool enabled) => HurtboxEnabled = enabled;
    public void SetDebugTargetPointEnabled(bool enabled) => TargetPointEnabled = enabled;
    public void SetDebugAttackRangeEnabled(bool enabled) => AttackRangeEnabled = enabled;
    public void SetDebugSeparationRadiusEnabled(bool enabled) => SeparationRadiusEnabled = enabled;

    public void ToggleDebugHurtbox()
    {
        HurtboxEnabled = !HurtboxEnabled;
        GD.Print($"[UnitDebug] Hurtbox: {(HurtboxEnabled ? "ON" : "OFF")}");
    }

    public void ToggleDebugTargetPoint()
    {
        TargetPointEnabled = !TargetPointEnabled;
        GD.Print($"[UnitDebug] Target Point: {(TargetPointEnabled ? "ON" : "OFF")}");
    }

    public void ToggleDebugAttackRange()
    {
        AttackRangeEnabled = !AttackRangeEnabled;
        GD.Print($"[UnitDebug] Attack Range: {(AttackRangeEnabled ? "ON" : "OFF")}");
    }

    public void ToggleDebugSeparationRadius()
    {
        SeparationRadiusEnabled = !SeparationRadiusEnabled;
        GD.Print($"[UnitDebug] Separation Radius: {(SeparationRadiusEnabled ? "ON" : "OFF")}");
    }
}
