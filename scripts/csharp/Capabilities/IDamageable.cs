namespace ProjectSummoner.Capabilities;

/// <summary>
/// Interface for entities that can take damage.
/// All combat units must implement this.
/// </summary>
public interface IDamageable
{
    /// <summary>Current health points.</summary>
    float CurrentHp { get; }

    /// <summary>Maximum health points.</summary>
    float MaxHp { get; }

    /// <summary>Whether the entity is alive.</summary>
    bool IsAlive { get; }

    /// <summary>
    /// Apply damage to this entity (physical damage).
    /// </summary>
    /// <param name="amount">Amount of damage to apply.</param>
    void TakeDamage(float amount);

    /// <summary>
    /// Apply damage to this entity with damage type.
    /// </summary>
    /// <param name="amount">Amount of damage to apply.</param>
    /// <param name="damageType">Type of damage (e.g., "physical", "fire").</param>
    void TakeDamage(float amount, string damageType);
}
