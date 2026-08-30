namespace Fateforged.Application.UiTutorial;

using System;
using System.Linq;
using Fateforged.Data.Quests;
using Godot;

/// <summary>
/// Immutable process-wide switch for the designer-facing UI walkthrough.
/// Review exports opt in through the ui_tutorial custom feature; editor and
/// command-line runs can opt in with --ui-tutorial.
/// </summary>
[GlobalClass]
public partial class UiTutorialModeService : Node
{
    public const string FeatureName = "ui_tutorial";
    public const string CommandLineFlag = "--ui-tutorial";

    public static UiTutorialModeService? Instance { get; private set; }

    public static bool EnabledForCurrentRun { get; } = ResolveEnabled(
        OS.HasFeature(FeatureName),
        OS.GetCmdlineUserArgs()
    );

    public static string CurrentRuntimeMode =>
        EnabledForCurrentRun ? QuestRuntimeModes.UiTutorial : QuestRuntimeModes.Normal;

    public override void _EnterTree()
    {
        Instance = this;
        if (EnabledForCurrentRun)
            GD.Print("[UI Tutorial] Walkthrough mode enabled");
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool IsEnabled() => EnabledForCurrentRun;

    public string GetRuntimeMode() => CurrentRuntimeMode;

    public static bool ResolveEnabled(bool hasFeature, string[] userArgs) =>
        hasFeature || userArgs.Any(arg => string.Equals(arg, CommandLineFlag, StringComparison.Ordinal));
}
