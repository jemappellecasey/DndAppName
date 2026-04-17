using DndApp.Api.Data;
using DndApp.Api.MixedRules;
using Microsoft.EntityFrameworkCore;

namespace DndApp.Api.Characters;

public interface ICharacterBuildService
{
    Task<CharacterBuildData?> GetBuildAsync(Guid characterId, CancellationToken cancellationToken);
    Task<(CharacterBuildData? Data, IReadOnlyList<string> Errors)> UpsertBuildAsync(Guid characterId, UpsertCharacterBuildRequest request, CancellationToken cancellationToken);
    Task<(CharacterBuildData? Data, IReadOnlyList<string> Errors)> PatchBuildAsync(Guid characterId, PatchCharacterBuildRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteBuildAsync(Guid characterId, CancellationToken cancellationToken);
}

public sealed class CharacterBuildService : ICharacterBuildService
{
    private static readonly string[] AllowedAbilityNames =
    {
        "Strength",
        "Dexterity",
        "Constitution",
        "Intelligence",
        "Wisdom",
        "Charisma",
    };

    private readonly AppDbContext _db;

    public CharacterBuildService(AppDbContext db)
    {
        _db = db;
    }

    public Task<CharacterBuildData?> GetBuildAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return ReadBuildAsync(characterId, cancellationToken);
    }

