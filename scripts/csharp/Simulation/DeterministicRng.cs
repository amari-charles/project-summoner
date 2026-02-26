namespace Fateforged.Simulation;

/// <summary>
/// Deterministic RNG using xorshift32.
/// Both host and client initialize with the same seed → same random sequence.
/// Used by Simulation.Tick() for all randomness (crit rolls, etc.).
/// </summary>
public class DeterministicRng
{
    private uint _state;

    public DeterministicRng(long seed)
    {
        // Ensure non-zero state (xorshift requires it)
        _state = (uint)(seed & 0xFFFFFFFF);
        if (_state == 0) _state = 1;
    }

    /// <summary>
    /// Generate next random uint via xorshift32.
    /// </summary>
    public uint Next()
    {
        _state ^= _state << 13;
        _state ^= _state >> 17;
        _state ^= _state << 5;
        return _state;
    }

    /// <summary>
    /// Random float in [0, 1) range.
    /// </summary>
    public float NextFloat()
    {
        return Next() / (float)uint.MaxValue;
    }

    /// <summary>
    /// Random int in [min, max] inclusive range.
    /// </summary>
    public int Range(int min, int max)
    {
        if (min >= max) return min;
        uint range = (uint)(max - min + 1);
        return min + (int)(Next() % range);
    }
}
