using System.Collections.Generic;

namespace Fateforged.Data.Academy;

/// <summary>
/// Authored identity and campus ownership for a professor who offers academy quests.
/// Presentation and exact world coordinates remain scene-owned.
/// </summary>
public sealed class AcademyProfessorDefinition
{
    public ProfessorId Id { get; set; } = ProfessorId.None;

    public string NameKey { get; set; } = "";

    public string RoleKey { get; set; } = "";

    public string LandmarkKey { get; set; } = "";

    public List<string> QuestIds { get; set; } = [];
}
