namespace DndApp.Api.Mechanics;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DndApp.Api.Data;
using Microsoft.EntityFrameworkCore;

public interface IExperienceService
{
    /// <summary>
    /// Get XP required to reach a specific level (D&D 5e standard progression)
    /// </summary>
    long GetExperienceForLevel(int level);

    /// <summary>
    /// Get XP required for the next level given current experience
    /// </summary>
    long GetExperienceForNextLevel(long currentXp);

    /// <summary>
    /// Calculate what level the character should be at given total XP
    /// </summary>
    int CalculateLevelFromExperience(long totalExperience);

    /// <summary>
    /// Award XP to a character and handle level-ups
    /// </summary>
    Task<LevelUpResult> AwardExperienceAsync(string characterId, long xpToAward, AppDbContext db);

    /// <summary>
    /// Get character's current XP state
    /// </summary>
    Task<CharacterExperienceState?> GetCharacterExperienceAsync(string characterId, AppDbContext db);

    /// <summary>
    /// Initialize experience for a new character
    /// </summary>
    Task InitializeCharacterExperienceAsync(string characterId, AppDbContext db);
}

public sealed class ExperienceService : IExperienceService
{
    // D&D 5e standard XP progression table (levels 1-20)
    private static readonly long[] ExperienceTable = new[]
    {
        0L,        // Level 1
        300L,      // Level 2
        900L,      // Level 3
        2700L,     // Level 4
        6500L,     // Level 5
        14000L,    // Level 6
        23000L,    // Level 7
        34000L,    // Level 8
        48000L,    // Level 9
        64000L,    // Level 10
        85000L,    // Level 11
        100000L,   // Level 12
        120000L,   // Level 13
        140000L,   // Level 14
        165000L,   // Level 15
        195000L,   // Level 16
        225000L,   // Level 17
        265000L,   // Level 18
        305000L,   // Level 19
        355000L,   // Level 20
    };

    // Levels where Ability Score Improvements (ASIs) are granted
    private static readonly int[] ASILevels = { 4, 8, 12, 16, 19 };

    // Levels where feat options are available (in addition to ASI)
    private static readonly int[] FeatLevels = { 1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 20 };

    public long GetExperienceForLevel(int level)
    {
        if (level < 1 || level > 20)
            throw new ArgumentException("Level must be between 1 and 20");

        return ExperienceTable[level - 1];
    }

    public long GetExperienceForNextLevel(long currentXp)
    {
        var currentLevel = CalculateLevelFromExperience(currentXp);
        if (currentLevel >= 20)
            return ExperienceTable[19]; // Max level

        var nextLevel = currentLevel + 1;
        return ExperienceTable[nextLevel - 1];
    }

    public int CalculateLevelFromExperience(long totalExperience)
    {
        for (int i = ExperienceTable.Length - 1; i >= 0; i--)
        {
            if (totalExperience >= ExperienceTable[i])
                return i + 1;
        }
        return 1;
    }

