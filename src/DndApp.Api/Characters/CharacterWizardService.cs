using System.Text.Json;
using DndApp.Api.Data;
using DndApp.Api.MixedRules;
using Microsoft.EntityFrameworkCore;

namespace DndApp.Api.Characters;

public interface ICharacterWizardService
{
    Task<CharacterWizardResult> StartDraftAsync(StartCharacterWizardRequest request, CancellationToken cancellationToken);
    Task<CharacterWizardDraft?> GetDraftAsync(Guid characterId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CharacterSummary>> ListCharactersAsync(bool includeArchived, bool archivedOnly, string? ownerUserId, CancellationToken cancellationToken);
    Task<CharacterSummary?> GetCharacterAsync(Guid characterId, CancellationToken cancellationToken);
    Task<CharacterSummary?> UpdateCharacterAsync(Guid characterId, UpdateCharacterRequest request, CancellationToken cancellationToken);
    Task<CharacterSummary?> ArchiveCharacterAsync(Guid characterId, CancellationToken cancellationToken);
    Task<CharacterSummary?> RestoreCharacterAsync(Guid characterId, CancellationToken cancellationToken);
    Task<bool> DeleteCharacterAsync(Guid characterId, CancellationToken cancellationToken);
    Task<CharacterSummary?> DuplicateCharacterAsync(Guid characterId, DuplicateCharacterRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<CharacterRevisionEntry>> GetCharacterHistoryAsync(Guid characterId, CancellationToken cancellationToken);
    Task<CharacterWizardResult> SubmitStepAsync(Guid characterId, SubmitWizardStepRequest request, CancellationToken cancellationToken);
    Task<CharacterWizardResult> FinalizeAsync(Guid characterId, FinalizeWizardRequest request, CancellationToken cancellationToken);
    Task<CharacterWizardResult> CopyToRulesetAsync(Guid characterId, CopyCharacterRulesetRequest request, CancellationToken cancellationToken);
}

public sealed class CharacterWizardService : ICharacterWizardService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _db;
    private readonly IMixedRulesResolutionService _resolver;

    public CharacterWizardService(AppDbContext db, IMixedRulesResolutionService resolver)
    {
        _db = db;
        _resolver = resolver;
    }

    public async Task<CharacterWizardResult> StartDraftAsync(StartCharacterWizardRequest request, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.SessionToken))
        {
            errors.Add("Session token is required.");
        }
        if (!request.RulesProfile.MixedModeEnabled && request.RulesProfile.OverlaySources.Count > 0)
        {
            errors.Add("Overlay sources can only be set when mixed mode is enabled.");
        }
        if (errors.Count > 0)
        {
            return new CharacterWizardResult(false, null, errors, Array.Empty<string>());
        }

        var now = DateTimeOffset.UtcNow;
        var characterId = Guid.NewGuid();
        var characterName = string.IsNullOrWhiteSpace(request.CharacterName) ? "New Adventurer" : request.CharacterName.Trim();
        var draftEntity = new CharacterDraftEntity
        {
            CharacterId = characterId.ToString(),
            OwnerUserId = request.SessionToken.Trim(),
            CharacterName = characterName,
            BaseRuleSystem = request.RulesProfile.BaseRuleSystem.ToString(),
            MixedModeEnabled = request.RulesProfile.MixedModeEnabled,
            OverlaySourcesJson = SerializeJson(request.RulesProfile.OverlaySources),
            IsFinalized = false,
            StepsJson = SerializeJson(Array.Empty<WizardStepState>()),
            WarningsJson = SerializeJson(Array.Empty<string>()),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _db.CharacterDrafts.Add(draftEntity);
        AppendHistory(characterId, "draft-started", draftEntity.OwnerUserId, "Character wizard draft created.");
        await _db.SaveChangesAsync(cancellationToken);

        return new CharacterWizardResult(
            true,
            ToDraft(draftEntity),
            Array.Empty<string>(),
            Array.Empty<string>());
    }

