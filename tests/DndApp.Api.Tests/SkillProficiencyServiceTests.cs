using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DndApp.Api.Data;
using DndApp.Api.Mechanics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DndApp.Api.Tests;

public sealed class SkillProficiencyServiceTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;
    private SkillProficiencyService _service = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        await _db.Database.EnsureCreatedAsync();

        _service = new SkillProficiencyService(_db);

        // Seed test data
        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        // Seed a few class proficiencies for testing
        var rogueSkills = new[]
        {
            "Acrobatics", "Animal Handling", "Arcana", "Athletics", "Deception",
            "History", "Insight", "Investigation", "Medicine", "Nature", "Perception",
            "Performance", "Persuasion", "Religion", "Sleight of Hand", "Stealth", "Survival"
        };

        var barbarianSkills = new[] { "Animal Handling", "Athletics", "Insight", "Intimidation", "Nature", "Perception", "Survival" };
        var barbarianExpertise = new[] { "Animal Handling" };

        // Insert Rogue proficiencies
        foreach (var skill in rogueSkills)
        {
            _db.SkillProficiencySources.Add(new SkillProficiencySourceEntity
            {
                Id = $"rogue-2024-{skill.ToLower().Replace(" ", "-")}",
                SourceType = "class",
                SourceId = "rogue",
                SourceName = "Rogue",
                SkillName = skill,
                IsExpertise = false,
                IsChoice = false,
                Edition = "2024"
            });
        }

        // Insert Barbarian proficiencies (some as expertise)
        foreach (var skill in barbarianSkills)
        {
            var isExpertise = barbarianExpertise.Contains(skill);
            _db.SkillProficiencySources.Add(new SkillProficiencySourceEntity
            {
                Id = $"barbarian-2024-{skill.ToLower().Replace(" ", "-")}",
                SourceType = "class",
                SourceId = "barbarian",
                SourceName = "Barbarian",
                SkillName = skill,
                IsExpertise = isExpertise,
                IsChoice = false,
                Edition = "2024"
            });
        }

        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetClassSkillProficienciesAsync_WithValidRogueClass_ReturnsAllSkills()
    {
        // Arrange
        var className = "Rogue";
        var edition = "2024";

        // Act
        var result = await _service.GetClassSkillProficienciesAsync(className, edition);

        // Assert
        Assert.NotEmpty(result);
        Assert.Equal(17, result.Count);
        Assert.Contains(result, s => s.SkillName == "Acrobatics");
        Assert.Contains(result, s => s.SkillName == "Stealth");
    }

    [Fact]
    public async Task GetClassSkillProficienciesAsync_SkillsHaveCorrectProperties()
    {
        // Arrange
        var className = "Rogue";
        var edition = "2024";

        // Act
        var result = await _service.GetClassSkillProficienciesAsync(className, edition);
        var firstSkill = result.First();

        // Assert
        Assert.NotNull(firstSkill.SkillName);
        Assert.Equal("class", firstSkill.SourceType);
        Assert.Equal("rogue", firstSkill.SourceId);
        Assert.Equal("Rogue", firstSkill.SourceName);
        Assert.False(firstSkill.IsExpertise);
        Assert.False(firstSkill.IsChoice);
    }

    [Fact]
    public async Task GetClassSkillProficienciesAsync_WithBarbarianClass_IncludesExpertiseSkills()
    {
        // Arrange
        var className = "Barbarian";
        var edition = "2024";

        // Act
        var result = await _service.GetClassSkillProficienciesAsync(className, edition);

        // Assert
        Assert.NotEmpty(result);
        var animalHandling = result.FirstOrDefault(s => s.SkillName == "Animal Handling");
        Assert.NotNull(animalHandling);
        Assert.True(animalHandling.IsExpertise);
    }

    [Fact]
    public async Task GetSourceSkillsAsync_WithValidSource_ReturnsSkillNames()
    {
        // Arrange
        var sourceType = "class";
        var sourceId = "rogue";
        var edition = "2024";

        // Act
        var result = await _service.GetSourceSkillsAsync(sourceType, sourceId, edition);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("Acrobatics", result);
        Assert.Contains("Stealth", result);
        Assert.DoesNotContain(null, result);
    }

    [Fact]
    public async Task GetSourceSkillsAsync_WithInvalidSource_ReturnsEmpty()
    {
        // Arrange
        var sourceType = "class";
        var sourceId = "nonexistent";
        var edition = "2024";

        // Act
        var result = await _service.GetSourceSkillsAsync(sourceType, sourceId, edition);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetClassSkillProficienciesAsync_WithWrongEdition_ReturnsEmpty()
    {
        // Arrange
        var className = "Rogue";
        var edition = "2014"; // Different from seeded 2024

        // Act
        var result = await _service.GetClassSkillProficienciesAsync(className, edition);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetClassSkillProficienciesAsync_WithCaseInsensitiveClassName_ReturnsSkills()
    {
        // Arrange
        var className = "ROGUE"; // Different case
        var edition = "2024";

        // Act
        var result = await _service.GetClassSkillProficienciesAsync(className, edition);

        // Assert - Should work because we use ToLower() in the query
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task SeedBackgroundProficiencies_Soldier_HasAthletics()
    {
        // Arrange
        var backgroundName = "Soldier";
        var edition = "2024";
        
        // Add background proficiency
        _db.SkillProficiencySources.Add(new SkillProficiencySourceEntity
        {
            Id = "background-test-soldier-athletics",
            SourceType = "background",
            SourceId = "soldier",
            SourceName = "Soldier",
            SkillName = "Athletics",
            IsExpertise = false,
            IsChoice = false,
            Edition = edition
        });
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetSourceSkillsAsync("background", "soldier", edition);

        // Assert
        Assert.Contains("Athletics", result);
    }

    [Fact]
    public async Task SeedRaceProficiencies_HalfElf_HasMultipleSkills()
    {
        // Arrange
        var raceName = "Half-Elf";
        var edition = "2024";
        
        // Add race proficiencies
        var skills = new[] { "Insight", "Persuasion" };
        foreach (var skill in skills)
        {
            _db.SkillProficiencySources.Add(new SkillProficiencySourceEntity
            {
                Id = $"race-test-half-elf-{skill.ToLower()}",
                SourceType = "race",
                SourceId = "half-elf",
                SourceName = "Half-Elf",
                SkillName = skill,
                IsExpertise = false,
                IsChoice = false,
                Edition = edition
            });
        }
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetSourceSkillsAsync("race", "half-elf", edition);

        // Assert
        Assert.Contains("Insight", result);
        Assert.Contains("Persuasion", result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetClassSkillProficienciesAsync_BackgroundAndRaceDataSeeded_ReturnsCorrectTypes()
    {
        // Arrange
        var sourceTypes = new[] { "class", "background", "race" };

        // Add test proficiencies for each source type
        _db.SkillProficiencySources.Add(new SkillProficiencySourceEntity
        {
            Id = "test-background-acolyte",
            SourceType = "background",
            SourceId = "acolyte",
            SourceName = "Acolyte",
            SkillName = "Insight",
            IsExpertise = false,
            IsChoice = false,
            Edition = "2024"
        });

        _db.SkillProficiencySources.Add(new SkillProficiencySourceEntity
        {
            Id = "test-race-dwarf",
            SourceType = "race",
            SourceId = "dwarf",
            SourceName = "Dwarf",
            SkillName = "Insight",
            IsExpertise = false,
            IsChoice = false,
            Edition = "2024"
        });

        await _db.SaveChangesAsync();

        // Act
        var backgroundSkills = await _service.GetSourceSkillsAsync("background", "acolyte", "2024");
        var raceSkills = await _service.GetSourceSkillsAsync("race", "dwarf", "2024");

        // Assert
        Assert.NotEmpty(backgroundSkills);
        Assert.NotEmpty(raceSkills);
        Assert.Contains("Insight", backgroundSkills);
        Assert.Contains("Insight", raceSkills);
    }
}
