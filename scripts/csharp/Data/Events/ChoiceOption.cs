using Fateforged.Meta.Campaign;

namespace Fateforged.Data.Events;

/// <summary>
/// Represents a choice option at a branching path.
/// </summary>
public class ChoiceOption
{
    /// <summary>Unique identifier for this choice</summary>
    public ChoiceId Id { get; set; } = ChoiceId.None;

    /// <summary>Localization key for the choice label</summary>
    public string LabelKey { get; set; } = "";

    /// <summary>Localization key for the choice description</summary>
    public string DescriptionKey { get; set; } = "";

    public ChoiceOption() { }

    public ChoiceOption(ChoiceId id, string labelKey, string descriptionKey)
    {
        Id = id;
        LabelKey = labelKey;
        DescriptionKey = descriptionKey;
    }
}
