namespace DndApp.Api.MixedRules;

public enum RuleSystemMode
{
    Rules2014,
    Rules2024,
}

public sealed record RuleModuleSelection(
    string Slot,
    string ModuleId,
    string SourceCode,
    bool Compatible2014,
    bool Compatible2024);

public sealed record MixedRulesResolveRequest(
    RuleSystemMode BaseRuleSystem,
    bool MixedModeEnabled,
    IReadOnlyList<string> OverlaySources,
    IReadOnlyList<RuleModuleSelection> Selections,
    IReadOnlyDictionary<string, string>? ExplicitOverridesBySlot);

public sealed record ResolvedSlot(
    string Slot,
    RuleModuleSelection Winner,
    IReadOnlyList<RuleModuleSelection> Candidates,
    string ResolutionReason);

public sealed record MixedRulesResolveResult(
    IReadOnlyList<ResolvedSlot> ResolvedSlots,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors);
