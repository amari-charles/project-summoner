using System;
using Godot;
using ProjectSummoner.Cards.Configs;
using ProjectSummoner.Cards.Formations;
using ProjectSummoner.Units;

namespace ProjectSummoner.Cards;

/// <summary>
/// Card that summons units when played.
/// Uses SpawnConfig for what/how many to spawn, and IFormationStrategy for positioning.
///
/// Note: Currently, all summon execution is handled by CardFactory.execute_summon()
/// which is called from GDScript. This class exists for future full C# card execution
/// when GDScript Card is phased out.
/// </summary>
public partial class SummonCard : Card
{
    /// <summary>
    /// Configuration for spawning units (scene, count, timing).
    /// </summary>
    public SpawnConfig? Spawn { get; set; }

    /// <summary>
    /// Formation strategy for positioning spawned units.
    /// </summary>
    public IFormationStrategy? Formation { get; set; }

    /// <summary>
    /// Execute the summon by spawning units at the position.
    /// Currently not implemented - all execution goes through CardFactory.execute_summon().
    /// </summary>
    public override void Play3D(
        Vector3 position,
        Team team,
        Node battlefield,
        Node? modifierSystem = null,
        float spawnDuration = 0.0f)
    {
        throw new NotImplementedException(
            $"SummonCard.Play3D() not implemented. Use CardFactory.execute_summon() instead.");
    }
}
