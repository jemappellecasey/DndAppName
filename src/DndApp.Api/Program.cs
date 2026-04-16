using DndApp.Api.CustomContent;
using DndApp.Api.Items;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ICustomContentValidationService, CustomContentValidationService>();
builder.Services.AddSingleton<IItemEffectPipelineService, ItemEffectPipelineService>();

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

app.Run();
