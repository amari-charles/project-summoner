namespace ProjectSummoner.Data.Events;

/// <summary>
/// Represents a choice option at a branching path.
/// </summary>
public class ChoiceOption
{
    /// <summary>Unique identifier for this choice</summary>
    public string Id { get; set; } = "";

    /// <summary>Localization key for the choice label</summary>
    public string LabelKey { get; set; } = "";

    /// <summary>Localization key for the choice description</summary>
    public string DescriptionKey { get; set; } = "";

    public ChoiceOption() { }

    public ChoiceOption(string id, string labelKey, string descriptionKey)
    {
        Id = id;
        LabelKey = labelKey;
        DescriptionKey = descriptionKey;
    }
}
