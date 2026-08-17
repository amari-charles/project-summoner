using Fateforged.Data.Quests;
using Godot.Collections;

namespace Fateforged.Meta.Campaign.Quests;

public interface IQuestRuleHandler
{
    string Kind { get; }

    bool CanApply(QuestRuleDefinition rule);

    bool Apply(QuestRuleDefinition rule);

    Dictionary GetPreview(QuestRuleDefinition rule);
}
