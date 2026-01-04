using Godot;
using ProjectSummoner.Cards.Configs;
using ProjectSummoner.Cards.Formations;
using ProjectSummoner.Units;

namespace ProjectSummoner.Cards;

/// <summary>
/// Card that summons units when played.
/// Uses SpawnConfig for what/how many to spawn, and IFormationStrategy for positioning.
/// </summary>
public partial class SummonCard : Card
{
    // =========================================================================
    // SUMMON-SPECIFIC
    // =========================================================================

    /// <summary>
    /// Configuration for spawning units (scene, count, timing).
    /// </summary>
    public SpawnConfig? Spawn { get; set; }

    /// <summary>
    /// Formation strategy for positioning spawned units.
    /// </summary>
    public IFormationStrategy? Formation { get; set; }

    // =========================================================================
    // GDSCRIPT-COMPATIBLE ACCESSORS
    // =========================================================================

    // Expose spawn config properties for GDScript compatibility
    public string unit_scene_path => Spawn?.UnitScenePath ?? "";
    public int spawn_count => Spawn?.SpawnCount ?? 1;
    public float summon_time => Spawn?.SummonTime ?? 1.0f;

    // =========================================================================
    // EXECUTION
    // =========================================================================

    /// <summary>
    /// Execute the summon by spawning units at the position.
    /// Note: Currently, summoning is delegated to CardFactory from GDScript.
    /// This method exists for future full C# card execution.
    /// </summary>
    public override void Play3D(
        Vector3 position,
        Team team,
        Node battlefield,
        Node? modifierSystem = null,
        float spawnDuration = 0.0f)
    {
        if (Spawn == null)
        {
            GD.PrintErr($"[SummonCard] Card '{CardName}' has no SpawnConfig assigned!");
            return;
        }

        if (Formation == null)
        {
            GD.PrintErr($"[SummonCard] Card '{CardName}' has no Formation assigned!");
            return;
        }

        if (string.IsNullOrEmpty(Spawn.UnitScenePath))
        {
            GD.PrintErr($"[SummonCard] Card '{CardName}' has no unit scene path!");
            return;
        }

        // Note: Full execution is currently handled by CardFactory.execute_summon()
        // which is called from GDScript. This method is a placeholder for future
        // full C# card execution when GDScript Card is phased out.
        GD.PrintErr($"[SummonCard] Direct Play3D() not yet implemented - use CardFactory.execute_summon()");
    }
}
