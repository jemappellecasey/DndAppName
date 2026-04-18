using DndApp.Api.MixedRules;

namespace DndApp.Api.Characters;

public enum CharacterBuildMethod
{
    PointBuy,
    Manual,
    Roll,
}

public sealed record CharacterClassLevelData(
    string ClassModuleId,
    string ClassName,
    int Level,
    int SortOrder);

public sealed record CharacterSelectedModuleData(
    string Slot,
    string ModuleId,
    string DisplayName,
    string SourceCode);

public sealed record CharacterBuildData(
    Guid CharacterId,
    string CharacterName,
    RuleSystemMode BaseRuleSystem,
    CharacterBuildMethod BuildMethod,
    string ClassModuleId,
    string ClassName,
    int Level,
    int ProficiencyBonus,
    IReadOnlyDictionary<string, int> AbilityScores,
    IReadOnlyDictionary<string, string> SkillTrainingBySkill,
    IReadOnlyList<string> ProficientSkills,
    IReadOnlyList<string> SaveProficiencies,
    IReadOnlyList<CharacterClassLevelData> ClassLevels,
    IReadOnlyList<CharacterSelectedModuleData> SelectedModules,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record UpsertCharacterBuildRequest(
    string CharacterName,
    RuleSystemMode BaseRuleSystem,
    CharacterBuildMethod BuildMethod,
    string ClassModuleId,
    string ClassName,
    int Level,
    int ProficiencyBonus,
    IReadOnlyDictionary<string, int> AbilityScores,
    IReadOnlyList<string> ProficientSkills,
    IReadOnlyDictionary<string, string>? SkillTrainingBySkill = null,
    IReadOnlyList<CharacterClassLevelData>? ClassLevels = null,
    IReadOnlyList<CharacterSelectedModuleData>? SelectedModules = null);

public sealed record PatchCharacterBuildRequest(
    string? CharacterName,
    RuleSystemMode? BaseRuleSystem,
    CharacterBuildMethod? BuildMethod,
    string? ClassModuleId,
    string? ClassName,
    int? Level,
    int? ProficiencyBonus,
    IReadOnlyDictionary<string, int>? AbilityScores,
    IReadOnlyList<string>? ProficientSkills,
    IReadOnlyDictionary<string, string>? SkillTrainingBySkill = null,
    IReadOnlyList<CharacterClassLevelData>? ClassLevels = null,
    IReadOnlyList<CharacterSelectedModuleData>? SelectedModules = null);
