using DndApp.Api.MixedRules;

namespace DndApp.Api.Characters;

public sealed record RulesProfile(
    RuleSystemMode BaseRuleSystem,
    bool MixedModeEnabled,
    IReadOnlyList<string> OverlaySources);

public sealed record StartCharacterWizardRequest(
    string CharacterName,
    RulesProfile RulesProfile);

public sealed record WizardStepState(
    string StepName,
    IReadOnlyList<RuleModuleSelection> Selections,
    DateTimeOffset SubmittedAtUtc);

public sealed record SubmitWizardStepRequest(
    string StepName,
    IReadOnlyList<RuleModuleSelection> Selections);

public sealed record FinalizeWizardRequest(
    IReadOnlyDictionary<string, string>? ExplicitOverridesBySlot);

public sealed record CopyCharacterRulesetRequest(
    RuleSystemMode TargetRuleSystem,
    bool CarrySelectionsForward,
    IReadOnlyList<string> TargetOverlaySources);

public sealed record CharacterWizardDraft(
    Guid CharacterId,
    string CharacterName,
    RulesProfile RulesProfile,
    bool IsFinalized,
    IReadOnlyList<WizardStepState> Steps,
    IReadOnlyList<string> Warnings);

public sealed record CharacterWizardResult(
    bool IsSuccess,
    CharacterWizardDraft? Draft,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);
