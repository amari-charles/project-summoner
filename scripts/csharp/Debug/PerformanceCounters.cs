using Godot;

namespace ProjectSummoner.Debug;

/// <summary>
/// Static performance counters for profiling hot paths.
/// Tracks rolling averages over the last N frames for readable display.
/// </summary>
public static class PerformanceCounters
{
    // =========================================================================
    // CONFIGURATION
    // =========================================================================

    private const int FrameBufferSize = 30;  // Average over ~0.5s at 60fps

    // =========================================================================
    // CURRENT FRAME COUNTERS (incremented by hot paths)
    // =========================================================================

    /// <summary>
    /// Number of GetEnemySummoner() calls that scan the scene tree.
    /// </summary>
    public static int SummonerLookups;

    /// <summary>
    /// Number of AcquireTarget() calls per frame.
    /// </summary>
    public static int TargetAcquisitions;

    /// <summary>
    /// Total microseconds spent in all Unit3D._PhysicsProcess() calls.
    /// </summary>
    public static ulong PhysicsProcessTimeUsec;

    /// <summary>
    /// Number of active units being processed this frame.
    /// </summary>
    public static int ActiveUnits;

    // =========================================================================
    // ROLLING AVERAGE BUFFERS
    // =========================================================================

    private static readonly int[] _summonerLookupsBuffer = new int[FrameBufferSize];
    private static readonly int[] _targetAcquisitionsBuffer = new int[FrameBufferSize];
    private static readonly ulong[] _physicsTimeBuffer = new ulong[FrameBufferSize];
    private static readonly int[] _activeUnitsBuffer = new int[FrameBufferSize];

    private static int _bufferIndex = 0;
    private static int _frameCount = 0;  // Track how many frames we've recorded

    // =========================================================================
    // AVERAGED VALUES (for display)
    // =========================================================================

    public static float AvgSummonerLookups { get; private set; }
    public static float AvgTargetAcquisitions { get; private set; }
    public static float AvgPhysicsTimeUsec { get; private set; }
    public static float AvgActiveUnits { get; private set; }

    /// <summary>
    /// Average microseconds per unit in _PhysicsProcess.
    /// </summary>
    public static float AvgUsecPerUnit => AvgActiveUnits > 0 ? AvgPhysicsTimeUsec / AvgActiveUnits : 0;

    // =========================================================================
    // API
    // =========================================================================

    /// <summary>
    /// End current frame: store values in buffer, calculate averages, then reset.
    /// Call at start of each frame (before hot paths run).
    /// </summary>
    public static void ResetFrame()
    {
        // Store current frame values in buffer (from previous frame)
        _summonerLookupsBuffer[_bufferIndex] = SummonerLookups;
        _targetAcquisitionsBuffer[_bufferIndex] = TargetAcquisitions;
        _physicsTimeBuffer[_bufferIndex] = PhysicsProcessTimeUsec;
        _activeUnitsBuffer[_bufferIndex] = ActiveUnits;

        _bufferIndex = (_bufferIndex + 1) % FrameBufferSize;
        if (_frameCount < FrameBufferSize)
            _frameCount++;

        // Calculate averages
        CalculateAverages();

        // Reset for next frame
        SummonerLookups = 0;
        TargetAcquisitions = 0;
        PhysicsProcessTimeUsec = 0UL;
        ActiveUnits = 0;
    }

    private static void CalculateAverages()
    {
        if (_frameCount == 0)
            return;

        int count = _frameCount;

        int sumLookups = 0;
        int sumAcquisitions = 0;
        ulong sumPhysics = 0;
        int sumUnits = 0;

        for (int i = 0; i < count; i++)
        {
            sumLookups += _summonerLookupsBuffer[i];
            sumAcquisitions += _targetAcquisitionsBuffer[i];
            sumPhysics += _physicsTimeBuffer[i];
            sumUnits += _activeUnitsBuffer[i];
        }

        AvgSummonerLookups = sumLookups / (float)count;
        AvgTargetAcquisitions = sumAcquisitions / (float)count;
        AvgPhysicsTimeUsec = sumPhysics / (float)count;
        AvgActiveUnits = sumUnits / (float)count;
    }

    /// <summary>
    /// Get a formatted summary for display.
    /// </summary>
    public static string GetSummary()
    {
        return $"Units: {AvgActiveUnits:F0}\n" +
               $"Summoner Lookups: {AvgSummonerLookups:F1}\n" +
               $"Target Acquisitions: {AvgTargetAcquisitions:F1}\n" +
               $"Physics Time: {AvgPhysicsTimeUsec:F0}us ({AvgUsecPerUnit:F1}us/unit)";
    }
}
