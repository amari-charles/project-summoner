using Godot;
using System;

namespace ProjectSummoner.Units.Components;

/// <summary>
/// Manages unit health, damage, healing, and death.
/// Extracted from Unit3D to isolate HP management.
/// </summary>
public class UnitHealth
{
    // =========================================================================
    // STATE
    // =========================================================================

    public float CurrentHp { get; private set; }
    public float MaxHp { get; private set; }
    public bool IsAlive { get; private set; } = true;
    public bool IsDying { get; private set; }

    // =========================================================================
    // EVENTS
    // =========================================================================

    /// <summary>Called when HP changes (newHp, maxHp).</summary>
    public event Action<float, float>? OnHpChanged;

    /// <summary>Called when unit dies.</summary>
    public event Action? OnDeath;

    // =========================================================================
    // PUBLIC API
    // =========================================================================

    /// <summary>
    /// Initialize health with max HP value.
    /// </summary>
    public void Initialize(float maxHp)
    {
        MaxHp = maxHp;
        CurrentHp = maxHp;
        IsAlive = true;
        IsDying = false;
    }

    /// <summary>
    /// Update max HP (e.g., from modifiers). Optionally heals to new max.
    /// </summary>
    public void SetMaxHp(float newMaxHp, bool healToMax = false)
    {
        MaxHp = newMaxHp;
        if (healToMax)
        {
            CurrentHp = MaxHp;
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
        }
        else if (CurrentHp > MaxHp)
        {
            CurrentHp = MaxHp;
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
        }
    }

    /// <summary>
    /// Apply damage to this unit.
    /// </summary>
    /// <returns>True if unit died from this damage.</returns>
    public bool TakeDamage(float amount)
    {
        if (!IsAlive || IsDying)
            return false;

        CurrentHp = Mathf.Max(CurrentHp - amount, 0);
        OnHpChanged?.Invoke(CurrentHp, MaxHp);

        if (CurrentHp <= 0)
        {
            Die();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Heal this unit by the specified amount.
    /// </summary>
    /// <returns>Actual amount healed.</returns>
    public float Heal(float amount)
    {
        if (!IsAlive || IsDying)
            return 0f;

        float previousHp = CurrentHp;
        CurrentHp = Mathf.Min(CurrentHp + amount, MaxHp);
        float actualHeal = CurrentHp - previousHp;

        if (actualHeal > 0)
        {
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
        }

        return actualHeal;
    }

    /// <summary>
    /// Kill this unit immediately.
    /// </summary>
    public void Die()
    {
        if (IsDying)
            return;

        IsDying = true;
        IsAlive = false;
        OnDeath?.Invoke();
    }

    /// <summary>
    /// Reset health state (for respawn/pooling).
    /// </summary>
    public void Reset(float maxHp)
    {
        Initialize(maxHp);
    }
}
