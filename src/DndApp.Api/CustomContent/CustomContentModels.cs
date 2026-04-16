namespace DndApp.Api.CustomContent;

public enum CustomContentMode
{
    GuidedCustom,
    FullyCustom,
}

public sealed record AbilityBonusEntry(string Ability, int Bonus);

public sealed record CreateCustomOriginRequest(
    string Name,
    CustomContentMode Mode,
    IReadOnlyList<AbilityBonusEntry> AbilityBonuses,
    IReadOnlyList<string> SkillProficiencies,
    IReadOnlyList<string> FeatureNotes);

public sealed record CreateCustomSpeciesRequest(
    string Name,
    CustomContentMode Mode,
    string Size,
    int WalkingSpeed,
    IReadOnlyList<string> Traits,
    IReadOnlyList<string> Languages);

public sealed record ValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);

public sealed record BuilderGuidance(
    string Title,
    IReadOnlyList<string> Rules,
    IReadOnlyList<string> Tips);

public sealed record OriginPreviewResult(
    ValidationResult Validation,
    int TotalAbilityBonus,
    IReadOnlyList<string> DerivedTags);

public sealed record SpeciesPreviewResult(
    ValidationResult Validation,
    int WalkingSpeed,
    IReadOnlyList<string> DerivedTags);
