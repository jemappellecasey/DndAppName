using System.Collections.Concurrent;
using DndApp.Api.MixedRules;

namespace DndApp.Api.Characters;

public interface ICharacterWizardService
{
    CharacterWizardResult StartDraft(StartCharacterWizardRequest request);
    CharacterWizardDraft? GetDraft(Guid characterId);
    IReadOnlyList<CharacterSummary> ListCharacters(bool includeArchived);
    CharacterSummary? GetCharacter(Guid characterId);
    CharacterSummary? UpdateCharacter(Guid characterId, UpdateCharacterRequest request);
    CharacterSummary? ArchiveCharacter(Guid characterId);
    CharacterSummary? DuplicateCharacter(Guid characterId, DuplicateCharacterRequest request);
    CharacterWizardResult SubmitStep(Guid characterId, SubmitWizardStepRequest request);
    CharacterWizardResult Finalize(Guid characterId, FinalizeWizardRequest request);
    CharacterWizardResult CopyToRuleset(Guid characterId, CopyCharacterRulesetRequest request);
}

public sealed class CharacterWizardService : ICharacterWizardService
{
    private readonly ConcurrentDictionary<Guid, CharacterWizardDraft> _drafts = new();
    private readonly ConcurrentDictionary<Guid, CharacterRecord> _characters = new();
    private readonly IMixedRulesResolutionService _resolver;

    public CharacterWizardService(IMixedRulesResolutionService resolver)
    {
        _resolver = resolver;
    }

