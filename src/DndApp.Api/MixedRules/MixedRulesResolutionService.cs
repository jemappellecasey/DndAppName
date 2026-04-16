namespace DndApp.Api.MixedRules;

public interface IMixedRulesResolutionService
{
    MixedRulesResolveResult Resolve(MixedRulesResolveRequest request);
}

public sealed class MixedRulesResolutionService : IMixedRulesResolutionService
{
    public MixedRulesResolveResult Resolve(MixedRulesResolveRequest request)
    {
        var warnings = new List<string>();
        var errors = new List<string>();
        var resolved = new List<ResolvedSlot>();
        var overlays = new HashSet<string>(request.OverlaySources, StringComparer.OrdinalIgnoreCase);
        var explicitOverrides = request.ExplicitOverridesBySlot
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var selection in request.Selections)
        {
            if (!IsCompatible(selection, request.BaseRuleSystem))
            {
                errors.Add($"Selection '{selection.ModuleId}' in slot '{selection.Slot}' is not compatible with base rules {request.BaseRuleSystem}.");
            }

            var isBaseSource = MatchesBaseSystem(selection.SourceCode, request.BaseRuleSystem);
            if (!request.MixedModeEnabled && !isBaseSource)
            {
                errors.Add($"Selection '{selection.ModuleId}' in slot '{selection.Slot}' is from non-base source '{selection.SourceCode}' while mixed mode is disabled.");
            }

            if (request.MixedModeEnabled && !isBaseSource && !overlays.Contains(selection.SourceCode))
            {
                errors.Add($"Selection '{selection.ModuleId}' in slot '{selection.Slot}' uses source '{selection.SourceCode}' which is not in overlays.");
            }
        }

        var grouped = request.Selections.GroupBy(x => x.Slot, StringComparer.OrdinalIgnoreCase);
        foreach (var slotGroup in grouped)
        {
            var candidates = slotGroup.ToList();
            RuleModuleSelection winner;
            string reason;

            if (candidates.Count == 1)
            {
                winner = candidates[0];
                reason = "Single candidate";
            }
            else if (explicitOverrides.TryGetValue(slotGroup.Key, out var overrideModuleId))
            {
                winner = candidates.FirstOrDefault(c => string.Equals(c.ModuleId, overrideModuleId, StringComparison.OrdinalIgnoreCase))
                    ?? candidates[0];
                reason = "Explicit override";
                warnings.Add($"Conflict in slot '{slotGroup.Key}' resolved by explicit override '{winner.ModuleId}'.");
            }
            else
            {
                winner = candidates.FirstOrDefault(c => MatchesBaseSystem(c.SourceCode, request.BaseRuleSystem)) ?? candidates[0];
                reason = "Base rules precedence";
                warnings.Add($"Conflict in slot '{slotGroup.Key}' auto-resolved to '{winner.ModuleId}' using base rules precedence.");
            }

            resolved.Add(new ResolvedSlot(slotGroup.Key, winner, candidates, reason));
        }

        return new MixedRulesResolveResult(resolved, warnings, errors);
    }

    private static bool IsCompatible(RuleModuleSelection selection, RuleSystemMode baseRuleSystem)
    {
        return baseRuleSystem == RuleSystemMode.Rules2014 ? selection.Compatible2014 : selection.Compatible2024;
    }

    private static bool MatchesBaseSystem(string sourceCode, RuleSystemMode baseRuleSystem)
    {
        return baseRuleSystem == RuleSystemMode.Rules2014
            ? sourceCode.Contains("2014", StringComparison.OrdinalIgnoreCase)
            : sourceCode.Contains("2024", StringComparison.OrdinalIgnoreCase);
    }
}
