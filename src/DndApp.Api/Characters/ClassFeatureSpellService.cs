using DndApp.Api.Data;
using DndApp.Api.MixedRules;
using Microsoft.EntityFrameworkCore;

namespace DndApp.Api.Characters;

public interface IClassFeatureSpellService
{
    Task<IReadOnlyList<AutomaticSpellGrant>> GetAutomaticSpellsAsync(
        string characterId,
        string classModuleId,
        int classLevel,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<FeatSpellChoice>> GetFeatSpellGrantsAsync(
        string characterId,
        CancellationToken cancellationToken);

    Task<SpellVariantComparison?> GetSpellVariantsAsync(
        string spellSlug,
        CancellationToken cancellationToken);
}

public sealed class ClassFeatureSpellService : IClassFeatureSpellService
{
    private readonly AppDbContext _db;

    public ClassFeatureSpellService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AutomaticSpellGrant>> GetAutomaticSpellsAsync(
        string characterId,
        string classModuleId,
        int classLevel,
        CancellationToken cancellationToken)
    {
        var id = characterId.ToString();
        var sheet = await _db.CharacterSheets.AsNoTracking()
            .Where(x => x.CharacterId == id)
            .Select(x => new { x.BaseRuleSystem })
            .FirstOrDefaultAsync(cancellationToken);

        if (sheet is null)
        {
            return Array.Empty<AutomaticSpellGrant>();
        }

        var grants = new List<AutomaticSpellGrant>();

        // Get class spell grants
        var classGrants = await _db.ClassSpellGrants.AsNoTracking()
            .Where(x => x.ClassId == classModuleId && x.Edition == sheet.BaseRuleSystem && x.MinLevel <= classLevel)
            .Join(_db.Spells.AsNoTracking(), g => g.SpellId, s => s.Id, (g, s) => new { Grant = g, Spell = s })
            .ToListAsync(cancellationToken);

        foreach (var item in classGrants)
        {
            var variants = await GetSpellVariantsAsync(item.Spell.Slug, cancellationToken);
            grants.Add(new AutomaticSpellGrant(
                item.Spell.Id,
                item.Spell.Name,
                item.Grant.SourceDescription,
                "Class",
                item.Grant.MinLevel,
                variants?.BothEditionsAvailable ?? false,
                variants
            ));
        }

        // Get subclass spell grants (if character has this class at the specified level)
        var characterClass = await _db.CharacterClassLevels.AsNoTracking()
            .Where(x => x.CharacterId == id && x.ClassModuleId == classModuleId)
            .Select(x => new { x.ClassModuleId })
            .FirstOrDefaultAsync(cancellationToken);

        if (characterClass is not null)
        {
            var subclasses = await _db.CharacterSelectedModules.AsNoTracking()
                .Where(x => x.CharacterId == id && x.Slot == "subclass")
                .Select(x => x.ModuleId)
                .ToListAsync(cancellationToken);

            foreach (var subclassId in subclasses)
            {
                var subclassGrants = await _db.SubclassSpellGrants.AsNoTracking()
                    .Where(x => x.SubclassId == subclassId && x.Edition == sheet.BaseRuleSystem && x.MinLevel <= classLevel)
                    .Join(_db.Spells.AsNoTracking(), g => g.SpellId, s => s.Id, (g, s) => new { Grant = g, Spell = s })
                    .ToListAsync(cancellationToken);

                foreach (var item in subclassGrants)
                {
                    var variants = await GetSpellVariantsAsync(item.Spell.Slug, cancellationToken);
                    grants.Add(new AutomaticSpellGrant(
                        item.Spell.Id,
                        item.Spell.Name,
                        item.Grant.SourceDescription,
                        "Subclass",
                        item.Grant.MinLevel,
                        variants?.BothEditionsAvailable ?? false,
                        variants
                    ));
                }
            }
        }

        // Get race spell grants
        var raceId = await _db.CharacterSelectedModules.AsNoTracking()
            .Where(x => x.CharacterId == id && x.Slot == "race")
            .Select(x => x.ModuleId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrEmpty(raceId))
        {
            var raceGrants = await _db.RaceSpellGrants.AsNoTracking()
                .Where(x => x.RaceOrSpeciesId == raceId && x.Edition == sheet.BaseRuleSystem)
                .Join(_db.Spells.AsNoTracking(), g => g.SpellId, s => s.Id, (g, s) => new { Grant = g, Spell = s })
                .ToListAsync(cancellationToken);

            foreach (var item in raceGrants)
            {
                var variants = await GetSpellVariantsAsync(item.Spell.Slug, cancellationToken);
                grants.Add(new AutomaticSpellGrant(
                    item.Spell.Id,
                    item.Spell.Name,
                    item.Grant.SourceDescription,
                    "Race",
                    0,
                    variants?.BothEditionsAvailable ?? false,
                    variants
                ));
            }
        }

        // Get background spell grants
        var backgroundId = await _db.CharacterSelectedModules.AsNoTracking()
            .Where(x => x.CharacterId == id && x.Slot == "background")
            .Select(x => x.ModuleId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrEmpty(backgroundId))
        {
            var backgroundGrants = await _db.BackgroundSpellGrants.AsNoTracking()
                .Where(x => x.BackgroundId == backgroundId && x.Edition == sheet.BaseRuleSystem)
                .Join(_db.Spells.AsNoTracking(), g => g.SpellId, s => s.Id, (g, s) => new { Grant = g, Spell = s })
                .ToListAsync(cancellationToken);

            foreach (var item in backgroundGrants)
            {
                var variants = await GetSpellVariantsAsync(item.Spell.Slug, cancellationToken);
                grants.Add(new AutomaticSpellGrant(
                    item.Spell.Id,
                    item.Spell.Name,
                    item.Grant.SourceDescription,
                    "Background",
                    0,
                    variants?.BothEditionsAvailable ?? false,
                    variants
                ));
            }
        }

        return grants;
    }