    public async Task<LevelUpResult> AwardExperienceAsync(string characterId, long xpToAward, AppDbContext db)
    {
        var existing = await db.CharacterExperience.FirstOrDefaultAsync(x => x.CharacterId == characterId);
        if (existing == null)
        {
            await InitializeCharacterExperienceAsync(characterId, db);
            existing = await db.CharacterExperience.FirstAsync(x => x.CharacterId == characterId);
        }

        var oldLevel = existing.CurrentLevel;
        var oldXp = existing.TotalExperience;
        var newXp = oldXp + xpToAward;
        var newLevel = CalculateLevelFromExperience(newXp);

        existing.TotalExperience = newXp;
        existing.CurrentLevel = newLevel;
        existing.ExperienceForNextLevel = GetExperienceForNextLevel(newXp);
        existing.UpdatedAtUtc = DateTimeOffset.UtcNow;

        var levelsGained = newLevel - oldLevel;
        var levelUps = new List<LevelUpGrant>();

        // Track all levels gained
        for (int level = oldLevel + 1; level <= newLevel; level++)
        {
            var grantASI = ASILevels.Contains(level);
            var grantFeat = FeatLevels.Contains(level) && !ASILevels.Contains(level);

            var grants = new LevelUpGrant 
            { 
                Level = level,
                GrantsAbilityScoreImprovement = grantASI,
                GrantsFeatOption = grantFeat
            };

            levelUps.Add(grants);

            if (grantASI)
                existing.AbilityScoreImprovementsUsed++;

            // Log progression
            var progressionId = $"prog-{characterId}-{level}-{DateTimeOffset.UtcNow.Ticks}";
            var progression = new CharacterLevelProgressionEntity
            {
                Id = progressionId,
                CharacterId = characterId,
                Level = level,
                ExperienceRequired = GetExperienceForLevel(level),
                LeveledUpAtUtc = DateTimeOffset.UtcNow,
                GrantedAbilityScoreImprovement = grantASI,
                GrantedFeatOption = grantFeat
            };
            db.CharacterLevelProgression.Add(progression);
        }

        if (levelsGained > 0)
        {
            existing.LastLevelUpAtUtc = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync();

        return new LevelUpResult
        {
            OldLevel = oldLevel,
            NewLevel = newLevel,
            ExperienceAwarded = xpToAward,
            TotalExperience = newXp,
            LevelsGained = levelsGained,
            LevelUps = levelUps
        };
    }

    public async Task<CharacterExperienceState?> GetCharacterExperienceAsync(string characterId, AppDbContext db)
    {
        var exp = await db.CharacterExperience.FirstOrDefaultAsync(x => x.CharacterId == characterId);
        if (exp == null)
            return null;

        var progressions = await db.CharacterLevelProgression
            .Where(x => x.CharacterId == characterId)
            .OrderBy(x => x.Level)
            .ToListAsync();

        return new CharacterExperienceState
        {
            CharacterId = characterId,
            CurrentLevel = exp.CurrentLevel,
            TotalExperience = exp.TotalExperience,
            ExperienceForNextLevel = exp.ExperienceForNextLevel,
            AbilityScoreImprovementsAvailable = ASILevels.Count(l => l <= exp.CurrentLevel) - exp.AbilityScoreImprovementsUsed,
            LevelProgression = progressions
                .Select(p => new LevelProgressionInfo
                {
                    Level = p.Level,
                    ExperienceRequired = p.ExperienceRequired,
                    LeveledUpAtUtc = p.LeveledUpAtUtc,
                    GrantedAbilityScoreImprovement = p.GrantedAbilityScoreImprovement,
                    GrantedFeatOption = p.GrantedFeatOption
                })
                .ToList()
        };
    }

    public async Task InitializeCharacterExperienceAsync(string characterId, AppDbContext db)
    {
        var existing = await db.CharacterExperience.FirstOrDefaultAsync(x => x.CharacterId == characterId);
        if (existing != null)
            return; // Already initialized

        var exp = new CharacterExperienceEntity
        {
            CharacterId = characterId,
            CurrentLevel = 1,
            TotalExperience = 0,
            ExperienceForNextLevel = 300,
            AbilityScoreImprovementsUsed = 0,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        db.CharacterExperience.Add(exp);

        // Log initial level
        var progressionId = $"prog-{characterId}-1-{DateTimeOffset.UtcNow.Ticks}";
        var progression = new CharacterLevelProgressionEntity
        {
            Id = progressionId,
            CharacterId = characterId,
            Level = 1,
            ExperienceRequired = 0,
            LeveledUpAtUtc = DateTimeOffset.UtcNow
        };
        db.CharacterLevelProgression.Add(progression);

        await db.SaveChangesAsync();
    }
}

public sealed record LevelUpResult
{
    public int OldLevel { get; init; }
    public int NewLevel { get; init; }
    public long ExperienceAwarded { get; init; }
    public long TotalExperience { get; init; }
    public int LevelsGained { get; init; }
    public List<LevelUpGrant> LevelUps { get; init; } = new();
}

public sealed record LevelUpGrant
{
    public int Level { get; init; }
    public bool GrantsAbilityScoreImprovement { get; init; }
    public bool GrantsFeatOption { get; init; }
}

public sealed record CharacterExperienceState
{
    public string CharacterId { get; init; } = string.Empty;
    public int CurrentLevel { get; init; }
    public long TotalExperience { get; init; }
    public long ExperienceForNextLevel { get; init; }
    public int AbilityScoreImprovementsAvailable { get; init; }
    public List<LevelProgressionInfo> LevelProgression { get; init; } = new();
}

public sealed record LevelProgressionInfo
{
    public int Level { get; init; }
    public long ExperienceRequired { get; init; }
    public DateTimeOffset LeveledUpAtUtc { get; init; }
    public bool GrantedAbilityScoreImprovement { get; init; }
    public bool GrantedFeatOption { get; init; }
}
