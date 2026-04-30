using System.ComponentModel.DataAnnotations;

namespace DndApp.Api.Data;

public sealed class CharacterExperienceEntity
{
    [Key]
    public string CharacterId { get; set; } = string.Empty;
    public int CurrentLevel { get; set; } = 1;
    public long TotalExperience { get; set; } = 0;
    public long ExperienceForNextLevel { get; set; } = 300;
    public int AbilityScoreImprovementsUsed { get; set; } = 0;
    public DateTimeOffset? LastLevelUpAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class CharacterLevelProgressionEntity
{
    public string Id { get; set; } = string.Empty;
    public string CharacterId { get; set; } = string.Empty;
    public int Level { get; set; }
    public long ExperienceRequired { get; set; }
    public DateTimeOffset LeveledUpAtUtc { get; set; }
    public bool GrantedAbilityScoreImprovement { get; set; }
    public bool GrantedFeatOption { get; set; }
    public string? FeatOrASIChosenJson { get; set; }
}

public sealed class CharacterLevelUpChoiceEntity
{
    public string Id { get; set; } = string.Empty;
    public string CharacterId { get; set; } = string.Empty;
    public int Level { get; set; }
    public string ChoiceType { get; set; } = string.Empty; // "AbilityScoreImprovement" or "Feat"
    public string? ChosenAbility { get; set; } // "Strength", "Dexterity", etc.
    public string? ChosenFeatId { get; set; } // Feat module ID
    public bool IsConfirmed { get; set; } = false;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ConfirmedAtUtc { get; set; }
}