    public async Task<IReadOnlyList<FeatSpellChoice>> GetFeatSpellGrantsAsync(
        string characterId,
        CancellationToken cancellationToken)
    {
        var id = characterId.ToString();
        var sheet = await _db.CharacterSheets.AsNoTracking()
            .Where(x => x.CharacterId == id)
            .Select(x => new { x.BaseRuleSystem })
            .FirstOrDefaultAsync(cancellationToken);

        if (sheet is null)
        {
            return Array.Empty<FeatSpellChoice>();
        }

        // Get feats selected by character
        var selectedFeats = await _db.CharacterLevelUpChoices.AsNoTracking()
            .Where(x => x.CharacterId == id && x.ChoiceType == "Feat" && !string.IsNullOrEmpty(x.ChosenFeatId))
            .Select(x => x.ChosenFeatId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var choices = new List<FeatSpellChoice>();

        foreach (var featId in selectedFeats)
        {
            var grants = await _db.FeatSpellGrants.AsNoTracking()
                .Where(x => x.FeatId == featId && x.Edition == sheet.BaseRuleSystem)
                .ToListAsync(cancellationToken);

            foreach (var grant in grants)
            {
                var spellIds = CatalogParsing.ParseStringArray(grant.SpellIdsJson, string.Empty);

                var feat2014 = await _db.Feats2014.AsNoTracking()
                    .Where(x => x.Id == featId)
                    .Select(x => x.Name)
                    .FirstOrDefaultAsync(cancellationToken);

                var feat2024 = await _db.Feats2024.AsNoTracking()
                    .Where(x => x.Id == featId)
                    .Select(x => x.Name)
                    .FirstOrDefaultAsync(cancellationToken);

                var featName = feat2014 ?? feat2024 ?? featId ?? string.Empty;

                choices.Add(new FeatSpellChoice(
                    featId ?? string.Empty,
                    featName,
                    grant.GrantType,
                    grant.SelectionCount,
                    spellIds
                ));
            }
        }

        return choices;
    }

    public async Task<SpellVariantComparison?> GetSpellVariantsAsync(
        string spellSlug,
        CancellationToken cancellationToken)
    {
        var spells = await _db.Spells.AsNoTracking()
            .Where(x => x.Slug == spellSlug)
            .OrderBy(x => x.EditionYear)
            .ToListAsync(cancellationToken);

        if (spells.Count == 0)
        {
            return null;
        }

        var spell2014 = spells.FirstOrDefault(x => x.EditionYear == 2014);
        var spell2024 = spells.FirstOrDefault(x => x.EditionYear == 2024);

        var variant2014 = spell2014 is not null ? ConvertToVariant(spell2014) : null;
        var variant2024 = spell2024 is not null ? ConvertToVariant(spell2024) : null;

        return new SpellVariantComparison(
            spellSlug,
            spells.First().Name,
            variant2014,
            variant2024,
            spell2014 is not null && spell2024 is not null
        );
    }

    private static SpellVariantData ConvertToVariant(SpellEntity spell)
    {
        return new SpellVariantData(
            spell.Id,
            spell.Name,
            spell.Level,
            spell.School,
            spell.CastingTime,
            spell.RangeText,
            spell.Duration,
            spell.Ritual,
            spell.Concentration,
            spell.Description
        );
    }
}
