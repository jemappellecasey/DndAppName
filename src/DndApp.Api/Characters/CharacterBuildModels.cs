using DndApp.Api.MixedRules;

namespace DndApp.Api.Characters;

public enum CharacterBuildMethod
{
    PointBuy,
    Manual,
    Roll,
}

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
    IReadOnlyList<string> ProficientSkills,
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
    IReadOnlyList<string> ProficientSkills);

public sealed record PatchCharacterBuildRequest(
    string? CharacterName,
    RuleSystemMode? BaseRuleSystem,
    CharacterBuildMethod? BuildMethod,
    string? ClassModuleId,
    string? ClassName,
    int? Level,
    int? ProficiencyBonus,
    IReadOnlyDictionary<string, int>? AbilityScores,
    IReadOnlyList<string>? ProficientSkills);
