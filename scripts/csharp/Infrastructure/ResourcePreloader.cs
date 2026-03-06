using Godot;
using System.Collections.Generic;

namespace Fateforged.Infrastructure;

[GlobalClass]
public partial class ResourcePreloader : Node
{
    [Signal]
    public delegate void ProgressUpdatedEventHandler(float progress);

    [Signal]
    public delegate void CompletedEventHandler();

    public bool IsLoading { get; private set; }

    private readonly List<string> _pendingPaths = new();
    private int _totalCount;

    public override void _Ready()
    {
        SetProcess(false);
    }

    public void LoadAll(string[] paths)
    {
        _pendingPaths.Clear();
        _totalCount = 0;

        if (paths == null || paths.Length == 0)
        {
            EmitSignal(SignalName.ProgressUpdated, 1.0f);
            EmitSignal(SignalName.Completed);
            return;
        }

        foreach (var path in paths)
        {
            if (string.IsNullOrEmpty(path))
                continue;

            if (!ResourceLoader.Exists(path))
            {
                GD.PushWarning($"ResourcePreloader: Path does not exist, skipping: {path}");
                continue;
            }

            var err = ResourceLoader.LoadThreadedRequest(path);
            if (err != Error.Ok)
            {
                GD.PushError($"ResourcePreloader: Failed to request '{path}': {err}");
                continue;
            }

            _pendingPaths.Add(path);
        }

        _totalCount = _pendingPaths.Count;

        if (_totalCount == 0)
        {
            EmitSignal(SignalName.ProgressUpdated, 1.0f);
            EmitSignal(SignalName.Completed);
            return;
        }

        IsLoading = true;
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        if (_pendingPaths.Count == 0)
        {
            _Finish();
            return;
        }

        var progressArray = new Godot.Collections.Array();
        for (int i = _pendingPaths.Count - 1; i >= 0; i--)
        {
            var path = _pendingPaths[i];
            var status = ResourceLoader.LoadThreadedGetStatus(path, progressArray);

            switch (status)
            {
                case ResourceLoader.ThreadLoadStatus.Loaded:
                    // Finalize the resource so it enters the cache
                    ResourceLoader.LoadThreadedGet(path);
                    _pendingPaths.RemoveAt(i);
                    break;

                case ResourceLoader.ThreadLoadStatus.Failed:
                case ResourceLoader.ThreadLoadStatus.InvalidResource:
                    GD.PushError($"ResourcePreloader: Failed to load '{path}' (status: {status})");
                    _pendingPaths.RemoveAt(i);
                    break;
            }
        }

        float completed = _totalCount - _pendingPaths.Count;
        float progress = completed / _totalCount;
        EmitSignal(SignalName.ProgressUpdated, progress);

        if (_pendingPaths.Count == 0)
        {
            _Finish();
        }
    }

    private void _Finish()
    {
        IsLoading = false;
        SetProcess(false);
        EmitSignal(SignalName.ProgressUpdated, 1.0f);
        EmitSignal(SignalName.Completed);
    }
}
