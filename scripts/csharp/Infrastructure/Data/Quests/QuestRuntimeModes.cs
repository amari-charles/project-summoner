namespace Fateforged.Data.Quests;

public static class QuestRuntimeModes
{
    public const string Normal = "normal";
    public const string UiTutorial = "ui_tutorial";

    public static bool IsKnown(string value) => value is Normal or UiTutorial;
}
