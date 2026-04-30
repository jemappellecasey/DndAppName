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
}
