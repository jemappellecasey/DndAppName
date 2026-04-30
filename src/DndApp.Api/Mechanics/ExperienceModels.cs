namespace DndApp.Api.Mechanics;

public sealed record AwardExperienceRequest(long ExperienceAmount);

public sealed record CharacterExperienceResponse(
    string CharacterId,
    int CurrentLevel,
    long TotalExperience,
    long ExperienceForNextLevel,
    long ExperienceTowardNextLevel,
    int AbilityScoreImprovementsUsed,
    int AvailableAbilityScoreImprovements,
    DateTimeOffset? LastLevelUpAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record LevelUpNotificationResponse(
    string CharacterId,
    int OldLevel,
    int NewLevel,
    int LevelsGained,
    List<LevelUpGrantResponse> LevelUps);

public sealed record LevelUpGrantResponse(
    int Level,
    long ExperienceRequired,
    bool GrantsAbilityScoreImprovement,
    bool GrantsFeatOption,
    DateTimeOffset LeveledUpAtUtc);
