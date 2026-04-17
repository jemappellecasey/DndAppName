using DndApp.Api.CustomContent;
using DndApp.Api.Characters;
using DndApp.Api.Auth;
using DndApp.Api.Items;
using DndApp.Api.Mechanics;
using DndApp.Api.MixedRules;
using DndApp.Api.Data;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var provider = (builder.Configuration["Database:Provider"] ?? "sqlite").Trim().ToLowerInvariant();
    if (provider == "sqlite")
    {
        var connection = builder.Configuration["Database:ConnectionStrings:Sqlite"] ?? "Data Source=dndapp.sqlite";
        options.UseSqlite(connection);
        return;
    }

    throw new InvalidOperationException(
        $"Unsupported database provider '{provider}'. This phase supports 'sqlite'. PostgreSQL provider wiring is planned next.");
});
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
builder.Services.AddScoped<ICharacterBuildService, CharacterBuildService>();

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
app.MapGet(
    "/db/health",
    async (AppDbContext db, CancellationToken cancellationToken) =>
    {
        var canConnect = await db.Database.CanConnectAsync(cancellationToken);
        return Results.Ok(new { canConnect });
    });

app.MapGet(
    "/catalog/classes",
    async (RuleSystemMode ruleSystem, AppDbContext db, CancellationToken cancellationToken) =>
    {
        var classes = await (
            from module in db.RuleModules
            where EF.Functions.Like(module.ModuleType, "%class%")
            join source in db.ContentSources on module.ContentSourceId equals source.Id into sourceJoin
            from source in sourceJoin.DefaultIfEmpty()
            where source == null
                || (ruleSystem == RuleSystemMode.Rules2014
                    ? source.RuleSystemId == "rules-2014"
                    : source.RuleSystemId == "rules-2024")
            orderby module.DisplayName
            select new ClassCatalogItem(
                module.Id,
                module.DisplayName,
                source != null ? source.Code : module.ContentSourceId,
                module.VersionTag))
            .ToArrayAsync(cancellationToken);

        return Results.Ok(classes);
    });

app.MapGet(
    "/catalog/items",
    async (AppDbContext db, CancellationToken cancellationToken) =>
    {
        var itemRows = await (
            from item in db.ItemDefinitions
            join module in db.RuleModules on item.RuleModuleId equals module.Id into moduleJoin
            from module in moduleJoin.DefaultIfEmpty()
            join source in db.ContentSources on module.ContentSourceId equals source.Id into sourceJoin
            from source in sourceJoin.DefaultIfEmpty()
            orderby module != null ? module.DisplayName : item.Id
            select new ItemCatalogItem(
                item.Id,
                module != null ? module.DisplayName : item.Id,
                source != null ? source.Code : string.Empty,
                item.ItemType,
                item.Rarity,
                item.RequiresAttunement,
                Array.Empty<ItemCatalogEffect>()))
            .ToArrayAsync(cancellationToken);

        var itemIds = itemRows.Select(x => x.ItemId).ToArray();
        var effectRows = await db.ItemEffects
            .Where(x => itemIds.Contains(x.ItemDefinitionId))
            .Select(x => new
            {
                x.ItemDefinitionId,
                Effect = new ItemCatalogEffect(
                    x.Id,
                    x.EffectType,
                    x.EffectPayloadJson,
                    x.ConditionJson)
            })
            .ToArrayAsync(cancellationToken);

        var effectsByItem = effectRows
            .GroupBy(x => x.ItemDefinitionId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<ItemCatalogEffect>)x.Select(y => y.Effect).ToArray(), StringComparer.OrdinalIgnoreCase);

        var items = itemRows
            .Select(x => x with
            {
                Effects = effectsByItem.TryGetValue(x.ItemId, out var effects)
                    ? effects
                    : Array.Empty<ItemCatalogEffect>()
            })
            .ToArray();

        return Results.Ok(items);
    });

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

app.MapGet(
    "/characters/{characterId:guid}/build",
    async (Guid characterId, ICharacterBuildService buildService, CancellationToken cancellationToken) =>
    {
        var build = await buildService.GetBuildAsync(characterId, cancellationToken);
        return build is null ? Results.NotFound() : Results.Ok(build);
    });

app.MapPut(
    "/characters/{characterId:guid}/build",
    async (Guid characterId, UpsertCharacterBuildRequest request, ICharacterBuildService buildService, CancellationToken cancellationToken) =>
    {
        var result = await buildService.UpsertBuildAsync(characterId, request, cancellationToken);
        return result.Errors.Count > 0
            ? Results.BadRequest(new { errors = result.Errors })
            : Results.Ok(result.Data);
    });

app.MapPatch(
    "/characters/{characterId:guid}/build",
    async (Guid characterId, PatchCharacterBuildRequest request, ICharacterBuildService buildService, CancellationToken cancellationToken) =>
    {
        var result = await buildService.PatchBuildAsync(characterId, request, cancellationToken);
        if (result.Data is null && result.Errors.Count == 1 && string.Equals(result.Errors[0], "Character build was not found.", StringComparison.Ordinal))
        {
            return Results.NotFound(new { errors = result.Errors });
        }

        return result.Errors.Count > 0
            ? Results.BadRequest(new { errors = result.Errors })
            : Results.Ok(result.Data);
    });

app.MapDelete(
    "/characters/{characterId:guid}/build",
    async (Guid characterId, ICharacterBuildService buildService, CancellationToken cancellationToken) =>
    {
        var deleted = await buildService.DeleteBuildAsync(characterId, cancellationToken);
        return deleted ? Results.NoContent() : Results.NotFound();
    });

app.Run();

public sealed record ClassCatalogItem(
    string ModuleId,
    string ClassName,
    string SourceCode,
    string VersionTag);

public sealed record ItemCatalogEffect(
    string EffectId,
    string EffectType,
    string EffectPayloadJson,
    string ConditionJson);

public sealed record ItemCatalogItem(
    string ItemId,
    string ItemName,
    string SourceCode,
    string ItemType,
    string Rarity,
    bool RequiresAttunement,
    IReadOnlyList<ItemCatalogEffect> Effects);