    public async Task<CharacterWizardDraft?> GetDraftAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var entity = await _db.CharacterDrafts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CharacterId == characterId.ToString(), cancellationToken);
        return entity is null ? null : ToDraft(entity);
    }

    public async Task<IReadOnlyList<CharacterSummary>> ListCharactersAsync(bool includeArchived, bool archivedOnly, string? ownerUserId, CancellationToken cancellationToken)
    {
        var query = _db.CharacterRecords.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(ownerUserId))
        {
            query = query.Where(x => x.OwnerUserId == ownerUserId);
        }

        if (archivedOnly)
        {
            query = query.Where(x => x.IsArchived);
        }
        else if (!includeArchived)
        {
            query = query.Where(x => !x.IsArchived);
        }

        // SQLite cannot translate DateTimeOffset ORDER BY, so materialize and sort in memory.
        var entities = await query.ToArrayAsync(cancellationToken);
        return entities
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Select(ToSummary)
            .ToArray();
    }

    public async Task<CharacterSummary?> GetCharacterAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var entity = await _db.CharacterRecords
            .AsNoTracking()
            .Where(x => x.CharacterId == characterId.ToString())
            .FirstOrDefaultAsync(cancellationToken);
        return entity is null ? null : ToSummary(entity);
    }

    public async Task<CharacterSummary?> UpdateCharacterAsync(Guid characterId, UpdateCharacterRequest request, CancellationToken cancellationToken)
    {
        var entity = await _db.CharacterRecords.FirstOrDefaultAsync(x => x.CharacterId == characterId.ToString(), cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.CharacterName))
        {
            entity.CharacterName = request.CharacterName.Trim();
        }
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;
        AppendHistory(characterId, "character-updated", entity.OwnerUserId, $"Character renamed to '{entity.CharacterName}'.");
        await _db.SaveChangesAsync(cancellationToken);
        return ToSummary(entity);
    }

    public async Task<CharacterSummary?> ArchiveCharacterAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var entity = await _db.CharacterRecords.FirstOrDefaultAsync(x => x.CharacterId == characterId.ToString(), cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.IsArchived = true;
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;
        AppendHistory(characterId, "character-archived", entity.OwnerUserId, "Character archived (soft delete).");
        await _db.SaveChangesAsync(cancellationToken);
        return ToSummary(entity);
    }

    public async Task<CharacterSummary?> RestoreCharacterAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var entity = await _db.CharacterRecords.FirstOrDefaultAsync(x => x.CharacterId == characterId.ToString(), cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.IsArchived = false;
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;
        AppendHistory(characterId, "character-restored", entity.OwnerUserId, "Character restored from archive.");
        await _db.SaveChangesAsync(cancellationToken);
        return ToSummary(entity);
    }

    public async Task<bool> DeleteCharacterAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var id = characterId.ToString();
        var record = await _db.CharacterRecords.FirstOrDefaultAsync(x => x.CharacterId == id, cancellationToken);
        if (record is null)
        {
            return false;
        }

        _db.CharacterHistoryEntries.RemoveRange(_db.CharacterHistoryEntries.Where(x => x.CharacterId == id));
        _db.CharacterDrafts.RemoveRange(_db.CharacterDrafts.Where(x => x.CharacterId == id));
        _db.CharacterVitals.RemoveRange(_db.CharacterVitals.Where(x => x.CharacterId == id));
        _db.CharacterResourcePools.RemoveRange(_db.CharacterResourcePools.Where(x => x.CharacterId == id));
        _db.CharacterSpellEntries.RemoveRange(_db.CharacterSpellEntries.Where(x => x.CharacterId == id));
        _db.CharacterInventoryItems.RemoveRange(_db.CharacterInventoryItems.Where(x => x.CharacterId == id));
        _db.CharacterAbilityScores.RemoveRange(_db.CharacterAbilityScores.Where(x => x.CharacterId == id));
        _db.CharacterSkillProficiencies.RemoveRange(_db.CharacterSkillProficiencies.Where(x => x.CharacterId == id));
        _db.CharacterClassLevels.RemoveRange(_db.CharacterClassLevels.Where(x => x.CharacterId == id));
        _db.CharacterSelectedModules.RemoveRange(_db.CharacterSelectedModules.Where(x => x.CharacterId == id));
        _db.CharacterSheets.RemoveRange(_db.CharacterSheets.Where(x => x.CharacterId == id));
        _db.CharacterRecords.Remove(record);

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<CharacterSummary?> DuplicateCharacterAsync(Guid characterId, DuplicateCharacterRequest request, CancellationToken cancellationToken)
    {
        var source = await _db.CharacterRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CharacterId == characterId.ToString(), cancellationToken);
        if (source is null)
        {
            return null;
        }

        var duplicateId = Guid.NewGuid();
        var suffix = string.IsNullOrWhiteSpace(request.NameSuffix) ? "Copy" : request.NameSuffix.Trim();
        var now = DateTimeOffset.UtcNow;
        var duplicate = new CharacterRecordEntity
        {
            CharacterId = duplicateId.ToString(),
            OwnerUserId = source.OwnerUserId,
            CharacterName = $"{source.CharacterName} ({suffix})",
            BaseRuleSystem = source.BaseRuleSystem,
            MixedModeEnabled = source.MixedModeEnabled,
            OverlaySourcesJson = source.OverlaySourcesJson,
            IsArchived = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _db.CharacterRecords.Add(duplicate);
        AppendHistory(duplicateId, "character-duplicated", source.OwnerUserId, $"Duplicated from '{source.CharacterId}'.");
        await _db.SaveChangesAsync(cancellationToken);
        return ToSummary(duplicate);
    }

    public async Task<IReadOnlyList<CharacterRevisionEntry>> GetCharacterHistoryAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var entries = await _db.CharacterHistoryEntries
            .AsNoTracking()
            .Where(x => x.CharacterId == characterId.ToString())
            .Select(x => new CharacterRevisionEntry(
                x.TimestampUtc,
                x.Action,
                x.ActorUserId,
                x.Details))
            .ToArrayAsync(cancellationToken);
        return entries
            .OrderByDescending(x => x.TimestampUtc)
            .ToArray();
    }

    public async Task<CharacterWizardResult> SubmitStepAsync(Guid characterId, SubmitWizardStepRequest request, CancellationToken cancellationToken)
    {
        var entity = await _db.CharacterDrafts.FirstOrDefaultAsync(x => x.CharacterId == characterId.ToString(), cancellationToken);
        if (entity is null)
        {
            return new CharacterWizardResult(false, null, new[] { "Character draft not found." }, Array.Empty<string>());
        }

        var draft = ToDraft(entity);
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

        entity.StepsJson = SerializeJson(nextSteps);
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new CharacterWizardResult(
            true,
            ToDraft(entity),
            Array.Empty<string>(),
            Array.Empty<string>());
    }

    public async Task<CharacterWizardResult> FinalizeAsync(Guid characterId, FinalizeWizardRequest request, CancellationToken cancellationToken)
    {
        var entity = await _db.CharacterDrafts.FirstOrDefaultAsync(x => x.CharacterId == characterId.ToString(), cancellationToken);
        if (entity is null)
        {
            return new CharacterWizardResult(false, null, new[] { "Character draft not found." }, Array.Empty<string>());
        }

        var draft = ToDraft(entity);
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

        entity.IsFinalized = true;
        entity.WarningsJson = SerializeJson(resolveResult.Warnings);
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;

        var character = await _db.CharacterRecords.FirstOrDefaultAsync(x => x.CharacterId == entity.CharacterId, cancellationToken);
        if (character is null)
        {
            _db.CharacterRecords.Add(new CharacterRecordEntity
            {
                CharacterId = entity.CharacterId,
                OwnerUserId = entity.OwnerUserId,
                CharacterName = entity.CharacterName,
                BaseRuleSystem = entity.BaseRuleSystem,
                MixedModeEnabled = entity.MixedModeEnabled,
                OverlaySourcesJson = entity.OverlaySourcesJson,
                IsArchived = false,
                CreatedAtUtc = entity.CreatedAtUtc,
                UpdatedAtUtc = entity.UpdatedAtUtc
            });
        }

        AppendHistory(characterId, "character-finalized", entity.OwnerUserId, "Wizard draft finalized into character record.");
        await _db.SaveChangesAsync(cancellationToken);

        return new CharacterWizardResult(
            true,
            ToDraft(entity),
            Array.Empty<string>(),
            resolveResult.Warnings);
    }

    public async Task<CharacterWizardResult> CopyToRulesetAsync(Guid characterId, CopyCharacterRulesetRequest request, CancellationToken cancellationToken)
    {
        var source = await _db.CharacterDrafts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CharacterId == characterId.ToString(), cancellationToken);
        if (source is null)
        {
            return new CharacterWizardResult(false, null, new[] { "Character draft not found." }, Array.Empty<string>());
        }

        var sourceDraft = ToDraft(source);
        var warnings = new List<string>
        {
            "Base ruleset is locked after creation. This operation creates a copied character in the target ruleset."
        };
        var copiedSteps = request.CarrySelectionsForward ? sourceDraft.Steps : Array.Empty<WizardStepState>();
        if (!request.CarrySelectionsForward)
        {
            warnings.Add("Selections were not carried forward; review all wizard steps in the copied character.");
        }
        else
        {
            warnings.Add("Selections were carried forward; review compatibility conflicts in the target ruleset.");
        }

        var copiedId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var copiedEntity = new CharacterDraftEntity
        {
            CharacterId = copiedId.ToString(),
            OwnerUserId = source.OwnerUserId,
            CharacterName = $"{source.CharacterName} (Copy)",
            BaseRuleSystem = request.TargetRuleSystem.ToString(),
            MixedModeEnabled = request.TargetOverlaySources.Count > 0,
            OverlaySourcesJson = SerializeJson(request.TargetOverlaySources),
            IsFinalized = false,
            StepsJson = SerializeJson(copiedSteps),
            WarningsJson = SerializeJson(warnings),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _db.CharacterDrafts.Add(copiedEntity);
        AppendHistory(copiedId, "draft-copied-ruleset", source.OwnerUserId, $"Copied from '{source.CharacterId}' to target ruleset '{request.TargetRuleSystem}'.");
        await _db.SaveChangesAsync(cancellationToken);

        return new CharacterWizardResult(true, ToDraft(copiedEntity), Array.Empty<string>(), warnings);
    }

    private void AppendHistory(Guid characterId, string action, string actorUserId, string details)
    {
        _db.CharacterHistoryEntries.Add(new CharacterHistoryEntity
        {
            EntryId = Guid.NewGuid().ToString(),
            CharacterId = characterId.ToString(),
            TimestampUtc = DateTimeOffset.UtcNow,
            Action = action,
            ActorUserId = actorUserId,
            Details = details
        });
    }

    private static CharacterSummary ToSummary(CharacterRecordEntity entity)
    {
        return new CharacterSummary(
            Guid.Parse(entity.CharacterId),
            entity.CharacterName,
            ParseRuleSystem(entity.BaseRuleSystem),
            entity.MixedModeEnabled,
            entity.IsArchived,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);
    }

    private static CharacterWizardDraft ToDraft(CharacterDraftEntity entity)
    {
        return new CharacterWizardDraft(
            Guid.Parse(entity.CharacterId),
            entity.OwnerUserId,
            entity.CharacterName,
            new RulesProfile(
                ParseRuleSystem(entity.BaseRuleSystem),
                entity.MixedModeEnabled,
                DeserializeJson<IReadOnlyList<string>>(entity.OverlaySourcesJson) ?? Array.Empty<string>()),
            entity.IsFinalized,
            DeserializeJson<IReadOnlyList<WizardStepState>>(entity.StepsJson) ?? Array.Empty<WizardStepState>(),
            DeserializeJson<IReadOnlyList<string>>(entity.WarningsJson) ?? Array.Empty<string>());
    }

    private static RuleSystemMode ParseRuleSystem(string value)
    {
        return Enum.TryParse<RuleSystemMode>(value, ignoreCase: true, out var parsed)
            ? parsed
            : RuleSystemMode.Rules2024;
    }

    private static string SerializeJson<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    private static T? DeserializeJson<T>(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }
        return JsonSerializer.Deserialize<T>(value, JsonOptions);
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
}
