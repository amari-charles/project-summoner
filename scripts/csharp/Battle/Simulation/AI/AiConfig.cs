namespace Fateforged.Simulation.AI;

public enum AiType
{
    None,
    Simple,
    Heuristic,
    Scripted,
}

public enum AiPersonality
{
    Balanced,
    Aggressive,
    Defensive,
    SpellFocused,
}

/// <summary>
/// A single step in a scripted AI sequence.
/// At TriggerTime seconds into the match, play the card matching CardId.
/// </summary>
public struct ScriptedAiStep
{
    public float TriggerTime { get; set; }
    public string CardId { get; set; }
    public SimVector3 SpawnPosition { get; set; }

    public ScriptedAiStep(float triggerTime, string cardId, SimVector3 spawnPosition)
    {
        TriggerTime = triggerTime;
        CardId = cardId;
        SpawnPosition = spawnPosition;
    }
}

/// <summary>
/// AI configuration attached to a SummonerData.
/// Null Ai on SummonerData means human-controlled.
/// </summary>
public class AiConfig
{
    public AiType Type { get; set; } = AiType.Heuristic;
    public AiPersonality Personality { get; set; } = AiPersonality.Balanced;
    public int Difficulty { get; set; } = 3;
    public float PlayIntervalMin { get; set; } = 3.0f;
    public float PlayIntervalMax { get; set; } = 6.0f;
    public ScriptedAiStep[]? Script { get; set; }

    /// <summary>
    /// Optional authored spawn rectangle for compact or unusual encounters.
    /// Standard battles leave this unset and use the global battlefield zones.
    /// </summary>
    public float? SpawnMinX { get; set; }
    public float? SpawnMaxX { get; set; }
    public float? SpawnMinZ { get; set; }
    public float? SpawnMaxZ { get; set; }

    public bool HasSpawnBounds =>
        SpawnMinX.HasValue
        && SpawnMaxX.HasValue
        && SpawnMinZ.HasValue
        && SpawnMaxZ.HasValue
        && SpawnMaxX.Value >= SpawnMinX.Value
        && SpawnMaxZ.Value >= SpawnMinZ.Value;
}
