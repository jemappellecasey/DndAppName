using DndApp.Api.CustomContent;
using DndApp.Api.Characters;
using DndApp.Api.Auth;
using DndApp.Api.Items;
using DndApp.Api.Mechanics;
using DndApp.Api.MixedRules;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend-dev", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "http://localhost:4173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddSingleton<ICustomContentValidationService, CustomContentValidationService>();
builder.Services.AddSingleton<IItemEffectPipelineService, ItemEffectPipelineService>();
builder.Services.AddSingleton<ICalculationEngineService, CalculationEngineService>();
builder.Services.AddSingleton<IMixedRulesResolutionService, MixedRulesResolutionService>();
builder.Services.AddSingleton<ICharacterWizardService, CharacterWizardService>();
builder.Services.AddSingleton<ILocalAuthService, LocalAuthService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("frontend-dev");

app.Use(async (context, next) =>
{
    var started = DateTimeOffset.UtcNow;
    await next();
    var elapsedMs = (DateTimeOffset.UtcNow - started).TotalMilliseconds;
    app.Logger.LogInformation(
        "Request {Method} {Path} responded {StatusCode} in {ElapsedMs:0.00}ms",
        context.Request.Method,
        context.Request.Path,
        context.Response.StatusCode,
        elapsedMs);
});

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/", () => Results.Ok(new { name = "DndApp.Api", status = "ok", health = "/health" }));

app.MapPost(
    "/auth/local/login",
    (LocalLoginRequest request, ILocalAuthService auth) => Results.Ok(auth.Login(request)));

app.MapGet(
    "/auth/local/me",
    (string sessionToken, ILocalAuthService auth) =>
    {
        var session = auth.GetSession(sessionToken);
        return session is null ? Results.NotFound() : Results.Ok(session);
    });

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

app.MapGet(
    "/custom/builders/origin/guidance",
    (ICustomContentValidationService validator) => Results.Ok(validator.GetOriginGuidance()));

app.MapGet(
    "/custom/builders/species/guidance",
    (ICustomContentValidationService validator) => Results.Ok(validator.GetSpeciesGuidance()));

app.MapPost(
    "/custom/builders/origin/preview",
    (CreateCustomOriginRequest request, ICustomContentValidationService validator) => Results.Ok(validator.PreviewOrigin(request)));

app.MapPost(
    "/custom/builders/species/preview",
    (CreateCustomSpeciesRequest request, ICustomContentValidationService validator) => Results.Ok(validator.PreviewSpecies(request)));

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

app.MapGet(
    "/inventory/attunement-guidance",
    (IItemEffectPipelineService pipeline) => Results.Ok(pipeline.GetAttunementGuidance()));

app.MapPost(
    "/characters/{characterId:guid}/inventory/update-item-state",
    (Guid characterId, UpdateInventoryItemStateRequest request, IItemEffectPipelineService pipeline) =>
    {
        var result = pipeline.UpdateItemState(request);
        return Results.Ok(new { characterId, result });
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

app.MapGet(
    "/characters",
    (bool includeArchived, ICharacterWizardService wizardService) =>
    {
        var result = wizardService.ListCharacters(includeArchived);
        return Results.Ok(result);
    });

app.MapGet(
    "/characters/{characterId:guid}",
    (Guid characterId, ICharacterWizardService wizardService) =>
    {
        var result = wizardService.GetCharacter(characterId);
        return result is null ? Results.NotFound() : Results.Ok(result);
    });

app.MapPatch(
    "/characters/{characterId:guid}",
    (Guid characterId, UpdateCharacterRequest request, ICharacterWizardService wizardService) =>
    {
        var result = wizardService.UpdateCharacter(characterId, request);
        return result is null ? Results.NotFound() : Results.Ok(result);
    });

app.MapPost(
    "/characters/{characterId:guid}/archive",
    (Guid characterId, ICharacterWizardService wizardService) =>
    {
        var result = wizardService.ArchiveCharacter(characterId);
        return result is null ? Results.NotFound() : Results.Ok(result);
    });

app.MapPost(
    "/characters/{characterId:guid}/duplicate",
    (Guid characterId, DuplicateCharacterRequest request, ICharacterWizardService wizardService) =>
    {
        var result = wizardService.DuplicateCharacter(characterId, request);
        return result is null ? Results.NotFound() : Results.Ok(result);
    });

app.MapGet(
    "/characters/{characterId:guid}/history",
    (Guid characterId, ICharacterWizardService wizardService) =>
    {
        var result = wizardService.GetCharacterHistory(characterId);
        return Results.Ok(result);
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