    public CharacterWizardResult StartDraft(StartCharacterWizardRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.CharacterName))
        {
            errors.Add("Character name is required.");
        }

        if (!request.RulesProfile.MixedModeEnabled && request.RulesProfile.OverlaySources.Count > 0)
        {
            errors.Add("Overlay sources can only be set when mixed mode is enabled.");
        }

        if (errors.Count > 0)
        {
            return new CharacterWizardResult(false, null, errors, Array.Empty<string>());
        }

        var draft = new CharacterWizardDraft(
            Guid.NewGuid(),
            request.CharacterName.Trim(),
            request.RulesProfile,
            false,
            Array.Empty<WizardStepState>(),
            Array.Empty<string>());

        _drafts[draft.CharacterId] = draft;
        return new CharacterWizardResult(true, draft, Array.Empty<string>(), Array.Empty<string>());
    }

    public CharacterWizardDraft? GetDraft(Guid characterId)
    {
        return _drafts.TryGetValue(characterId, out var draft) ? draft : null;
    }

    public IReadOnlyList<CharacterSummary> ListCharacters(bool includeArchived)
    {
        return _characters.Values
            .Where(x => includeArchived || !x.IsArchived)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Select(ToSummary)
            .ToArray();
    }

    public CharacterSummary? GetCharacter(Guid characterId)
    {
        return _characters.TryGetValue(characterId, out var character) ? ToSummary(character) : null;
    }

    public CharacterSummary? UpdateCharacter(Guid characterId, UpdateCharacterRequest request)
    {
        if (!_characters.TryGetValue(characterId, out var character))
        {
            return null;
        }

        var name = string.IsNullOrWhiteSpace(request.CharacterName) ? character.CharacterName : request.CharacterName.Trim();
        var updated = character with
        {
            CharacterName = name,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        _characters[characterId] = updated;
        return ToSummary(updated);
    }

    public CharacterSummary? ArchiveCharacter(Guid characterId)
    {
        if (!_characters.TryGetValue(characterId, out var character))
        {
            return null;
        }

        var updated = character with
        {
            IsArchived = true,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        _characters[characterId] = updated;
        return ToSummary(updated);
    }

    public CharacterSummary? DuplicateCharacter(Guid characterId, DuplicateCharacterRequest request)
    {
        if (!_characters.TryGetValue(characterId, out var character))
        {
            return null;
        }

        var suffix = string.IsNullOrWhiteSpace(request.NameSuffix) ? "Copy" : request.NameSuffix.Trim();
        var now = DateTimeOffset.UtcNow;
        var duplicate = new CharacterRecord(
            Guid.NewGuid(),
            $"{character.CharacterName} ({suffix})",
            character.RulesProfile,
            false,
            now,
            now);

        _characters[duplicate.CharacterId] = duplicate;
        return ToSummary(duplicate);
    }

    public CharacterWizardResult SubmitStep(Guid characterId, SubmitWizardStepRequest request)
    {
        if (!_drafts.TryGetValue(characterId, out var draft))
        {
            return new CharacterWizardResult(false, null, new[] { "Character draft not found." }, Array.Empty<string>());
        }

        if (draft.IsFinalized)
        {
            return new CharacterWizardResult(false, draft, new[] { "Character draft is already finalized." }, Array.Empty<string>());
        }

        if (string.IsNullOrWhiteSpace(request.StepName))
        {
            return new CharacterWizardResult(false, draft, new[] { "Step name is required." }, Array.Empty<string>());
        }

        var errors = ValidateSelectionsAgainstRulesMode(draft.RulesProfile, request.Selections);
        if (errors.Count > 0)
        {
            return new CharacterWizardResult(false, draft, errors, Array.Empty<string>());
        }

        var nextSteps = draft.Steps
            .Where(s => !string.Equals(s.StepName, request.StepName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        nextSteps.Add(new WizardStepState(request.StepName.Trim(), request.Selections, DateTimeOffset.UtcNow));
        var updated = draft with { Steps = nextSteps };
        _drafts[characterId] = updated;
        return new CharacterWizardResult(true, updated, Array.Empty<string>(), Array.Empty<string>());
    }

    public CharacterWizardResult Finalize(Guid characterId, FinalizeWizardRequest request)
    {
        if (!_drafts.TryGetValue(characterId, out var draft))
        {
            return new CharacterWizardResult(false, null, new[] { "Character draft not found." }, Array.Empty<string>());
        }

        var allSelections = draft.Steps.SelectMany(s => s.Selections).ToList();
        var resolveResult = _resolver.Resolve(
            new MixedRulesResolveRequest(
                draft.RulesProfile.BaseRuleSystem,
                draft.RulesProfile.MixedModeEnabled,
                draft.RulesProfile.OverlaySources,
                allSelections,
                request.ExplicitOverridesBySlot));

        if (resolveResult.Errors.Count > 0)
        {
            return new CharacterWizardResult(false, draft, resolveResult.Errors, resolveResult.Warnings);
        }

        var finalized = draft with
        {
            IsFinalized = true,
            Warnings = resolveResult.Warnings
        };
        _drafts[characterId] = finalized;

        var now = DateTimeOffset.UtcNow;
        var character = new CharacterRecord(
            finalized.CharacterId,
            finalized.CharacterName,
            finalized.RulesProfile,
            false,
            now,
            now);
        _characters[finalized.CharacterId] = character;

        return new CharacterWizardResult(true, finalized, Array.Empty<string>(), resolveResult.Warnings);
    }

    public CharacterWizardResult CopyToRuleset(Guid characterId, CopyCharacterRulesetRequest request)
    {
        if (!_drafts.TryGetValue(characterId, out var draft))
        {
            return new CharacterWizardResult(false, null, new[] { "Character draft not found." }, Array.Empty<string>());
        }

        var warnings = new List<string>
        {
            "Base ruleset is locked after creation. This operation creates a copied character in the target ruleset."
        };

        var copiedSteps = request.CarrySelectionsForward ? draft.Steps : Array.Empty<WizardStepState>();
        if (!request.CarrySelectionsForward)
        {
            warnings.Add("Selections were not carried forward; review all wizard steps in the copied character.");
        }
        else
        {
            warnings.Add("Selections were carried forward; review compatibility conflicts in the target ruleset.");
        }

        var copied = new CharacterWizardDraft(
            Guid.NewGuid(),
            $"{draft.CharacterName} (Copy)",
            new RulesProfile(request.TargetRuleSystem, request.TargetOverlaySources.Count > 0, request.TargetOverlaySources),
            false,
            copiedSteps,
            warnings);

        _drafts[copied.CharacterId] = copied;
        return new CharacterWizardResult(true, copied, Array.Empty<string>(), warnings);
    }

    private static CharacterSummary ToSummary(CharacterRecord character)
    {
        return new CharacterSummary(
            character.CharacterId,
            character.CharacterName,
            character.RulesProfile.BaseRuleSystem,
            character.RulesProfile.MixedModeEnabled,
            character.IsArchived,
            character.CreatedAtUtc,
            character.UpdatedAtUtc);
    }

    private static List<string> ValidateSelectionsAgainstRulesMode(RulesProfile profile, IReadOnlyList<RuleModuleSelection> selections)
    {
        var errors = new List<string>();
        var overlays = new HashSet<string>(profile.OverlaySources, StringComparer.OrdinalIgnoreCase);

        foreach (var selection in selections)
        {
            var isCompatible = profile.BaseRuleSystem == RuleSystemMode.Rules2014
                ? selection.Compatible2014
                : selection.Compatible2024;

            if (!isCompatible)
            {
                errors.Add($"Selection '{selection.ModuleId}' in slot '{selection.Slot}' is incompatible with {profile.BaseRuleSystem}.");
            }

            var isBaseSource = profile.BaseRuleSystem == RuleSystemMode.Rules2014
                ? selection.SourceCode.Contains("2014", StringComparison.OrdinalIgnoreCase)
                : selection.SourceCode.Contains("2024", StringComparison.OrdinalIgnoreCase);

            if (!profile.MixedModeEnabled && !isBaseSource)
            {
                errors.Add($"Selection '{selection.ModuleId}' in slot '{selection.Slot}' requires mixed mode to use source '{selection.SourceCode}'.");
            }

            if (profile.MixedModeEnabled && !isBaseSource && !overlays.Contains(selection.SourceCode))
            {
                errors.Add($"Selection '{selection.ModuleId}' in slot '{selection.Slot}' uses source '{selection.SourceCode}' that is not in selected overlays.");
            }
        }

        return errors;
    }

    private sealed record CharacterRecord(
        Guid CharacterId,
        string CharacterName,
        RulesProfile RulesProfile,
        bool IsArchived,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);
}
