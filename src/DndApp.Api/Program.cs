using DndApp.Api.CustomContent;
using DndApp.Api.Characters;
using DndApp.Api.Auth;
using DndApp.Api.Items;
using DndApp.Api.Mechanics;
using DndApp.Api.MixedRules;
using DndApp.Api.Data;
using System.Text.Json;
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
        $"Unsupported database provider '{provider}'. Supported provider: 'sqlite'.");
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
builder.Services.AddSingleton<ICatalogCacheService, CatalogCacheService>();
builder.Services.AddScoped<ICharacterWizardService, CharacterWizardService>();
builder.Services.AddScoped<ILocalAuthService, LocalAuthService>();
builder.Services.AddScoped<ICharacterBuildService, CharacterBuildService>();
builder.Services.AddScoped<ICharacterInventoryService, CharacterInventoryService>();
builder.Services.AddScoped<ICharacterComputationService, CharacterComputationService>();
builder.Services.AddScoped<ICharacterProgressionService, CharacterProgressionService>();
builder.Services.AddScoped<IRuleValidationService, RuleValidationService>();
builder.Services.AddScoped<ISkillProficiencyService, SkillProficiencyService>();
builder.Services.AddScoped<IExperienceService, ExperienceService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    var cacheService = scope.ServiceProvider.GetRequiredService<ICatalogCacheService>();
    await cacheService.InitializeAsync(CancellationToken.None);
}

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
        if (ruleSystem == RuleSystemMode.Rules2014)
        {
            var typedRows = await (
                from item in db.Classes2014
                join source in db.ContentSources on item.ContentSourceId equals source.Id into sourceJoin
                from source in sourceJoin.DefaultIfEmpty()
                orderby item.Name
                select new ClassCatalogItem(
                    item.Id,
                    item.Name,
                    source != null ? source.Code : item.ContentSourceId,
                    "v1"))
                .ToArrayAsync(cancellationToken);
            if (typedRows.Length > 0)
            {
                return Results.Ok(typedRows);
            }
        }
        else
        {
            var typedRows = await (
                from item in db.Classes2024
                join source in db.ContentSources on item.ContentSourceId equals source.Id into sourceJoin
                from source in sourceJoin.DefaultIfEmpty()
                orderby item.Name
                select new ClassCatalogItem(
                    item.Id,
                    item.Name,
                    source != null ? source.Code : item.ContentSourceId,
                    "v1"))
                .ToArrayAsync(cancellationToken);
            if (typedRows.Length > 0)
            {
                return Results.Ok(typedRows);
            }
        }

        var fallbackRows = await (
            from module in db.RuleModules
            where module.ModuleType == "class"
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

        return Results.Ok(fallbackRows);
    });

