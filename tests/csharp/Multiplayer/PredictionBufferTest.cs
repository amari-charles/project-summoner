namespace ProjectSummoner.Tests.Multiplayer;

using GdUnit4;
using Godot;
using Fateforged.Multiplayer.Client;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for PredictionBuffer operations.
/// </summary>
[TestSuite]
public class PredictionBufferTest
{
    private PredictionBuffer _buffer = null!;

    [BeforeTest]
    public void Setup()
    {
        _buffer = new PredictionBuffer();
    }

    [TestCase]
    public void Add_IncrementsCount()
    {
        var prediction = new CardPlayPrediction(1, 0, Vector3.Zero, 100L);

        _buffer.Add(prediction);

        AssertThat(_buffer.Count).IsEqual(1);
    }

    [TestCase]
    public void Get_ReturnsAddedPrediction()
    {
        var prediction = new CardPlayPrediction(42, 3, new Vector3(1, 0, 2), 12345L);
        _buffer.Add(prediction);

        var result = _buffer.Get(42);

        AssertThat(result).IsNotNull();
        AssertThat(result!.Sequence).IsEqual(42);
        AssertThat(result.CardIndex).IsEqual(3);
        AssertThat(result.Position.X).IsEqual(1f);
        AssertThat(result.Timestamp).IsEqual(12345L);
    }

    [TestCase]
    public void Get_ReturnsNullForMissingSequence()
    {
        var prediction = new CardPlayPrediction(1, 0, Vector3.Zero, 100L);
        _buffer.Add(prediction);

        var result = _buffer.Get(999);

        AssertThat(result).IsNull();
    }

    [TestCase]
    public void Remove_DecreasesCount()
    {
        _buffer.Add(new CardPlayPrediction(1, 0, Vector3.Zero, 100L));
        _buffer.Add(new CardPlayPrediction(2, 0, Vector3.Zero, 200L));
        AssertThat(_buffer.Count).IsEqual(2);

        _buffer.Remove(1);

        AssertThat(_buffer.Count).IsEqual(1);
    }

    [TestCase]
    public void Remove_MakesGetReturnNull()
    {
        var prediction = new CardPlayPrediction(5, 0, Vector3.Zero, 100L);
        _buffer.Add(prediction);
        AssertThat(_buffer.Get(5)).IsNotNull();

        _buffer.Remove(5);

        AssertThat(_buffer.Get(5)).IsNull();
    }

    [TestCase]
    public void Has_ReturnsTrueForExisting()
    {
        _buffer.Add(new CardPlayPrediction(10, 0, Vector3.Zero, 100L));

        AssertThat(_buffer.Has(10)).IsTrue();
    }

    [TestCase]
    public void Has_ReturnsFalseForMissing()
    {
        _buffer.Add(new CardPlayPrediction(10, 0, Vector3.Zero, 100L));

        AssertThat(_buffer.Has(999)).IsFalse();
    }

    [TestCase]
    public void GetAll_ReturnsAllPredictions()
    {
        _buffer.Add(new CardPlayPrediction(1, 0, Vector3.Zero, 100L));
        _buffer.Add(new CardPlayPrediction(2, 1, Vector3.One, 200L));
        _buffer.Add(new CardPlayPrediction(3, 2, Vector3.Up, 300L));

        var all = _buffer.GetAll();
        var count = 0;
        foreach (var _ in all) count++;

        AssertThat(count).IsEqual(3);
    }

    [TestCase]
    public void Clear_RemovesAllPredictions()
    {
        _buffer.Add(new CardPlayPrediction(1, 0, Vector3.Zero, 100L));
        _buffer.Add(new CardPlayPrediction(2, 0, Vector3.Zero, 200L));
        AssertThat(_buffer.Count).IsEqual(2);

        _buffer.Clear();

        AssertThat(_buffer.Count).IsEqual(0);
        AssertThat(_buffer.Get(1)).IsNull();
        AssertThat(_buffer.Get(2)).IsNull();
    }

    [TestCase]
    public void Add_OverwritesSameSequence()
    {
        var first = new CardPlayPrediction(1, 0, Vector3.Zero, 100L);
        var second = new CardPlayPrediction(1, 5, Vector3.One, 200L);

        _buffer.Add(first);
        _buffer.Add(second);

        AssertThat(_buffer.Count).IsEqual(1);
        var result = _buffer.Get(1);
        AssertThat(result!.CardIndex).IsEqual(5); // Should be the second one
    }
}
