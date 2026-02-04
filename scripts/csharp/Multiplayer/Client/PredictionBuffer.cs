using System.Collections.Generic;

namespace Fateforged.Multiplayer.Client;

/// <summary>
/// Buffer for tracking unconfirmed predictions.
/// Predictions are removed when confirmed by the server or rolled back when rejected.
/// </summary>
public class PredictionBuffer
{
    private readonly Dictionary<int, CardPlayPrediction> _predictions = new();

    /// <summary>
    /// Number of pending predictions.
    /// </summary>
    public int Count => _predictions.Count;

    /// <summary>
    /// Add a prediction to the buffer.
    /// </summary>
    public void Add(CardPlayPrediction prediction)
    {
        _predictions[prediction.Sequence] = prediction;
    }

    /// <summary>
    /// Get a prediction by sequence number.
    /// </summary>
    public CardPlayPrediction? Get(int sequence)
    {
        return _predictions.TryGetValue(sequence, out var prediction) ? prediction : null;
    }

    /// <summary>
    /// Remove a prediction (confirmed or rejected).
    /// </summary>
    public void Remove(int sequence)
    {
        _predictions.Remove(sequence);
    }

    /// <summary>
    /// Check if a prediction exists.
    /// </summary>
    public bool Has(int sequence)
    {
        return _predictions.ContainsKey(sequence);
    }

    /// <summary>
    /// Get all pending predictions (for reconciliation).
    /// </summary>
    public IEnumerable<CardPlayPrediction> GetAll()
    {
        return _predictions.Values;
    }

    /// <summary>
    /// Clear all predictions (on match end or full resync).
    /// </summary>
    public void Clear()
    {
        _predictions.Clear();
    }
}
