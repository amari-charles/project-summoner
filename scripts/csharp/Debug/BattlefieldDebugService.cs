using Fateforged.Constants;
using Godot;

namespace Fateforged.Infrastructure.Debug;

/// <summary>
/// Small debug bridge exposed as an autoload so GDScript debug tools can
/// control BattlefieldBounds static debug flags.
/// </summary>
[GlobalClass]
public partial class BattlefieldDebugService : Node
{
    private static BattlefieldDebugService? _instance;
    public static BattlefieldDebugService? Instance
    {
        get
        {
            if (_instance != null)
                return _instance;

            if (Engine.GetMainLoop() is SceneTree tree)
            {
                _instance = tree.Root.GetNodeOrNull<BattlefieldDebugService>("BattlefieldDebug");
            }

            return _instance;
        }
        private set => _instance = value;
    }

    // Unit visualization flags (read by UnitVisual, toggled by DebugMenu).
    public bool HurtboxEnabled { get; set; }
    public bool TargetPointEnabled { get; set; }
    public bool EngageRangeEnabled { get; set; }
    public bool DamageShapeEnabled { get; set; }
    public bool NavigationFootprintEnabled { get; set; }
    public bool ProjectileHitGeometryEnabled { get; set; }

    public bool AnyUnitDebugEnabled =>
        HurtboxEnabled
        || TargetPointEnabled
        || EngageRangeEnabled
        || DamageShapeEnabled
        || NavigationFootprintEnabled;
    public bool AnyProjectileDebugEnabled => ProjectileHitGeometryEnabled;

    public override void _Ready()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool IsSpawnBoundaryBypassEnabled()
    {
        return BattlefieldBounds.IsDebugBypassSpawnBoundaryEnabled();
    }

    public void SetSpawnBoundaryBypassEnabled(bool enabled)
    {
        BattlefieldBounds.SetDebugBypassSpawnBoundary(enabled);
    }

    public bool ToggleSpawnBoundaryBypass()
    {
        BattlefieldBounds.ToggleDebugBypassSpawnBoundary();
        return BattlefieldBounds.IsDebugBypassSpawnBoundaryEnabled();
    }

    // Unit debug visualization API (GDScript-callable).
    public bool IsDebugHurtboxEnabled() => HurtboxEnabled;

    public bool IsDebugTargetPointEnabled() => TargetPointEnabled;

    public bool IsDebugEngageRangeEnabled() => EngageRangeEnabled;

    public bool IsDebugDamageShapeEnabled() => DamageShapeEnabled;

    public bool IsDebugNavigationFootprintEnabled() => NavigationFootprintEnabled;

    public bool IsDebugProjectileHitGeometryEnabled() => ProjectileHitGeometryEnabled;

    public void SetDebugHurtboxEnabled(bool enabled) => HurtboxEnabled = enabled;

    public void SetDebugTargetPointEnabled(bool enabled) => TargetPointEnabled = enabled;

    public void SetDebugEngageRangeEnabled(bool enabled) => EngageRangeEnabled = enabled;

    public void SetDebugDamageShapeEnabled(bool enabled) => DamageShapeEnabled = enabled;

    public void SetDebugNavigationFootprintEnabled(bool enabled) =>
        NavigationFootprintEnabled = enabled;

    public void SetDebugProjectileHitGeometryEnabled(bool enabled) =>
        ProjectileHitGeometryEnabled = enabled;

    public void ToggleDebugHurtbox() => HurtboxEnabled = !HurtboxEnabled;

    public void ToggleDebugTargetPoint() => TargetPointEnabled = !TargetPointEnabled;

    public void ToggleDebugEngageRange() => EngageRangeEnabled = !EngageRangeEnabled;

    public void ToggleDebugDamageShape() => DamageShapeEnabled = !DamageShapeEnabled;

    public void ToggleDebugNavigationFootprint() =>
        NavigationFootprintEnabled = !NavigationFootprintEnabled;

    public void ToggleDebugProjectileHitGeometry() =>
        ProjectileHitGeometryEnabled = !ProjectileHitGeometryEnabled;
}
