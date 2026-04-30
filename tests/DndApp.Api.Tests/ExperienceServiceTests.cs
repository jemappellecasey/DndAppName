namespace DndApp.Api.Mechanics;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

public sealed class ExperienceServiceTests
{
    [Fact]
    public void GetExperienceForLevel_Level1_Returns0()
    {
        // Arrange
        var service = new ExperienceService();

        // Act
        var xp = service.GetExperienceForLevel(1);

        // Assert
        Assert.Equal(0, xp);
    }

    [Fact]
    public void GetExperienceForLevel_Level2_Returns300()
    {
        // Arrange
        var service = new ExperienceService();

        // Act
        var xp = service.GetExperienceForLevel(2);

        // Assert
        Assert.Equal(300, xp);
    }

    [Fact]
    public void GetExperienceForLevel_Level5_Returns6500()
    {
        // Arrange
        var service = new ExperienceService();

        // Act
        var xp = service.GetExperienceForLevel(5);

        // Assert
        Assert.Equal(6500, xp);
    }

    [Fact]
    public void GetExperienceForLevel_Level20_Returns355000()
    {
        // Arrange
        var service = new ExperienceService();

        // Act
        var xp = service.GetExperienceForLevel(20);

        // Assert
        Assert.Equal(355000, xp);
    }

    [Fact]
    public void CalculateLevelFromExperience_With0XP_ReturnsLevel1()
    {
        // Arrange
        var service = new ExperienceService();

        // Act
        var level = service.CalculateLevelFromExperience(0);

        // Assert
        Assert.Equal(1, level);
    }

    [Fact]
    public void CalculateLevelFromExperience_With299XP_ReturnsLevel1()
    {
        // Arrange
        var service = new ExperienceService();

        // Act
        var level = service.CalculateLevelFromExperience(299);

        // Assert
        Assert.Equal(1, level);
    }

    [Fact]
    public void CalculateLevelFromExperience_With300XP_ReturnsLevel2()
    {
        // Arrange
        var service = new ExperienceService();

        // Act
        var level = service.CalculateLevelFromExperience(300);

        // Assert
        Assert.Equal(2, level);
    }

    [Fact]
    public void CalculateLevelFromExperience_With100000XP_ReturnsLevel12()
    {
        // Arrange
        var service = new ExperienceService();

        // Act
        var level = service.CalculateLevelFromExperience(100000);

        // Assert
        Assert.Equal(12, level);
    }

    [Fact]
    public void CalculateLevelFromExperience_With355000XP_ReturnsLevel20()
    {
        // Arrange
        var service = new ExperienceService();

        // Act
        var level = service.CalculateLevelFromExperience(355000);

        // Assert
        Assert.Equal(20, level);
    }

    [Fact]
    public void GetExperienceForNextLevel_At0XP_Returns300()
    {
        // Arrange
        var service = new ExperienceService();

        // Act
        var nextXp = service.GetExperienceForNextLevel(0);

        // Assert
        Assert.Equal(300, nextXp);
    }

    [Fact]
    public void GetExperienceForNextLevel_At350000XP_Returns355000()
    {
        // Arrange
        var service = new ExperienceService();

        // Act
        var nextXp = service.GetExperienceForNextLevel(350000);

        // Assert
        Assert.Equal(355000, nextXp); // Max level, cap at 355000
    }

    [Fact]
    public void CalculateLevelFromMilestone_With0XP_ReturnsLevel1()
    {
        // Arrange
        var service = new ExperienceService();

        // Act
        var level = service.CalculateLevelFromMilestone(0);

        // Assert
        Assert.Equal(1, level);
    }

    [Fact]
    public void CalculateLevelFromMilestone_With3999XP_ReturnsLevel1()
    {
        // Arrange
        var service = new ExperienceService();

        // Act
        var level = service.CalculateLevelFromMilestone(3999);

        // Assert
        Assert.Equal(1, level);
    }

    [Fact]
    public void CalculateLevelFromMilestone_With4000XP_ReturnsLevel2()
    {
        // Arrange
        var service = new ExperienceService();

        // Act
        var level = service.CalculateLevelFromMilestone(4000);

        // Assert
        Assert.Equal(2, level);
    }

    [Fact]
    public void CalculateLevelFromMilestone_With16000XP_ReturnsLevel5()
    {
        // Arrange
        var service = new ExperienceService();

        // Act
        var level = service.CalculateLevelFromMilestone(16000);

        // Assert
        Assert.Equal(5, level);
    }

    [Fact]
    public void GetExperienceForNextMilestone_At0XP_Returns4000()
    {
        // Arrange
        var service = new ExperienceService();

        // Act
        var nextXp = service.GetExperienceForNextMilestone(0);

        // Assert
        Assert.Equal(4000, nextXp);
    }

    [Fact]
    public void GetExperienceForNextMilestone_At8000XP_Returns12000()
    {
        // Arrange
        var service = new ExperienceService();

        // Act
        var nextXp = service.GetExperienceForNextMilestone(8000);

        // Assert
        Assert.Equal(12000, nextXp);
    }
}
