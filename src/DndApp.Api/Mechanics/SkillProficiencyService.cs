namespace DndApp.Api.Mechanics;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DndApp.Api.Data;
using Microsoft.EntityFrameworkCore;

public interface ISkillProficiencyService
{
    /// <summary>
    /// Get all skill proficiency sources for a character class
    /// </summary>
    Task<IReadOnlyList<SkillProficiencyEntry>> GetClassSkillProficienciesAsync(string className, string edition);

    /// <summary>
    /// Get all skills available from a specific source (e.g., Rogue class)
    /// </summary>
    Task<IReadOnlyList<string>> GetSourceSkillsAsync(string sourceType, string sourceId, string edition);
}

public sealed class SkillProficiencyService : ISkillProficiencyService
{
    private readonly AppDbContext _db;

    public SkillProficiencyService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get all class skill proficiencies for a specific class
    /// </summary>
    public async Task<IReadOnlyList<SkillProficiencyEntry>> GetClassSkillProficienciesAsync(string className, string edition)
    {
        var proficiencies = await _db.SkillProficiencySources
            .Where(s => s.SourceType == "class" && s.SourceId == className.ToLower() && s.Edition == edition)
            .Select(s => new SkillProficiencyEntry
            {
                SkillName = s.SkillName,
                SourceType = s.SourceType,
                SourceId = s.SourceId,
                SourceName = s.SourceName,
                IsExpertise = s.IsExpertise,
                IsChoice = s.IsChoice
            })
            .ToListAsync();

        return proficiencies;
    }

    public async Task<IReadOnlyList<string>> GetSourceSkillsAsync(string sourceType, string sourceId, string edition)
    {
        var skills = await _db.SkillProficiencySources
            .Where(s => s.SourceType == sourceType && s.SourceId == sourceId && s.Edition == edition)
            .Select(s => s.SkillName)
            .Distinct()
            .ToListAsync();

        return skills;
    }
}

public sealed record SkillProficiencyEntry
{
    public string SkillName { get; init; } = string.Empty;
    public string SourceType { get; init; } = string.Empty;
    public string SourceId { get; init; } = string.Empty;
    public string SourceName { get; init; } = string.Empty;
    public bool IsExpertise { get; init; } = false;
    public bool IsChoice { get; init; } = false;
}
