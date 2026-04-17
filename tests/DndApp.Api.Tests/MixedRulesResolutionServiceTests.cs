using DndApp.Api.MixedRules;

namespace DndApp.Api.Tests;

public sealed class MixedRulesResolutionServiceTests
{
    [Fact]
    public void Resolve_UsesBasePrecedence_WhenNoExplicitOverride()
    {
        var service = new MixedRulesResolutionService();
        var request = new MixedRulesResolveRequest(
            RuleSystemMode.Rules2014,
            MixedModeEnabled: true,
            OverlaySources: new[] { "PHB2024" },
            Selections: new[]
            {
                new RuleModuleSelection("species", "species-2014-human", "PHB2014", true, true),
                new RuleModuleSelection("species", "species-2024-human", "PHB2024", true, true),
            },
            ExplicitOverridesBySlot: null);

        var result = service.Resolve(request);

        var resolved = Assert.Single(result.ResolvedSlots);
        Assert.Equal("species-2014-human", resolved.Winner.ModuleId);
        Assert.Contains(result.Warnings, w => w.Contains("base rules precedence", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Resolve_UsesExplicitOverride_WhenProvided()
    {
        var service = new MixedRulesResolutionService();
        var request = new MixedRulesResolveRequest(
            RuleSystemMode.Rules2014,
            MixedModeEnabled: true,
            OverlaySources: new[] { "PHB2024" },
            Selections: new[]
            {
                new RuleModuleSelection("origin", "origin-2014", "PHB2014", true, true),
                new RuleModuleSelection("origin", "origin-2024", "PHB2024", true, true),
            },
            ExplicitOverridesBySlot: new Dictionary<string, string>
            {
                ["origin"] = "origin-2024"
            });

        var result = service.Resolve(request);

        var resolved = Assert.Single(result.ResolvedSlots);
        Assert.Equal("origin-2024", resolved.Winner.ModuleId);
        Assert.Contains(result.Warnings, w => w.Contains("explicit override", StringComparison.OrdinalIgnoreCase));
    }
}
