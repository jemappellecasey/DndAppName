using DndApp.Api.Data;
using DndApp.Api.MixedRules;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DndApp.Api.Characters;

public interface ICharacterBuildService
{
    Task<CharacterBuildData?> GetBuildAsync(Guid characterId, CancellationToken cancellationToken);
    Task<(CharacterBuildData? Data, IReadOnlyList<string> Errors)> UpsertBuildAsync(Guid characterId, string ownerUserId, UpsertCharacterBuildRequest request, CancellationToken cancellationToken);
    Task<(CharacterBuildData? Data, IReadOnlyList<string> Errors)> PatchBuildAsync(Guid characterId, PatchCharacterBuildRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteBuildAsync(Guid characterId, CancellationToken cancellationToken);
}

public sealed class CharacterBuildService : ICharacterBuildService
{
    private static readonly string[] AllowedAbilityNames =
    {
        "Strength", "Dexterity", "Constitution", "Intelligence", "Wisdom", "Charisma",
    };

    private static readonly HashSet<string> AllowedTrainingLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "None", "Proficient", "Expertise"
    };

    private static readonly Dictionary<string, string[]> ClassSaveProficiencies = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Artificer"] = ["Constitution", "Intelligence"],
        ["Barbarian"] = ["Strength", "Constitution"],
        ["Bard"] = ["Dexterity", "Charisma"],
        ["Cleric"] = ["Wisdom", "Charisma"],
        ["Druid"] = ["Intelligence", "Wisdom"],
        ["Fighter"] = ["Strength", "Constitution"],
        ["Monk"] = ["Strength", "Dexterity"],
        ["Paladin"] = ["Wisdom", "Charisma"],
        ["Ranger"] = ["Strength", "Dexterity"],
        ["Rogue"] = ["Dexterity", "Intelligence"],
        ["Sorcerer"] = ["Constitution", "Charisma"],
        ["Warlock"] = ["Wisdom", "Charisma"],
        ["Wizard"] = ["Intelligence", "Wisdom"],
    };

    private readonly AppDbContext _db;
    private readonly IRuleValidationService _validation;

    public CharacterBuildService(AppDbContext db, IRuleValidationService validation)
    {
        _db = db;
        _validation = validation;
    }

    public Task<CharacterBuildData?> GetBuildAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return ReadBuildAsync(characterId, cancellationToken);
    }

    public async Task<(CharacterBuildData? Data, IReadOnlyList<string> Errors)> UpsertBuildAsync(
        Guid characterId,
        string ownerUserId,
        UpsertCharacterBuildRequest request,
        CancellationToken cancellationToken)
    {
        var classLevels = NormalizeClassLevels(request.ClassLevels, request.ClassModuleId, request.ClassName, request.Level);
        var skillTraining = NormalizeSkillTraining(request.SkillTrainingBySkill, request.ProficientSkills);
        var selectedModules = NormalizeSelectedModules(request.SelectedModules);

        var errors = Validate(
            request.CharacterName,
            request.ClassModuleId,
            request.ClassName,
            request.Level,
            request.ProficiencyBonus,
            request.AbilityScores,
            skillTraining,
            classLevels,
            selectedModules);
        if (errors.Count > 0)
        {
            return (null, errors);
        }

        foreach (var classLevel in classLevels)
        {
            var classErrors = await _validation.ValidateClassSelectionAsync(
                classLevel.ClassModuleId,
                request.BaseRuleSystem,
                request.AbilityScores,
                classLevel.Level,
                cancellationToken);
            if (classErrors.Count > 0)
            {
                return (null, classErrors);
            }
        }

        var proficiencyErrors = await ValidateProficiencySelectionsAsync(classLevels, selectedModules, skillTraining, cancellationToken);
        if (proficiencyErrors.Count > 0)
        {
            return (null, proficiencyErrors);
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
                OwnerUserId = ownerUserId,
                CreatedAtUtc = now,
            };
            _db.CharacterSheets.Add(existing);
        }

        var primaryClass = classLevels.OrderBy(x => x.SortOrder).First();
        existing.CharacterName = request.CharacterName.Trim();
        existing.BaseRuleSystem = request.BaseRuleSystem.ToString();
        existing.BuildMethod = request.BuildMethod.ToString();
        existing.ClassModuleId = primaryClass.ClassModuleId.Trim();
        existing.ClassName = primaryClass.ClassName.Trim();
        existing.Level = request.Level;
        existing.ProficiencyBonus = request.ProficiencyBonus;
        if (string.IsNullOrWhiteSpace(existing.OwnerUserId))
        {
            existing.OwnerUserId = ownerUserId;
        }
        existing.UpdatedAtUtc = now;

        await ReplaceAbilityScoresAsync(id, request.AbilityScores, cancellationToken);
        await ReplaceSkillTrainingAsync(id, skillTraining, cancellationToken);
        await ReplaceClassLevelsAsync(id, classLevels, cancellationToken);
        await ReplaceSelectedModulesAsync(id, selectedModules, cancellationToken);
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

        var currentSkillTraining = await _db.CharacterSkillProficiencies
            .Where(x => x.CharacterId == id)
            .ToDictionaryAsync(x => x.SkillName, x => x.TrainingLevel, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var currentClassLevels = await _db.CharacterClassLevels
            .Where(x => x.CharacterId == id)
            .OrderBy(x => x.SortOrder)
            .Select(x => new CharacterClassLevelData(x.ClassModuleId, x.ClassName, x.Level, x.SortOrder))
            .ToArrayAsync(cancellationToken);

        var currentSelectedModules = await _db.CharacterSelectedModules
            .Where(x => x.CharacterId == id)
            .Select(x => new CharacterSelectedModuleData(x.Slot, x.ModuleId, x.DisplayName, x.SourceCode))
            .ToArrayAsync(cancellationToken);

        if (currentClassLevels.Length == 0)
        {
            currentClassLevels = [new CharacterClassLevelData(sheet.ClassModuleId, sheet.ClassName, sheet.Level, 0)];
        }
        if (currentSkillTraining.Count == 0)
        {
            var currentSkills = await _db.CharacterSkillProficiencies
                .Where(x => x.CharacterId == id)
                .Select(x => x.SkillName)
                .ToArrayAsync(cancellationToken);
            currentSkillTraining = currentSkills.ToDictionary(x => x, _ => "Proficient", StringComparer.OrdinalIgnoreCase);
        }

        var mergedCharacterName = request.CharacterName?.Trim() ?? sheet.CharacterName;
        var mergedBaseRuleSystem = request.BaseRuleSystem ?? Enum.Parse<RuleSystemMode>(sheet.BaseRuleSystem, ignoreCase: true);
        var mergedBuildMethod = request.BuildMethod ?? Enum.Parse<CharacterBuildMethod>(sheet.BuildMethod, ignoreCase: true);
        var mergedClassModuleId = request.ClassModuleId?.Trim() ?? sheet.ClassModuleId;
        var mergedClassName = request.ClassName?.Trim() ?? sheet.ClassName;
        var mergedLevel = request.Level ?? sheet.Level;
        var mergedProficiencyBonus = request.ProficiencyBonus ?? sheet.ProficiencyBonus;
        var mergedAbilities = request.AbilityScores ?? currentAbilities;
        var mergedSkillTraining = request.SkillTrainingBySkill is not null
            ? NormalizeSkillTraining(request.SkillTrainingBySkill, request.ProficientSkills)
            : (request.ProficientSkills is not null
                ? NormalizeSkillTraining(null, request.ProficientSkills)
                : currentSkillTraining);

        var mergedClassLevels = request.ClassLevels is not null && request.ClassLevels.Count > 0
            ? NormalizeClassLevels(request.ClassLevels, mergedClassModuleId, mergedClassName, mergedLevel)
            : currentClassLevels;
        if ((request.ClassLevels is null || request.ClassLevels.Count == 0)
            && request.Level is not null
            && mergedClassLevels.Count == 1)
        {
            mergedClassLevels = [mergedClassLevels[0] with { Level = mergedLevel }];
        }

        var mergedSelectedModules = request.SelectedModules is not null
            ? NormalizeSelectedModules(request.SelectedModules)
            : currentSelectedModules;

        var errors = Validate(
            mergedCharacterName,
            mergedClassModuleId,
            mergedClassName,
            mergedLevel,
            mergedProficiencyBonus,
            mergedAbilities,
            mergedSkillTraining,
            mergedClassLevels,
            mergedSelectedModules);
        if (errors.Count > 0)
        {
            return (null, errors);
        }

        foreach (var classLevel in mergedClassLevels)
        {
            var classErrors = await _validation.ValidateClassSelectionAsync(
                classLevel.ClassModuleId,
                mergedBaseRuleSystem,
                mergedAbilities,
                classLevel.Level,
                cancellationToken);
            if (classErrors.Count > 0)
            {
                return (null, classErrors);
            }
        }

        var proficiencyErrors = await ValidateProficiencySelectionsAsync(mergedClassLevels, mergedSelectedModules, mergedSkillTraining, cancellationToken);
        if (proficiencyErrors.Count > 0)
        {
            return (null, proficiencyErrors);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var primaryClass = mergedClassLevels.OrderBy(x => x.SortOrder).First();
        sheet.CharacterName = mergedCharacterName;
        sheet.BaseRuleSystem = mergedBaseRuleSystem.ToString();
        sheet.BuildMethod = mergedBuildMethod.ToString();
        sheet.ClassModuleId = primaryClass.ClassModuleId;
        sheet.ClassName = primaryClass.ClassName;
        sheet.Level = mergedLevel;
        sheet.ProficiencyBonus = mergedProficiencyBonus;
        sheet.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await ReplaceAbilityScoresAsync(id, mergedAbilities, cancellationToken);
        await ReplaceSkillTrainingAsync(id, mergedSkillTraining, cancellationToken);
        await ReplaceClassLevelsAsync(id, mergedClassLevels, cancellationToken);
        await ReplaceSelectedModulesAsync(id, mergedSelectedModules, cancellationToken);
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

        var skillTrainingRows = await _db.CharacterSkillProficiencies.AsNoTracking()
            .Where(x => x.CharacterId == id)
            .OrderBy(x => x.SkillName)
            .ToArrayAsync(cancellationToken);
        var skillTraining = skillTrainingRows
            .ToDictionary(x => x.SkillName, x => NormalizeTrainingLevel(x.TrainingLevel), StringComparer.OrdinalIgnoreCase);
        var proficientSkills = skillTraining
            .Where(x => !string.Equals(x.Value, "None", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Key)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var classLevels = await _db.CharacterClassLevels.AsNoTracking()
            .Where(x => x.CharacterId == id)
            .OrderBy(x => x.SortOrder)
            .Select(x => new CharacterClassLevelData(x.ClassModuleId, x.ClassName, x.Level, x.SortOrder))
            .ToArrayAsync(cancellationToken);
        if (classLevels.Length == 0)
        {
            classLevels = [new CharacterClassLevelData(sheet.ClassModuleId, sheet.ClassName, sheet.Level, 0)];
        }

        var selectedModules = await _db.CharacterSelectedModules.AsNoTracking()
            .Where(x => x.CharacterId == id)
            .OrderBy(x => x.Slot)
            .Select(x => new CharacterSelectedModuleData(x.Slot, x.ModuleId, x.DisplayName, x.SourceCode))
            .ToArrayAsync(cancellationToken);

        var saveProficiencies = DeriveSaveProficiencies(classLevels);

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
            skillTraining,
            proficientSkills,
            saveProficiencies,
            classLevels,
            selectedModules,
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

    private async Task ReplaceSkillTrainingAsync(string characterId, IReadOnlyDictionary<string, string> skills, CancellationToken cancellationToken)
    {
        var current = await _db.CharacterSkillProficiencies.Where(x => x.CharacterId == characterId).ToListAsync(cancellationToken);
        _db.CharacterSkillProficiencies.RemoveRange(current);
        foreach (var pair in skills.DistinctBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            var training = NormalizeTrainingLevel(pair.Value);
            if (string.Equals(training, "None", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            _db.CharacterSkillProficiencies.Add(new CharacterSkillProficiencyEntity
            {
                CharacterId = characterId,
                SkillName = pair.Key.Trim(),
                TrainingLevel = training,
            });
        }
    }

    private async Task ReplaceClassLevelsAsync(string characterId, IReadOnlyList<CharacterClassLevelData> classLevels, CancellationToken cancellationToken)
    {
        var current = await _db.CharacterClassLevels.Where(x => x.CharacterId == characterId).ToListAsync(cancellationToken);
        _db.CharacterClassLevels.RemoveRange(current);
        foreach (var classLevel in classLevels.OrderBy(x => x.SortOrder))
        {
            _db.CharacterClassLevels.Add(new CharacterClassLevelEntity
            {
                CharacterId = characterId,
                ClassModuleId = classLevel.ClassModuleId.Trim(),
                ClassName = classLevel.ClassName.Trim(),
                Level = classLevel.Level,
                SortOrder = classLevel.SortOrder,
            });
        }
    }

    private async Task ReplaceSelectedModulesAsync(string characterId, IReadOnlyList<CharacterSelectedModuleData> selectedModules, CancellationToken cancellationToken)
    {
        var current = await _db.CharacterSelectedModules.Where(x => x.CharacterId == characterId).ToListAsync(cancellationToken);
        _db.CharacterSelectedModules.RemoveRange(current);
        foreach (var module in selectedModules)
        {
            _db.CharacterSelectedModules.Add(new CharacterSelectedModuleEntity
            {
                CharacterId = characterId,
                Slot = module.Slot.Trim(),
                ModuleId = module.ModuleId.Trim(),
                DisplayName = module.DisplayName.Trim(),
                SourceCode = module.SourceCode.Trim(),
            });
        }
    }

    private static IReadOnlyList<CharacterClassLevelData> NormalizeClassLevels(
        IReadOnlyList<CharacterClassLevelData>? provided,
        string fallbackClassModuleId,
        string fallbackClassName,
        int fallbackLevel)
    {
        if (provided is null || provided.Count == 0)
        {
            return [new CharacterClassLevelData(fallbackClassModuleId, fallbackClassName, fallbackLevel, 0)];
        }

        return provided
            .OrderBy(x => x.SortOrder)
            .Select((x, i) => new CharacterClassLevelData(x.ClassModuleId, x.ClassName, x.Level, i))
            .ToArray();
    }

    private static IReadOnlyList<CharacterSelectedModuleData> NormalizeSelectedModules(IReadOnlyList<CharacterSelectedModuleData>? provided)
    {
        return provided ?? Array.Empty<CharacterSelectedModuleData>();
    }

    private static IReadOnlyDictionary<string, string> NormalizeSkillTraining(
        IReadOnlyDictionary<string, string>? trainingBySkill,
        IReadOnlyList<string>? proficientSkills)
    {
        if (trainingBySkill is not null && trainingBySkill.Count > 0)
        {
            return trainingBySkill.ToDictionary(x => x.Key, x => NormalizeTrainingLevel(x.Value), StringComparer.OrdinalIgnoreCase);
        }

        var skills = proficientSkills ?? Array.Empty<string>();
        return skills
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Trim(), _ => "Proficient", StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeTrainingLevel(string? level)
    {
        if (string.IsNullOrWhiteSpace(level))
        {
            return "Proficient";
        }

        if (level.Equals("Expertise", StringComparison.OrdinalIgnoreCase))
        {
            return "Expertise";
        }
        if (level.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            return "None";
        }
        return "Proficient";
    }

    public static IReadOnlyList<string> DeriveSaveProficiencies(IReadOnlyList<CharacterClassLevelData> classLevels)
    {
        var saves = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var classLevel in classLevels)
        {
            if (!ClassSaveProficiencies.TryGetValue(classLevel.ClassName, out var classSaves))
            {
                continue;
            }

            foreach (var save in classSaves)
            {
                saves.Add(save);
            }
        }

        return saves.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static List<string> Validate(
        string characterName,
        string classModuleId,
        string className,
        int level,
        int proficiencyBonus,
        IReadOnlyDictionary<string, int> abilityScores,
        IReadOnlyDictionary<string, string> skillTraining,
        IReadOnlyList<CharacterClassLevelData> classLevels,
        IReadOnlyList<CharacterSelectedModuleData> selectedModules)
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

        var classLevelTotal = classLevels.Sum(x => x.Level);
        if (classLevels.Count == 0)
        {
            errors.Add("At least one class level entry is required.");
        }
        if (classLevels.Any(x => string.IsNullOrWhiteSpace(x.ClassModuleId) || string.IsNullOrWhiteSpace(x.ClassName)))
        {
            errors.Add("Each class level entry requires class module id and class name.");
        }
        if (classLevels.Any(x => x.Level < 1 || x.Level > 20))
        {
            errors.Add("Each class level entry must be between 1 and 20.");
        }
        if (classLevelTotal != level)
        {
            errors.Add("Class level entries must sum to the total character level.");
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

        foreach (var pair in skillTraining)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                errors.Add("Skill names in training map cannot be empty.");
            }
            if (!AllowedTrainingLevels.Contains(pair.Value))
            {
                errors.Add($"Skill '{pair.Key}' has invalid training level '{pair.Value}'.");
            }
        }

        foreach (var selected in selectedModules)
        {
            if (string.IsNullOrWhiteSpace(selected.Slot) || string.IsNullOrWhiteSpace(selected.ModuleId))
            {
                errors.Add("Selected module entries must include slot and module id.");
            }
        }

        return errors;
    }

    private async Task<IReadOnlyList<string>> ValidateProficiencySelectionsAsync(
        IReadOnlyList<CharacterClassLevelData> classLevels,
        IReadOnlyList<CharacterSelectedModuleData> selectedModules,
        IReadOnlyDictionary<string, string> skillTraining,
        CancellationToken cancellationToken)
    {
        var moduleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var classLevel in classLevels)
        {
            moduleIds.Add(classLevel.ClassModuleId);
        }

        foreach (var selected in selectedModules)
        {
            var normalizedSlot = selected.Slot.Trim().ToLowerInvariant();
            if (normalizedSlot is "class" or "subclass" or "race" or "species" or "background" or "origin")
            {
                moduleIds.Add(selected.ModuleId);
            }
        }

        var variants = await _db.RuleVariants.AsNoTracking()
            .Where(x => moduleIds.Contains(x.RuleModuleId))
            .Select(x => new { x.RuleModuleId, x.PayloadJson })
            .ToListAsync(cancellationToken);

        var fixedSkills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var skillChoices = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var toolChoices = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var languageChoices = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var skillChoiceCount = 0;
        var expertiseChoiceCount = 0;
        var toolChoiceCount = 0;
        var languageChoiceCount = 0;

        foreach (var variant in variants)
        {
            foreach (var skill in ParseStringArray(variant.PayloadJson, "fixedSkillProficiencies"))
            {
                fixedSkills.Add(skill);
            }
            foreach (var skill in ParseStringArray(variant.PayloadJson, "skillChoices"))
            {
                skillChoices.Add(skill);
            }
            foreach (var tool in ParseStringArray(variant.PayloadJson, "toolChoices"))
            {
                toolChoices.Add(tool);
            }
            foreach (var language in ParseStringArray(variant.PayloadJson, "languageChoices"))
            {
                languageChoices.Add(language);
            }

            skillChoiceCount += ParseInt(variant.PayloadJson, "skillChoiceCount");
            expertiseChoiceCount += ParseInt(variant.PayloadJson, "expertiseChoiceCount");
            toolChoiceCount += ParseInt(variant.PayloadJson, "toolChoiceCount");
            languageChoiceCount += ParseInt(variant.PayloadJson, "languageChoiceCount");
        }

        var errors = new List<string>();

        var hasSkillRules = fixedSkills.Count > 0 || skillChoices.Count > 0 || skillChoiceCount > 0;
        if (hasSkillRules)
        {
            foreach (var fixedSkill in fixedSkills)
            {
                if (!skillTraining.TryGetValue(fixedSkill, out var level) || string.Equals(level, "None", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"Missing required fixed skill proficiency '{fixedSkill}'.");
                }
            }

            var nonAutoSkillSelections = skillTraining
                .Where(x => !string.Equals(x.Value, "None", StringComparison.OrdinalIgnoreCase) && !fixedSkills.Contains(x.Key))
                .Select(x => x.Key)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (nonAutoSkillSelections.Length > skillChoiceCount)
            {
                errors.Add($"Selected {nonAutoSkillSelections.Length} non-fixed skill proficiencies but only {skillChoiceCount} are allowed.");
            }
            foreach (var selection in nonAutoSkillSelections)
            {
                if (!skillChoices.Contains(selection))
                {
                    errors.Add($"Skill proficiency '{selection}' is not in allowed skill choices.");
                }
            }
        }

        if (expertiseChoiceCount > 0)
        {
            var expertiseSelections = skillTraining
                .Where(x => string.Equals(x.Value, "Expertise", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Key)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (expertiseSelections.Length > expertiseChoiceCount)
            {
                errors.Add($"Selected {expertiseSelections.Length} expertise skills but only {expertiseChoiceCount} are allowed.");
            }
        }

        var selectedToolPicks = selectedModules
            .Where(x => string.Equals(x.Slot, "tool-proficiency", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.ModuleId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var hasToolRules = toolChoices.Count > 0 || toolChoiceCount > 0;
        if (hasToolRules)
        {
            if (selectedToolPicks.Length > toolChoiceCount)
            {
                errors.Add($"Selected {selectedToolPicks.Length} tool proficiencies but only {toolChoiceCount} are allowed.");
            }
            foreach (var tool in selectedToolPicks)
            {
                if (!toolChoices.Contains(tool))
                {
                    errors.Add($"Tool proficiency '{tool}' is not in allowed tool choices.");
                }
            }
        }

        var selectedLanguagePicks = selectedModules
            .Where(x => string.Equals(x.Slot, "language", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.ModuleId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var hasLanguageRules = languageChoices.Count > 0 || languageChoiceCount > 0;
        if (hasLanguageRules)
        {
            if (selectedLanguagePicks.Length > languageChoiceCount)
            {
                errors.Add($"Selected {selectedLanguagePicks.Length} languages but only {languageChoiceCount} are allowed.");
            }
            foreach (var language in selectedLanguagePicks)
            {
                if (!languageChoices.Contains(language))
                {
                    errors.Add($"Language '{language}' is not in allowed language choices.");
                }
            }
        }

        return errors;
    }

    private static IReadOnlyList<string> ParseStringArray(string? payloadJson, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return Array.Empty<string>();
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            if (!document.RootElement.TryGetProperty(propertyName, out var node) || node.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<string>();
            }

            return node.EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.String)
                .Select(x => x.GetString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    private static int ParseInt(string? payloadJson, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return 0;
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            if (!document.RootElement.TryGetProperty(propertyName, out var node))
            {
                return 0;
            }

            return node.ValueKind switch
            {
                JsonValueKind.Number when node.TryGetInt32(out var n) => n,
                JsonValueKind.String when int.TryParse(node.GetString(), out var n) => n,
                _ => 0,
            };
        }
        catch (JsonException)
        {
            return 0;
        }
    }
}
