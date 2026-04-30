namespace DndApp.Api.Data;

public sealed class SkillProficiencySourceEntity
{
    public string Id { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;  // 'class', 'race', 'background', 'feat', 'subclass'
    public string SourceId { get; set; } = string.Empty;    // module ID from rule_module
    public string SourceName { get; set; } = string.Empty;  // display name
    public string SkillName { get; set; } = string.Empty;   // e.g., "Acrobatics"
    public bool IsExpertise { get; set; } = false;          // double proficiency
    public bool IsChoice { get; set; } = false;             // user must choose
    public string? ChoiceGroup { get; set; }                // e.g., "choose-4-from-8"
    public string Edition { get; set; } = string.Empty;    // '2014' or '2024'
}
