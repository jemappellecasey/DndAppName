using DndApp.Api.CustomContent;
using DndApp.Api.Characters;
using DndApp.Api.Items;
using DndApp.Api.Mechanics;
using DndApp.Api.MixedRules;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ICustomContentValidationService, CustomContentValidationService>();
builder.Services.AddSingleton<IItemEffectPipelineService, ItemEffectPipelineService>();
builder.Services.AddSingleton<ICalculationEngineService, CalculationEngineService>();
builder.Services.AddSingleton<IMixedRulesResolutionService, MixedRulesResolutionService>();
builder.Services.AddSingleton<ICharacterWizardService, CharacterWizardService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost(
    "/characters/{characterId:guid}/custom/origin",
    (Guid characterId, CreateCustomOriginRequest request, ICustomContentValidationService validator) =>
    {
        var result = validator.ValidateOrigin(request);
        if (request.Mode == CustomContentMode.GuidedCustom && !result.IsValid)
        {
            return Results.BadRequest(new
            {
                characterId,
                mode = request.Mode,
                result.Errors,
                result.Warnings
            });
        }

        var response = new
        {
            characterId,
            mode = request.Mode,
            tag = request.Mode == CustomContentMode.GuidedCustom ? "guided-custom" : "fully-custom",
            request.Name,
            request.AbilityBonuses,
            request.SkillProficiencies,
            request.FeatureNotes,
            result.IsValid,
            result.Errors,
            result.Warnings
        };

        return Results.Ok(response);
    });

app.MapPost(
    "/characters/{characterId:guid}/custom/species",
    (Guid characterId, CreateCustomSpeciesRequest request, ICustomContentValidationService validator) =>
    {
        var result = validator.ValidateSpecies(request);
        if (request.Mode == CustomContentMode.GuidedCustom && !result.IsValid)
        {
            return Results.BadRequest(new
            {
                characterId,
                mode = request.Mode,
                result.Errors,
                result.Warnings
            });
        }

        var response = new
        {
            characterId,
            mode = request.Mode,
            tag = request.Mode == CustomContentMode.GuidedCustom ? "guided-custom" : "fully-custom",
            request.Name,
            request.Size,
            request.WalkingSpeed,
            request.Traits,
            request.Languages,
            result.IsValid,
            result.Errors,
            result.Warnings
        };

        return Results.Ok(response);
    });

app.MapPost(
    "/characters/{characterId:guid}/items/apply-effects",
    (Guid characterId, ApplyItemEffectsRequest request, IItemEffectPipelineService pipeline) =>
    {
        var result = pipeline.ApplyEffects(request);
        return Results.Ok(new
        {
            characterId,
            result.DerivedStats,
            result.Breakdown
        });
    });

app.MapPost(
    "/characters/{characterId:guid}/compute/check",
    (Guid characterId, ComputeCheckRequest request, ICalculationEngineService calculations) =>
    {
        var result = calculations.ComputeCheck(request);
        return Results.Ok(new { characterId, result });
    });

app.MapPost(
    "/characters/{characterId:guid}/compute/save",
    (Guid characterId, ComputeSaveRequest request, ICalculationEngineService calculations) =>
    {
        var result = calculations.ComputeSave(request);
        return Results.Ok(new { characterId, result });
    });

app.MapPost(
    "/characters/{characterId:guid}/compute/attack",
    (Guid characterId, ComputeAttackRequest request, ICalculationEngineService calculations) =>
    {
        var result = calculations.ComputeAttack(request);
        return Results.Ok(new { characterId, result });
    });

app.MapPost(
    "/rules/resolve-mixed",
    (MixedRulesResolveRequest request, IMixedRulesResolutionService resolver) =>
    {
        var result = resolver.Resolve(request);
        return Results.Ok(result);
    });

app.MapPost(
    "/wizard/characters/start",
    (StartCharacterWizardRequest request, ICharacterWizardService wizardService) =>
    {
        var result = wizardService.StartDraft(request);
        return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
    });

app.MapGet(
    "/wizard/characters/{characterId:guid}",
    (Guid characterId, ICharacterWizardService wizardService) =>
    {
        var result = wizardService.GetDraft(characterId);
        return result is null ? Results.NotFound() : Results.Ok(result);
    });

app.MapPost(
    "/wizard/characters/{characterId:guid}/steps",
    (Guid characterId, SubmitWizardStepRequest request, ICharacterWizardService wizardService) =>
    {
        var result = wizardService.SubmitStep(characterId, request);
        return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
    });

app.MapPost(
    "/wizard/characters/{characterId:guid}/finalize",
    (Guid characterId, FinalizeWizardRequest request, ICharacterWizardService wizardService) =>
    {
        var result = wizardService.Finalize(characterId, request);
        return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
    });

app.MapPost(
    "/characters/{characterId:guid}/copy-to-ruleset",
    (Guid characterId, CopyCharacterRulesetRequest request, ICharacterWizardService wizardService) =>
    {
        var result = wizardService.CopyToRuleset(characterId, request);
        return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
    });

app.Run();