app.MapGet(
    "/catalog/subclasses",
    async (RuleSystemMode ruleSystem, string classId, AppDbContext db, CancellationToken cancellationToken) =>
    {
        if (ruleSystem == RuleSystemMode.Rules2014)
        {
            var typedRows = await (
                from item in db.Subclasses2014
                join source in db.ContentSources on item.ContentSourceId equals source.Id into sourceJoin
                from source in sourceJoin.DefaultIfEmpty()
                where item.ParentClassId == classId
                orderby item.Name
                select new SubclassCatalogItem(
                    item.Id,
                    item.ParentClassId,
                    item.Name,
                    source != null ? source.Code : item.ContentSourceId,
                    item.SubclassFeatureStartLevel))
                .ToArrayAsync(cancellationToken);
            if (typedRows.Length > 0)
            {
                return Results.Ok(typedRows);
            }
        }
        else
        {
            var typedRows = await (
                from item in db.Subclasses2024
                join source in db.ContentSources on item.ContentSourceId equals source.Id into sourceJoin
                from source in sourceJoin.DefaultIfEmpty()
                where item.ParentClassId == classId
                orderby item.Name
                select new SubclassCatalogItem(
                    item.Id,
                    item.ParentClassId,
                    item.Name,
                    source != null ? source.Code : item.ContentSourceId,
                    item.SubclassFeatureStartLevel))
                .ToArrayAsync(cancellationToken);
            if (typedRows.Length > 0)
            {
                return Results.Ok(typedRows);
            }
        }

        var fallbackRows = await (
            from module in db.RuleModules
            where module.ModuleType == "subclass"
            join source in db.ContentSources on module.ContentSourceId equals source.Id into sourceJoin
            from source in sourceJoin.DefaultIfEmpty()
            where source == null
                || (ruleSystem == RuleSystemMode.Rules2014
                    ? source.RuleSystemId == "rules-2014"
                    : source.RuleSystemId == "rules-2024")
            orderby module.DisplayName
            select new SubclassCatalogItem(
                module.Id,
                classId,
                module.DisplayName,
                source != null ? source.Code : module.ContentSourceId,
                3))
            .ToArrayAsync(cancellationToken);

        return Results.Ok(fallbackRows);
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
                item.GoldValue,
                item.Weight,
                item.IsWeapon,
                item.DamageDice,
                item.WeaponAbility,
                item.AttackBonus,
                item.DamageBonus,
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

app.MapGet(
    "/catalog/content-sources",
    async (RuleSystemMode? ruleSystem, AppDbContext db, CancellationToken cancellationToken) =>
    {
        var sources = await db.ContentSources
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new ContentSourceCatalogItem(
                x.Code,
                x.Name,
                x.RuleSystemId == "rules-2014" ? RuleSystemMode.Rules2014 : RuleSystemMode.Rules2024))
            .ToArrayAsync(cancellationToken);

        if (ruleSystem is null)
        {
            return Results.Ok(sources);
        }

        var filtered = sources
            .Where(x => x.RuleSystem != ruleSystem.Value)
            .ToArray();
        return Results.Ok(filtered);
    });

app.MapGet(
    "/catalog/modules",
    async (
        RuleSystemMode baseRuleSystem,
        bool mixedMode,
        string[]? overlaySources,
        string[]? moduleTypes,
        AppDbContext db,
        CancellationToken cancellationToken) =>
    {
        var baseRuleSystemId = baseRuleSystem == RuleSystemMode.Rules2014 ? "rules-2014" : "rules-2024";
        var baseSourceCodes = await db.ContentSources
            .AsNoTracking()
            .Where(x => x.RuleSystemId == baseRuleSystemId)
            .Select(x => x.Code)
            .ToArrayAsync(cancellationToken);

        var allowedSources = new HashSet<string>(baseSourceCodes, StringComparer.OrdinalIgnoreCase);
        if (mixedMode && overlaySources is not null)
        {
            foreach (var source in overlaySources.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                allowedSources.Add(source.Trim());
            }
        }

        var requestedModuleTypes = (moduleTypes ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var rows = await (
            from module in db.RuleModules
            join source in db.ContentSources on module.ContentSourceId equals source.Id
            join variant in db.RuleVariants on module.Id equals variant.RuleModuleId into variantJoin
            from variant in variantJoin.DefaultIfEmpty()
            where allowedSources.Contains(source.Code)
            select new
            {
                Module = module,
                Source = source,
                VariantPayloadJson = variant != null ? variant.PayloadJson : null
            })
            .ToListAsync(cancellationToken);

        var moduleIds = rows
            .Select(x => x.Module.Id)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var prerequisiteRows = await db.Prerequisites
            .AsNoTracking()
            .Where(x => moduleIds.Contains(x.RuleModuleId))
            .Select(x => new { x.RuleModuleId, x.PredicateJson })
            .ToArrayAsync(cancellationToken);
        var minLevelByModule = prerequisiteRows
            .GroupBy(x => x.RuleModuleId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                x => x.Key,
                x => CatalogParsing.ParseMinLevelRequirement(x.Select(y => y.PredicateJson)),
                StringComparer.OrdinalIgnoreCase);
        var abilityReqByModule = prerequisiteRows
            .GroupBy(x => x.RuleModuleId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                x => x.Key,
                x => CatalogParsing.ParseAbilityRequirements(x.Select(y => y.PredicateJson)),
                StringComparer.OrdinalIgnoreCase);
        var subclassMetadataByModule = (await db.Subclasses2014
            .AsNoTracking()
            .Select(x => new
            {
                ModuleId = x.Id,
                ParentClassId = x.ParentClassId,
                x.SubclassFeatureStartLevel
            })
            .Concat(
                db.Subclasses2024
                    .AsNoTracking()
                    .Select(x => new
                    {
                        ModuleId = x.Id,
                        ParentClassId = x.ParentClassId,
                        x.SubclassFeatureStartLevel
                    }))
            .ToArrayAsync(cancellationToken))
            .GroupBy(x => x.ModuleId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                x => x.Key,
                x => x.First(),
                StringComparer.OrdinalIgnoreCase);

        var filtered = rows
            .Where(x => requestedModuleTypes.Count == 0 || requestedModuleTypes.Contains(x.Module.ModuleType))
            .GroupBy(x => $"{x.Module.ModuleType}\u001F{x.Module.DisplayName}\u001F{x.Source.Code}".ToUpperInvariant())
            .Select(g => g.First())
            .OrderBy(x => x.Module.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(x => new ModuleCatalogItem(
                x.Module.Id,
                x.Module.ModuleType,
                x.Module.DisplayName,
                x.Source.Code,
                x.Module.VersionTag,
                CatalogParsing.ParseAbilityBonuses(x.VariantPayloadJson),
                CatalogParsing.ParseStringArray(x.VariantPayloadJson, "fixedSkillProficiencies"),
                CatalogParsing.ParseStringArray(x.VariantPayloadJson, "skillChoices"),
                CatalogParsing.ParseInt(x.VariantPayloadJson, "skillChoiceCount"),
                CatalogParsing.ParseInt(x.VariantPayloadJson, "expertiseChoiceCount"),
                CatalogParsing.ParseStringArray(x.VariantPayloadJson, "fixedToolProficiencies"),
                CatalogParsing.ParseStringArray(x.VariantPayloadJson, "toolChoices"),
                CatalogParsing.ParseInt(x.VariantPayloadJson, "toolChoiceCount"),
                CatalogParsing.ParseStringArray(x.VariantPayloadJson, "fixedLanguages"),
                CatalogParsing.ParseStringArray(x.VariantPayloadJson, "languageChoices"),
                CatalogParsing.ParseInt(x.VariantPayloadJson, "languageChoiceCount"),
                minLevelByModule.TryGetValue(x.Module.Id, out var minLevel) ? minLevel : 0,
                abilityReqByModule.TryGetValue(x.Module.Id, out var abilities)
                    ? abilities
                    : new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
                CatalogParsing.ParseStringArray(x.VariantPayloadJson, "spellClasses"),
                CatalogParsing.ParseWalkingSpeed(x.VariantPayloadJson),
                subclassMetadataByModule.TryGetValue(x.Module.Id, out var subclassMetadata)
                    ? subclassMetadata.ParentClassId
                    : null,
                subclassMetadataByModule.TryGetValue(x.Module.Id, out subclassMetadata)
                    ? subclassMetadata.SubclassFeatureStartLevel
                    : null))
            .ToArray();

        return Results.Ok(filtered);
    });

app.MapPost(
    "/auth/local/register",
    async (LocalRegisterRequest request, ILocalAuthService auth, CancellationToken cancellationToken) =>
    {
        try
        {
            return Results.Ok(await auth.RegisterAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { errors = new[] { ex.Message } });
        }
    });

app.MapPost(
    "/auth/local/login",
    async (LocalLoginRequest request, ILocalAuthService auth, CancellationToken cancellationToken) =>
    {
        try
        {
            return Results.Ok(await auth.LoginAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { errors = new[] { ex.Message } });
        }
    });

app.MapGet(
    "/auth/local/me",
    async (HttpContext httpContext, ILocalAuthService auth, CancellationToken cancellationToken) =>
    {
        var sessionToken = EndpointAuth.ResolveSessionToken(httpContext);
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return Results.Unauthorized();
        }

        var session = await auth.GetSessionAsync(sessionToken, cancellationToken);
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

app.MapGet(
    "/characters/{characterId:guid}/inventory",
    async (Guid characterId, HttpContext httpContext, AppDbContext db, ILocalAuthService auth, ICharacterInventoryService inventoryService, CancellationToken cancellationToken) =>
    {
        var ownerResult = await EndpointAuth.AuthorizeCharacterOwnerAsync(httpContext, characterId, db, auth, cancellationToken);
        if (ownerResult is not null)
        {
            return ownerResult;
        }
        var state = await inventoryService.GetInventoryAsync(characterId, cancellationToken);
        return state is null ? Results.NotFound() : Results.Ok(state);
    });

app.MapPost(
    "/characters/{characterId:guid}/inventory/items",
    async (Guid characterId, AddInventoryItemRequest request, HttpContext httpContext, AppDbContext db, ILocalAuthService auth, ICharacterInventoryService inventoryService, CancellationToken cancellationToken) =>
    {
        var ownerResult = await EndpointAuth.AuthorizeCharacterOwnerAsync(httpContext, characterId, db, auth, cancellationToken);
        if (ownerResult is not null)
        {
            return ownerResult;
        }
        var result = await inventoryService.AddItemAsync(characterId, request, cancellationToken);
        if (result.State is null && result.Errors.Count > 0)
        {
            return Results.BadRequest(new { errors = result.Errors });
        }
        return Results.Ok(result.State);
    });

app.MapPatch(
    "/characters/{characterId:guid}/inventory/items/{inventoryItemId}",
    async (Guid characterId, string inventoryItemId, PatchInventoryItemStateRequest request, HttpContext httpContext, AppDbContext db, ILocalAuthService auth, ICharacterInventoryService inventoryService, CancellationToken cancellationToken) =>
    {
        var ownerResult = await EndpointAuth.AuthorizeCharacterOwnerAsync(httpContext, characterId, db, auth, cancellationToken);
        if (ownerResult is not null)
        {
            return ownerResult;
        }
        var result = await inventoryService.UpdateItemStateAsync(characterId, inventoryItemId, request, cancellationToken);
        if (result.State is null && result.Errors.Count > 0)
        {
            return Results.BadRequest(new { errors = result.Errors });
        }
        return Results.Ok(result.State);
    });

app.MapDelete(
    "/characters/{characterId:guid}/inventory/items/{inventoryItemId}",
    async (Guid characterId, string inventoryItemId, HttpContext httpContext, AppDbContext db, ILocalAuthService auth, ICharacterInventoryService inventoryService, CancellationToken cancellationToken) =>
    {
        var ownerResult = await EndpointAuth.AuthorizeCharacterOwnerAsync(httpContext, characterId, db, auth, cancellationToken);
        if (ownerResult is not null)
        {
            return ownerResult;
        }
        var removed = await inventoryService.RemoveItemAsync(characterId, inventoryItemId, cancellationToken);
        return removed ? Results.NoContent() : Results.NotFound();
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

app.MapGet(
    "/characters/{characterId:guid}/compute/derived-stats",
    async (Guid characterId, ICharacterComputationService computations, CancellationToken cancellationToken) =>
    {
        var result = await computations.GetDerivedStatsAsync(characterId, cancellationToken);
        return result.Errors.Count > 0
            ? Results.BadRequest(new { errors = result.Errors })
            : Results.Ok(result.Result);
    });

app.MapGet(
    "/characters/{characterId:guid}/compute/advanced-rules",
    async (Guid characterId, HttpContext httpContext, AppDbContext db, ILocalAuthService auth, ICharacterComputationService computations, CancellationToken cancellationToken) =>
    {
        var ownerResult = await EndpointAuth.AuthorizeCharacterOwnerAsync(httpContext, characterId, db, auth, cancellationToken);
        if (ownerResult is not null)
        {
            return ownerResult;
        }

        var result = await computations.GetAdvancedRulesSnapshotAsync(characterId, cancellationToken);
        return result.Errors.Count > 0
            ? Results.BadRequest(new { errors = result.Errors })
            : Results.Ok(result.Result);
    });

app.MapPost(
    "/characters/{characterId:guid}/compute/check/persisted",
    async (Guid characterId, PersistedComputeCheckRequest request, HttpContext httpContext, AppDbContext db, ILocalAuthService auth, ICharacterComputationService computations, CancellationToken cancellationToken) =>
    {
        var ownerResult = await EndpointAuth.AuthorizeCharacterOwnerAsync(httpContext, characterId, db, auth, cancellationToken);
        if (ownerResult is not null)
        {
            return ownerResult;
        }
        var result = await computations.ComputeCheckAsync(characterId, request, cancellationToken);
        return result.Errors.Count > 0
            ? Results.BadRequest(new { errors = result.Errors })
            : Results.Ok(new { characterId, result = result.Result });
    });

app.MapPost(
    "/characters/{characterId:guid}/compute/save/persisted",
    async (Guid characterId, PersistedComputeSaveRequest request, HttpContext httpContext, AppDbContext db, ILocalAuthService auth, ICharacterComputationService computations, CancellationToken cancellationToken) =>
    {
        var ownerResult = await EndpointAuth.AuthorizeCharacterOwnerAsync(httpContext, characterId, db, auth, cancellationToken);
        if (ownerResult is not null)
        {
            return ownerResult;
        }
        var result = await computations.ComputeSaveAsync(characterId, request, cancellationToken);
        return result.Errors.Count > 0
            ? Results.BadRequest(new { errors = result.Errors })
            : Results.Ok(new { characterId, result = result.Result });
    });

app.MapPost(
    "/characters/{characterId:guid}/compute/attack/persisted",
    async (Guid characterId, PersistedComputeAttackRequest request, HttpContext httpContext, AppDbContext db, ILocalAuthService auth, ICharacterComputationService computations, CancellationToken cancellationToken) =>
    {
        var ownerResult = await EndpointAuth.AuthorizeCharacterOwnerAsync(httpContext, characterId, db, auth, cancellationToken);
        if (ownerResult is not null)
        {
            return ownerResult;
        }
        var result = await computations.ComputeAttackAsync(characterId, request, cancellationToken);
        return result.Errors.Count > 0
            ? Results.BadRequest(new { errors = result.Errors })
            : Results.Ok(new { characterId, result = result.Result });
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
    async (StartCharacterWizardRequest request, ILocalAuthService auth, ICharacterWizardService wizardService, CancellationToken cancellationToken) =>
    {
        var session = await auth.GetSessionAsync(request.SessionToken, cancellationToken);
        if (session is null)
        {
            return Results.BadRequest(new { errors = new[] { "Session token is invalid or expired." } });
        }

        var result = await wizardService.StartDraftAsync(request with { SessionToken = session.UserId }, cancellationToken);
        return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
    });

app.MapGet(
    "/wizard/characters/{characterId:guid}",
    async (Guid characterId, ICharacterWizardService wizardService, CancellationToken cancellationToken) =>
    {
        var result = await wizardService.GetDraftAsync(characterId, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    });

app.MapGet(
    "/characters",
    async (bool? includeArchived, bool? archivedOnly, bool? mine, HttpContext httpContext, ILocalAuthService auth, ICharacterWizardService wizardService, CancellationToken cancellationToken) =>
    {
        var includeArchivedFlag = includeArchived ?? false;
        var archivedOnlyFlag = archivedOnly ?? false;
        var mineFlag = mine ?? false;
        string? ownerUserId = null;
        if (mineFlag)
        {
            var session = await EndpointAuth.RequireSessionAsync(httpContext, auth, cancellationToken);
            if (session is null)
            {
                return Results.Unauthorized();
            }

            ownerUserId = session.UserId;
        }

        var result = await wizardService.ListCharactersAsync(includeArchivedFlag, archivedOnlyFlag, ownerUserId, cancellationToken);
        return Results.Ok(result);
    });

app.MapGet(
    "/characters/{characterId:guid}",
    async (Guid characterId, ICharacterWizardService wizardService, CancellationToken cancellationToken) =>
    {
        var result = await wizardService.GetCharacterAsync(characterId, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    });

app.MapPatch(
    "/characters/{characterId:guid}",
    async (Guid characterId, UpdateCharacterRequest request, ICharacterWizardService wizardService, CancellationToken cancellationToken) =>
    {
        var result = await wizardService.UpdateCharacterAsync(characterId, request, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    });

app.MapPost(
    "/characters/{characterId:guid}/archive",
    async (Guid characterId, ICharacterWizardService wizardService, CancellationToken cancellationToken) =>
    {
        var result = await wizardService.ArchiveCharacterAsync(characterId, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    });

app.MapPost(
    "/characters/{characterId:guid}/restore",
    async (Guid characterId, ICharacterWizardService wizardService, CancellationToken cancellationToken) =>
    {
        var result = await wizardService.RestoreCharacterAsync(characterId, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    });

app.MapDelete(
    "/characters/{characterId:guid}",
    async (Guid characterId, ICharacterWizardService wizardService, CancellationToken cancellationToken) =>
    {
        var deleted = await wizardService.DeleteCharacterAsync(characterId, cancellationToken);
        return deleted ? Results.NoContent() : Results.NotFound();
    });

app.MapPost(
    "/characters/{characterId:guid}/duplicate",
    async (Guid characterId, DuplicateCharacterRequest request, ICharacterWizardService wizardService, CancellationToken cancellationToken) =>
    {
        var result = await wizardService.DuplicateCharacterAsync(characterId, request, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    });

app.MapPost(
    "/characters/reorder",
    async (ReorderCharactersRequest request, AppDbContext db, CancellationToken cancellationToken) =>
    {
        // Reorder characters based on the provided ID order
        // This persists the user's custom sort order from drag-and-drop
        var characters = await db.CharacterRecords
            .Where(c => request.CharacterIds.Contains(c.CharacterId))
            .ToListAsync(cancellationToken);

        if (characters.Count != request.CharacterIds.Count)
        {
            return Results.BadRequest(new { error = "One or more character IDs not found" });
        }

        // Update DisplayOrder based on position in the provided array
        for (int i = 0; i < request.CharacterIds.Count; i++)
        {
            var character = characters.FirstOrDefault(c => c.CharacterId == request.CharacterIds[i]);
            if (character != null)
            {
                character.DisplayOrder = i;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    });

app.MapGet(
    "/characters/{characterId:guid}/history",
    async (Guid characterId, ICharacterWizardService wizardService, CancellationToken cancellationToken) =>
    {
        var result = await wizardService.GetCharacterHistoryAsync(characterId, cancellationToken);
        return Results.Ok(result);
    });

app.MapPost(
    "/wizard/characters/{characterId:guid}/steps",
    async (Guid characterId, SubmitWizardStepRequest request, ICharacterWizardService wizardService, CancellationToken cancellationToken) =>
    {
        var result = await wizardService.SubmitStepAsync(characterId, request, cancellationToken);
        return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
    });

app.MapPost(
    "/wizard/characters/{characterId:guid}/finalize",
    async (Guid characterId, FinalizeWizardRequest request, ICharacterWizardService wizardService, CancellationToken cancellationToken) =>
    {
        var result = await wizardService.FinalizeAsync(characterId, request, cancellationToken);
        return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
    });

app.MapPost(
    "/characters/{characterId:guid}/copy-to-ruleset",
    async (Guid characterId, CopyCharacterRulesetRequest request, ICharacterWizardService wizardService, CancellationToken cancellationToken) =>
    {
        var result = await wizardService.CopyToRulesetAsync(characterId, request, cancellationToken);
        return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
    });

app.MapPost(
    "/admin/characters/reconcile",
    async (AppDbContext db, CancellationToken cancellationToken) =>
    {
        var now = DateTimeOffset.UtcNow;
        var sheets = await db.CharacterSheets.AsNoTracking().ToArrayAsync(cancellationToken);
        var existingRecords = await db.CharacterRecords.AsNoTracking()
            .Select(x => x.CharacterId)
            .ToHashSetAsync(cancellationToken);
        var existingDrafts = await db.CharacterDrafts.AsNoTracking()
            .Select(x => x.CharacterId)
            .ToHashSetAsync(cancellationToken);

        var createdRecords = 0;
        var createdDrafts = 0;
        foreach (var sheet in sheets)
        {
            if (!existingRecords.Contains(sheet.CharacterId))
            {
                db.CharacterRecords.Add(new CharacterRecordEntity
                {
                    CharacterId = sheet.CharacterId,
                    OwnerUserId = sheet.OwnerUserId,
                    CharacterName = sheet.CharacterName,
                    BaseRuleSystem = sheet.BaseRuleSystem,
                    MixedModeEnabled = false,
                    OverlaySourcesJson = "[]",
                    IsArchived = false,
                    CreatedAtUtc = sheet.CreatedAtUtc,
                    UpdatedAtUtc = sheet.UpdatedAtUtc
                });
                createdRecords++;
            }

            if (!existingDrafts.Contains(sheet.CharacterId))
            {
                db.CharacterDrafts.Add(new CharacterDraftEntity
                {
                    CharacterId = sheet.CharacterId,
                    OwnerUserId = sheet.OwnerUserId,
                    CharacterName = sheet.CharacterName,
                    BaseRuleSystem = sheet.BaseRuleSystem,
                    MixedModeEnabled = false,
                    OverlaySourcesJson = "[]",
                    IsFinalized = true,
                    StepsJson = "[]",
                    WarningsJson = "[]",
                    CreatedAtUtc = sheet.CreatedAtUtc,
                    UpdatedAtUtc = now
                });
                createdDrafts++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { scannedSheets = sheets.Length, createdRecords, createdDrafts });
    });

app.MapGet(
    "/characters/{characterId:guid}/build",
    async (Guid characterId, HttpContext httpContext, AppDbContext db, ILocalAuthService auth, ICharacterBuildService buildService, CancellationToken cancellationToken) =>
    {
        var ownerResult = await EndpointAuth.AuthorizeCharacterOwnerAsync(httpContext, characterId, db, auth, cancellationToken);
        if (ownerResult is not null)
        {
            return ownerResult;
        }
        var build = await buildService.GetBuildAsync(characterId, cancellationToken);
        return build is null ? Results.NotFound() : Results.Ok(build);
    });

app.MapPut(
    "/characters/{characterId:guid}/build",
    async (Guid characterId, UpsertCharacterBuildRequest request, HttpContext httpContext, AppDbContext db, ILocalAuthService auth, ICharacterBuildService buildService, CancellationToken cancellationToken) =>
    {
        var session = await EndpointAuth.RequireSessionAsync(httpContext, auth, cancellationToken);
        if (session is null)
        {
            return Results.Unauthorized();
        }

        var existingOwnerId = await db.CharacterSheets
            .AsNoTracking()
            .Where(x => x.CharacterId == characterId.ToString())
            .Select(x => x.OwnerUserId)
            .SingleOrDefaultAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(existingOwnerId) && !string.Equals(existingOwnerId, session.UserId, StringComparison.Ordinal))
        {
            return Results.Forbid();
        }

        var result = await buildService.UpsertBuildAsync(characterId, session.UserId, request, cancellationToken);
        return result.Errors.Count > 0
            ? Results.BadRequest(new { errors = result.Errors })
            : Results.Ok(result.Data);
    });

app.MapPatch(
    "/characters/{characterId:guid}/build",
    async (Guid characterId, PatchCharacterBuildRequest request, HttpContext httpContext, AppDbContext db, ILocalAuthService auth, ICharacterBuildService buildService, CancellationToken cancellationToken) =>
    {
        var ownerResult = await EndpointAuth.AuthorizeCharacterOwnerAsync(httpContext, characterId, db, auth, cancellationToken);
        if (ownerResult is not null)
        {
            return ownerResult;
        }
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
    async (Guid characterId, HttpContext httpContext, AppDbContext db, ILocalAuthService auth, ICharacterBuildService buildService, CancellationToken cancellationToken) =>
    {
        var ownerResult = await EndpointAuth.AuthorizeCharacterOwnerAsync(httpContext, characterId, db, auth, cancellationToken);
        if (ownerResult is not null)
        {
            return ownerResult;
        }
        var deleted = await buildService.DeleteBuildAsync(characterId, cancellationToken);
        return deleted ? Results.NoContent() : Results.NotFound();
    });

app.MapGet(
    "/characters/{characterId:guid}/spells",
    async (Guid characterId, HttpContext httpContext, AppDbContext db, ILocalAuthService auth, ICharacterProgressionService progression, CancellationToken cancellationToken) =>
    {
        var ownerResult = await EndpointAuth.AuthorizeCharacterOwnerAsync(httpContext, characterId, db, auth, cancellationToken);
        if (ownerResult is not null)
        {
            return ownerResult;
        }
        var spells = await progression.GetSpellsAsync(characterId, cancellationToken);
        return spells is null ? Results.NotFound() : Results.Ok(spells);
    });

app.MapPut(
    "/characters/{characterId:guid}/spells",
    async (Guid characterId, UpsertCharacterSpellsRequest request, HttpContext httpContext, AppDbContext db, ILocalAuthService auth, ICharacterProgressionService progression, CancellationToken cancellationToken) =>
    {
        var ownerResult = await EndpointAuth.AuthorizeCharacterOwnerAsync(httpContext, characterId, db, auth, cancellationToken);
        if (ownerResult is not null)
        {
            return ownerResult;
        }
        return Results.Ok(await progression.UpsertSpellsAsync(characterId, request, cancellationToken));
    });

app.MapGet(
    "/characters/{characterId:guid}/spells/recommended",
    async (Guid characterId, string classModuleId, int classLevel, HttpContext httpContext, AppDbContext db, ILocalAuthService auth, ICharacterProgressionService progression, CancellationToken cancellationToken) =>
    {
        var ownerResult = await EndpointAuth.AuthorizeCharacterOwnerAsync(httpContext, characterId, db, auth, cancellationToken);
        if (ownerResult is not null)
        {
            return ownerResult;
        }

        if (string.IsNullOrWhiteSpace(classModuleId))
        {
            return Results.BadRequest(new { errors = new[] { "classModuleId is required." } });
        }

        var result = await progression.GetRecommendedSpellsAsync(characterId, classModuleId.Trim(), classLevel, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    });

app.MapGet(
    "/characters/{characterId:guid}/resources",
    async (Guid characterId, HttpContext httpContext, AppDbContext db, ILocalAuthService auth, ICharacterProgressionService progression, CancellationToken cancellationToken) =>
    {
        var ownerResult = await EndpointAuth.AuthorizeCharacterOwnerAsync(httpContext, characterId, db, auth, cancellationToken);
        if (ownerResult is not null)
        {
            return ownerResult;
        }
        var resources = await progression.GetResourcesAsync(characterId, cancellationToken);
        return resources is null ? Results.NotFound() : Results.Ok(resources);
    });

app.MapPut(
    "/characters/{characterId:guid}/resources",
    async (Guid characterId, UpsertCharacterResourcesRequest request, HttpContext httpContext, AppDbContext db, ILocalAuthService auth, ICharacterProgressionService progression, CancellationToken cancellationToken) =>
    {
        var ownerResult = await EndpointAuth.AuthorizeCharacterOwnerAsync(httpContext, characterId, db, auth, cancellationToken);
        if (ownerResult is not null)
        {
            return ownerResult;
        }
        return Results.Ok(await progression.UpsertResourcesAsync(characterId, request, cancellationToken));
    });

app.MapGet(
    "/characters/{characterId:guid}/currency",
    async (Guid characterId, HttpContext httpContext, AppDbContext db, ILocalAuthService auth, ICharacterProgressionService progression, CancellationToken cancellationToken) =>
    {
        var ownerResult = await EndpointAuth.AuthorizeCharacterOwnerAsync(httpContext, characterId, db, auth, cancellationToken);
        if (ownerResult is not null)
        {
            return ownerResult;
        }

        var currency = await progression.GetCurrencyAsync(characterId, cancellationToken);
        return currency is null ? Results.NotFound() : Results.Ok(currency);
    });

app.MapPut(
    "/characters/{characterId:guid}/currency",
    async (Guid characterId, UpsertCharacterCurrencyRequest request, HttpContext httpContext, AppDbContext db, ILocalAuthService auth, ICharacterProgressionService progression, CancellationToken cancellationToken) =>
    {
        var ownerResult = await EndpointAuth.AuthorizeCharacterOwnerAsync(httpContext, characterId, db, auth, cancellationToken);
        if (ownerResult is not null)
        {
            return ownerResult;
        }

        return Results.Ok(await progression.UpsertCurrencyAsync(characterId, request, cancellationToken));
    });

app.MapPost(
    "/characters/{characterId:guid}/currency/convert",
    async (Guid characterId, ConvertCurrencyRequest request, HttpContext httpContext, AppDbContext db, ILocalAuthService auth, ICharacterProgressionService progression, CancellationToken cancellationToken) =>
    {
        var ownerResult = await EndpointAuth.AuthorizeCharacterOwnerAsync(httpContext, characterId, db, auth, cancellationToken);
        if (ownerResult is not null)
        {
            return ownerResult;
        }

        var result = await progression.ConvertCurrencyAsync(characterId, request, cancellationToken);
        return result.Errors.Count > 0 ? Results.BadRequest(new { errors = result.Errors }) : Results.Ok(result.Data);
    });

app.MapPost(
    "/characters/{characterId:guid}/currency/consolidate",
    async (Guid characterId, ConsolidateCurrencyRequest request, HttpContext httpContext, AppDbContext db, ILocalAuthService auth, ICharacterProgressionService progression, CancellationToken cancellationToken) =>
    {
        var ownerResult = await EndpointAuth.AuthorizeCharacterOwnerAsync(httpContext, characterId, db, auth, cancellationToken);
        if (ownerResult is not null)
        {
            return ownerResult;
        }

        return Results.Ok(await progression.ConsolidateCurrencyAsync(characterId, request, cancellationToken));
    });

app.MapPost(
    "/characters/{characterId:guid}/currency/purchase",
    async (Guid characterId, PurchaseFromCurrencyRequest request, HttpContext httpContext, AppDbContext db, ILocalAuthService auth, ICharacterProgressionService progression, CancellationToken cancellationToken) =>
    {
        var ownerResult = await EndpointAuth.AuthorizeCharacterOwnerAsync(httpContext, characterId, db, auth, cancellationToken);
        if (ownerResult is not null)
        {
            return ownerResult;
        }

        var result = await progression.PurchaseFromCurrencyAsync(characterId, request, cancellationToken);
        return result.Errors.Count > 0 ? Results.BadRequest(new { errors = result.Errors }) : Results.Ok(result.Data);
    });

app.MapGet(
    "/characters/{characterId:guid}/vitals",
    async (Guid characterId, HttpContext httpContext, AppDbContext db, ILocalAuthService auth, ICharacterProgressionService progression, CancellationToken cancellationToken) =>
    {
        var ownerResult = await EndpointAuth.AuthorizeCharacterOwnerAsync(httpContext, characterId, db, auth, cancellationToken);
        if (ownerResult is not null)
        {
            return ownerResult;
        }
        var vitals = await progression.GetVitalsAsync(characterId, cancellationToken);
        return vitals is null ? Results.NotFound() : Results.Ok(vitals);
    });

app.MapPut(
    "/characters/{characterId:guid}/vitals",
    async (Guid characterId, UpsertCharacterVitalsRequest request, HttpContext httpContext, AppDbContext db, ILocalAuthService auth, ICharacterProgressionService progression, CancellationToken cancellationToken) =>
    {
        var ownerResult = await EndpointAuth.AuthorizeCharacterOwnerAsync(httpContext, characterId, db, auth, cancellationToken);
        if (ownerResult is not null)
        {
            return ownerResult;
        }
        return Results.Ok(await progression.UpsertVitalsAsync(characterId, request, cancellationToken));
    });

app.Run();

public sealed record ClassCatalogItem(
    string ModuleId,
    string ClassName,
    string SourceCode,
    string VersionTag);

public sealed record SubclassCatalogItem(
    string ModuleId,
    string ParentClassId,
    string SubclassName,
    string SourceCode,
    int SubclassFeatureStartLevel);

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
    decimal GoldValue,
    decimal Weight,
    bool IsWeapon,
    string DamageDice,
    string WeaponAbility,
    int AttackBonus,
    int DamageBonus,
    IReadOnlyList<ItemCatalogEffect> Effects);

public sealed record ContentSourceCatalogItem(
    string SourceCode,
    string SourceName,
    RuleSystemMode RuleSystem);

public sealed record ModuleCatalogItem(
    string ModuleId,
    string ModuleType,
    string DisplayName,
    string SourceCode,
    string VersionTag,
    IReadOnlyDictionary<string, int> AbilityBonuses,
    IReadOnlyList<string> FixedSkillProficiencies,
    IReadOnlyList<string> SkillChoices,
    int SkillChoiceCount,
    int ExpertiseChoiceCount,
    IReadOnlyList<string> FixedToolProficiencies,
    IReadOnlyList<string> ToolChoices,
    int ToolChoiceCount,
    IReadOnlyList<string> FixedLanguages,
    IReadOnlyList<string> LanguageChoices,
    int LanguageChoiceCount,
    int MinLevelRequirement,
    IReadOnlyDictionary<string, int> AbilityScoreRequirements,
    IReadOnlyList<string> SpellClasses,
    int? WalkingSpeed,
    string? ParentClassModuleId,
    int? SubclassFeatureStartLevel);

public static class CatalogParsing
{
    private static JsonElement? ParseRoot(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return null;
        }

        using var document = JsonDocument.Parse(payloadJson);
        return document.RootElement.Clone();
    }

    public static IReadOnlyDictionary<string, int> ParseAbilityBonuses(string? payloadJson)
    {
        var root = ParseRoot(payloadJson);
        if (root is null)
        {
            return new Dictionary<string, int>();
        }

        if (!root.Value.TryGetProperty("abilityBonuses", out var bonusesElement) || bonusesElement.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, int>();
        }

        var bonuses = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in bonusesElement.EnumerateObject())
        {
            if (prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetInt32(out var value))
            {
                bonuses[prop.Name] = value;
            }
        }

        return bonuses;
    }

    public static IReadOnlyList<string> ParseStringArray(string? payloadJson, string propertyName)
    {
        var root = ParseRoot(payloadJson);
        if (root is null)
        {
            return Array.Empty<string>();
        }

        if (!root.Value.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return value.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static int ParseInt(string? payloadJson, string propertyName)
    {
        var root = ParseRoot(payloadJson);
        if (root is null)
        {
            return 0;
        }

        if (!root.Value.TryGetProperty(propertyName, out var value))
        {
            return 0;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var n) => n,
            JsonValueKind.String when int.TryParse(value.GetString(), out var n) => n,
            _ => 0
        };
    }

    public static int? ParseWalkingSpeed(string? payloadJson)
    {
        var root = ParseRoot(payloadJson);
        if (root is null || root.Value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        static int? ReadInt(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Number when element.TryGetInt32(out var n) => n,
                JsonValueKind.String when int.TryParse(element.GetString(), out var n) => n,
                _ => null
            };
        }

        if (root.Value.TryGetProperty("walkingSpeed", out var walkingSpeed))
        {
            return ReadInt(walkingSpeed);
        }
        if (root.Value.TryGetProperty("baseMoveSpeed", out var baseMoveSpeed))
        {
            return ReadInt(baseMoveSpeed);
        }
        if (root.Value.TryGetProperty("speed", out var speed))
        {
            return ReadInt(speed);
        }

        return null;
    }

    public static int ParseMinLevelRequirement(IEnumerable<string?> predicateJsonValues)
    {
        var maxRequired = 0;
        foreach (var predicateJson in predicateJsonValues)
        {
            var root = ParseRoot(predicateJson);
            if (root is null || root.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (root.Value.TryGetProperty("minLevel", out var minLevelElement) &&
                minLevelElement.ValueKind == JsonValueKind.Number &&
                minLevelElement.TryGetInt32(out var minLevel))
            {
                maxRequired = Math.Max(maxRequired, minLevel);
            }
        }

        return maxRequired;
    }

    public static IReadOnlyDictionary<string, int> ParseAbilityRequirements(IEnumerable<string?> predicateJsonValues)
    {
        var requirements = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var predicateJson in predicateJsonValues)
        {
            var root = ParseRoot(predicateJson);
            if (root is null || root.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (root.Value.TryGetProperty("ability", out var abilityElement) &&
                root.Value.TryGetProperty("minScore", out var minScoreElement) &&
                abilityElement.ValueKind == JsonValueKind.String)
            {
                var abilityName = abilityElement.GetString();
                if (!string.IsNullOrWhiteSpace(abilityName))
                {
                    var required = minScoreElement.ValueKind == JsonValueKind.Number && minScoreElement.TryGetInt32(out var score)
                        ? score
                        : 0;
                    if (required > 0)
                    {
                        requirements[abilityName.Trim()] = Math.Max(requirements.GetValueOrDefault(abilityName.Trim(), 0), required);
                    }
                }
            }

            if (root.Value.TryGetProperty("abilities", out var abilitiesElement) &&
                abilitiesElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var ability in abilitiesElement.EnumerateObject())
                {
                    if (ability.Value.ValueKind != JsonValueKind.Number || !ability.Value.TryGetInt32(out var requiredScore))
                    {
                        continue;
                    }

                    if (requiredScore > 0)
                    {
                        requirements[ability.Name] = Math.Max(requirements.GetValueOrDefault(ability.Name, 0), requiredScore);
                    }
                }
            }
        }

        return requirements;
    }
}

public static class EndpointAuth
{
    public static string? ResolveSessionToken(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue("X-Session-Token", out var headerToken) &&
            !string.IsNullOrWhiteSpace(headerToken))
        {
            return headerToken.ToString();
        }

        if (httpContext.Request.Query.TryGetValue("sessionToken", out var queryToken) &&
            !string.IsNullOrWhiteSpace(queryToken))
        {
            return queryToken.ToString();
        }

        return null;
    }

    public static async Task<LocalSession?> RequireSessionAsync(
        HttpContext httpContext,
        ILocalAuthService auth,
        CancellationToken cancellationToken)
    {
        var sessionToken = ResolveSessionToken(httpContext);
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return null;
        }

        return await auth.GetSessionAsync(sessionToken, cancellationToken);
    }

    public static async Task<IResult?> AuthorizeCharacterOwnerAsync(
        HttpContext httpContext,
        Guid characterId,
        AppDbContext db,
        ILocalAuthService auth,
        CancellationToken cancellationToken)
    {
        var session = await RequireSessionAsync(httpContext, auth, cancellationToken);
        if (session is null)
        {
            return Results.Unauthorized();
        }

        var ownerUserId = await db.CharacterSheets
            .AsNoTracking()
            .Where(x => x.CharacterId == characterId.ToString())
            .Select(x => x.OwnerUserId)
            .SingleOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(ownerUserId))
        {
            ownerUserId = await db.CharacterRecords
                .AsNoTracking()
                .Where(x => x.CharacterId == characterId.ToString())
                .Select(x => x.OwnerUserId)
                .SingleOrDefaultAsync(cancellationToken);
        }
        if (string.IsNullOrWhiteSpace(ownerUserId))
        {
            ownerUserId = await db.CharacterDrafts
                .AsNoTracking()
                .Where(x => x.CharacterId == characterId.ToString())
                .Select(x => x.OwnerUserId)
                .SingleOrDefaultAsync(cancellationToken);
        }
        if (string.IsNullOrWhiteSpace(ownerUserId))
        {
            return Results.NotFound();
        }

        return string.Equals(ownerUserId, session.UserId, StringComparison.Ordinal)
            ? null
            : Results.Forbid();
    }
}
