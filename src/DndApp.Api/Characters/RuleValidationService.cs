using System.Text.Json;
using DndApp.Api.Data;
using DndApp.Api.MixedRules;
using Microsoft.EntityFrameworkCore;

namespace DndApp.Api.Characters;

public interface IRuleValidationService
{
    Task<IReadOnlyList<string>> ValidateClassSelectionAsync(
        string classModuleId,
        RuleSystemMode baseRuleSystem,
        IReadOnlyDictionary<string, int> abilityScores,
        int level,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ValidateInventoryItemAsync(
        string itemDefinitionId,
        RuleSystemMode baseRuleSystem,
        bool mixedModeEnabled,
        IReadOnlyDictionary<string, int> abilityScores,
        int level,
        CancellationToken cancellationToken);
}

public sealed class RuleValidationService : IRuleValidationService
{
    private readonly AppDbContext _db;

    public RuleValidationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<string>> ValidateClassSelectionAsync(
        string classModuleId,
        RuleSystemMode baseRuleSystem,
        IReadOnlyDictionary<string, int> abilityScores,
        int level,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var module = await _db.RuleModules.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == classModuleId, cancellationToken);
        if (module is null)
        {
            errors.Add($"Class module '{classModuleId}' was not found.");
            return errors;
        }

        if (!module.ModuleType.Contains("class", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"Module '{classModuleId}' is not a class module.");
        }

        var contentSource = await _db.ContentSources.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == module.ContentSourceId, cancellationToken);
        if (contentSource is not null && !string.Equals(contentSource.RuleSystemId, ToRuleSystemId(baseRuleSystem), StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"Class module '{classModuleId}' source '{contentSource.Code}' is incompatible with {baseRuleSystem}.");
        }

        var prereqErrors = await ValidatePrerequisitesAsync(module.Id, abilityScores, level, cancellationToken);
        errors.AddRange(prereqErrors);
        return errors;
    }

    public async Task<IReadOnlyList<string>> ValidateInventoryItemAsync(
        string itemDefinitionId,
        RuleSystemMode baseRuleSystem,
        bool mixedModeEnabled,
        IReadOnlyDictionary<string, int> abilityScores,
        int level,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var item = await _db.ItemDefinitions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == itemDefinitionId, cancellationToken);
        if (item is null)
        {
            errors.Add($"Item definition '{itemDefinitionId}' was not found.");
            return errors;
        }

        var module = await _db.RuleModules.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == item.RuleModuleId, cancellationToken);
        if (module is null)
        {
            errors.Add($"Item definition '{itemDefinitionId}' references missing module '{item.RuleModuleId}'.");
            return errors;
        }

        var contentSource = await _db.ContentSources.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == module.ContentSourceId, cancellationToken);
        if (!mixedModeEnabled &&
            contentSource is not null &&
            !string.Equals(contentSource.RuleSystemId, ToRuleSystemId(baseRuleSystem), StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"Item '{itemDefinitionId}' source '{contentSource.Code}' is incompatible with {baseRuleSystem}.");
        }

        var prereqErrors = await ValidatePrerequisitesAsync(module.Id, abilityScores, level, cancellationToken);
        errors.AddRange(prereqErrors);
        return errors;
    }

    private async Task<IReadOnlyList<string>> ValidatePrerequisitesAsync(
        string moduleId,
        IReadOnlyDictionary<string, int> abilityScores,
        int level,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var prerequisites = await _db.Prerequisites.AsNoTracking()
            .Where(x => x.RuleModuleId == moduleId)
            .ToListAsync(cancellationToken);

        foreach (var prerequisite in prerequisites)
        {
            try
            {
                using var doc = JsonDocument.Parse(prerequisite.PredicateJson);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (doc.RootElement.TryGetProperty("minLevel", out var minLevelNode)
                    && minLevelNode.ValueKind == JsonValueKind.Number
                    && minLevelNode.TryGetInt32(out var minLevel)
                    && level < minLevel)
                {
                    errors.Add($"Prerequisite '{prerequisite.Id}' requires level {minLevel}.");
                }

                if (doc.RootElement.TryGetProperty("ability", out var abilityNode)
                    && doc.RootElement.TryGetProperty("minScore", out var minScoreNode)
                    && abilityNode.ValueKind == JsonValueKind.String
                    && minScoreNode.ValueKind == JsonValueKind.Number
                    && minScoreNode.TryGetInt32(out var minScore))
                {
                    var abilityName = abilityNode.GetString() ?? string.Empty;
                    var score = abilityScores.TryGetValue(abilityName, out var value) ? value : 0;
                    if (score < minScore)
                    {
                        errors.Add($"Prerequisite '{prerequisite.Id}' requires {abilityName} {minScore}+.");
                    }
                }

                if (doc.RootElement.TryGetProperty("abilities", out var abilitiesNode)
                    && abilitiesNode.ValueKind == JsonValueKind.Object)
                {
                    foreach (var ability in abilitiesNode.EnumerateObject())
                    {
                        if (ability.Value.ValueKind != JsonValueKind.Number || !ability.Value.TryGetInt32(out var requiredScore))
                        {
                            continue;
                        }
                        var score = abilityScores.TryGetValue(ability.Name, out var value) ? value : 0;
                        if (score < requiredScore)
                        {
                            errors.Add($"Prerequisite '{prerequisite.Id}' requires {ability.Name} {requiredScore}+.");
                        }
                    }
                }
            }
            catch (JsonException)
            {
                errors.Add($"Prerequisite '{prerequisite.Id}' contains invalid predicate JSON.");
            }
        }

        return errors;
    }

    private static string ToRuleSystemId(RuleSystemMode mode)
    {
        return mode == RuleSystemMode.Rules2014 ? "rules-2014" : "rules-2024";
    }
}