    public async Task<(CharacterBuildData? Data, IReadOnlyList<string> Errors)> UpsertBuildAsync(
        Guid characterId,
        UpsertCharacterBuildRequest request,
        CancellationToken cancellationToken)
    {
        var errors = Validate(
            request.CharacterName,
            request.ClassModuleId,
            request.ClassName,
            request.Level,
            request.ProficiencyBonus,
            request.AbilityScores,
            request.ProficientSkills);
        if (errors.Count > 0)
        {
            return (null, errors);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var id = characterId.ToString();
        var now = DateTimeOffset.UtcNow;
        var existing = await _db.CharacterSheets.SingleOrDefaultAsync(x => x.CharacterId == id, cancellationToken);
        if (existing is null)
        {
            existing = new CharacterSheetEntity
            {
                CharacterId = id,
                CreatedAtUtc = now,
            };
            _db.CharacterSheets.Add(existing);
        }

        existing.CharacterName = request.CharacterName.Trim();
        existing.BaseRuleSystem = request.BaseRuleSystem.ToString();
        existing.BuildMethod = request.BuildMethod.ToString();
        existing.ClassModuleId = request.ClassModuleId.Trim();
        existing.ClassName = request.ClassName.Trim();
        existing.Level = request.Level;
        existing.ProficiencyBonus = request.ProficiencyBonus;
        existing.UpdatedAtUtc = now;

        await ReplaceAbilityScoresAsync(id, request.AbilityScores, cancellationToken);
        await ReplaceSkillProficienciesAsync(id, request.ProficientSkills, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return (await ReadBuildAsync(characterId, cancellationToken), Array.Empty<string>());
    }

    public async Task<(CharacterBuildData? Data, IReadOnlyList<string> Errors)> PatchBuildAsync(
        Guid characterId,
        PatchCharacterBuildRequest request,
        CancellationToken cancellationToken)
    {
        var id = characterId.ToString();
        var sheet = await _db.CharacterSheets.SingleOrDefaultAsync(x => x.CharacterId == id, cancellationToken);
        if (sheet is null)
        {
            return (null, new[] { "Character build was not found." });
        }

        var currentAbilities = await _db.CharacterAbilityScores
            .Where(x => x.CharacterId == id)
            .ToDictionaryAsync(x => x.AbilityName, x => x.Score, cancellationToken);
        var currentSkills = await _db.CharacterSkillProficiencies
            .Where(x => x.CharacterId == id)
            .Select(x => x.SkillName)
            .ToArrayAsync(cancellationToken);

        var mergedCharacterName = request.CharacterName?.Trim() ?? sheet.CharacterName;
        var mergedClassModuleId = request.ClassModuleId?.Trim() ?? sheet.ClassModuleId;
        var mergedClassName = request.ClassName?.Trim() ?? sheet.ClassName;
        var mergedLevel = request.Level ?? sheet.Level;
        var mergedProficiencyBonus = request.ProficiencyBonus ?? sheet.ProficiencyBonus;
        var mergedAbilities = request.AbilityScores ?? currentAbilities;
        var mergedSkills = request.ProficientSkills ?? currentSkills;

        var errors = Validate(
            mergedCharacterName,
            mergedClassModuleId,
            mergedClassName,
            mergedLevel,
            mergedProficiencyBonus,
            mergedAbilities,
            mergedSkills);
        if (errors.Count > 0)
        {
            return (null, errors);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        sheet.CharacterName = mergedCharacterName;
        sheet.BaseRuleSystem = (request.BaseRuleSystem ?? Enum.Parse<RuleSystemMode>(sheet.BaseRuleSystem, ignoreCase: true)).ToString();
        sheet.BuildMethod = (request.BuildMethod ?? Enum.Parse<CharacterBuildMethod>(sheet.BuildMethod, ignoreCase: true)).ToString();
        sheet.ClassModuleId = mergedClassModuleId;
        sheet.ClassName = mergedClassName;
        sheet.Level = mergedLevel;
        sheet.ProficiencyBonus = mergedProficiencyBonus;
        sheet.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await ReplaceAbilityScoresAsync(id, mergedAbilities, cancellationToken);
        await ReplaceSkillProficienciesAsync(id, mergedSkills, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return (await ReadBuildAsync(characterId, cancellationToken), Array.Empty<string>());
    }

    public async Task<bool> DeleteBuildAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var id = characterId.ToString();
        var sheet = await _db.CharacterSheets.SingleOrDefaultAsync(x => x.CharacterId == id, cancellationToken);
        if (sheet is null)
        {
            return false;
        }

        _db.CharacterSheets.Remove(sheet);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<CharacterBuildData?> ReadBuildAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var id = characterId.ToString();
        var sheet = await _db.CharacterSheets.AsNoTracking().SingleOrDefaultAsync(x => x.CharacterId == id, cancellationToken);
        if (sheet is null)
        {
            return null;
        }

        var abilityScores = await _db.CharacterAbilityScores.AsNoTracking()
            .Where(x => x.CharacterId == id)
            .OrderBy(x => x.AbilityName)
            .ToDictionaryAsync(x => x.AbilityName, x => x.Score, cancellationToken);
        var proficientSkills = await _db.CharacterSkillProficiencies.AsNoTracking()
            .Where(x => x.CharacterId == id)
            .Select(x => x.SkillName)
            .OrderBy(x => x)
            .ToArrayAsync(cancellationToken);

        return new CharacterBuildData(
            characterId,
            sheet.CharacterName,
            Enum.Parse<RuleSystemMode>(sheet.BaseRuleSystem, ignoreCase: true),
            Enum.Parse<CharacterBuildMethod>(sheet.BuildMethod, ignoreCase: true),
            sheet.ClassModuleId,
            sheet.ClassName,
            sheet.Level,
            sheet.ProficiencyBonus,
            abilityScores,
            proficientSkills,
            sheet.CreatedAtUtc,
            sheet.UpdatedAtUtc);
    }

    private async Task ReplaceAbilityScoresAsync(string characterId, IReadOnlyDictionary<string, int> abilityScores, CancellationToken cancellationToken)
    {
        var current = await _db.CharacterAbilityScores.Where(x => x.CharacterId == characterId).ToListAsync(cancellationToken);
        _db.CharacterAbilityScores.RemoveRange(current);
        foreach (var pair in abilityScores)
        {
            _db.CharacterAbilityScores.Add(new CharacterAbilityScoreEntity
            {
                CharacterId = characterId,
                AbilityName = pair.Key.Trim(),
                Score = pair.Value,
            });
        }
    }

    private async Task ReplaceSkillProficienciesAsync(string characterId, IReadOnlyList<string> skills, CancellationToken cancellationToken)
    {
        var current = await _db.CharacterSkillProficiencies.Where(x => x.CharacterId == characterId).ToListAsync(cancellationToken);
        _db.CharacterSkillProficiencies.RemoveRange(current);
        foreach (var skill in skills.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            _db.CharacterSkillProficiencies.Add(new CharacterSkillProficiencyEntity
            {
                CharacterId = characterId,
                SkillName = skill.Trim(),
            });
        }
    }

    private static List<string> Validate(
        string characterName,
        string classModuleId,
        string className,
        int level,
        int proficiencyBonus,
        IReadOnlyDictionary<string, int> abilityScores,
        IReadOnlyList<string> proficientSkills)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(characterName))
        {
            errors.Add("Character name is required.");
        }
        if (string.IsNullOrWhiteSpace(classModuleId))
        {
            errors.Add("Class module id is required.");
        }
        if (string.IsNullOrWhiteSpace(className))
        {
            errors.Add("Class name is required.");
        }
        if (level < 1 || level > 20)
        {
            errors.Add("Character level must be between 1 and 20.");
        }
        if (proficiencyBonus < 1 || proficiencyBonus > 8)
        {
            errors.Add("Proficiency bonus must be between 1 and 8.");
        }

        var allowed = new HashSet<string>(AllowedAbilityNames, StringComparer.OrdinalIgnoreCase);
        foreach (var ability in AllowedAbilityNames)
        {
            if (!abilityScores.ContainsKey(ability))
            {
                errors.Add($"Missing ability score for '{ability}'.");
            }
        }

        foreach (var pair in abilityScores)
        {
            if (!allowed.Contains(pair.Key))
            {
                errors.Add($"Unknown ability name '{pair.Key}'.");
                continue;
            }
            if (pair.Value < 1 || pair.Value > 30)
            {
                errors.Add($"Ability '{pair.Key}' must be between 1 and 30.");
            }
        }

        foreach (var skill in proficientSkills)
        {
            if (string.IsNullOrWhiteSpace(skill))
            {
                errors.Add("Skill proficiency values cannot be empty.");
            }
        }

        return errors;
    }
}
