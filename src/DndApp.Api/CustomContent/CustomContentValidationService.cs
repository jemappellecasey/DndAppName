namespace DndApp.Api.CustomContent;

public interface ICustomContentValidationService
{
    ValidationResult ValidateOrigin(CreateCustomOriginRequest request);
    ValidationResult ValidateSpecies(CreateCustomSpeciesRequest request);
}

public sealed class CustomContentValidationService : ICustomContentValidationService
{
    public ValidationResult ValidateOrigin(CreateCustomOriginRequest request)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add("Origin name is required.");
        }

        var abilityTotal = request.AbilityBonuses.Sum(x => x.Bonus);
        if (request.Mode == CustomContentMode.GuidedCustom)
        {
            if (request.AbilityBonuses.Count is < 2 or > 3)
            {
                errors.Add("Guided origin must define 2-3 ability bonus entries.");
            }

            if (abilityTotal is < 2 or > 3)
            {
                errors.Add("Guided origin total ability bonuses must add up to 2 or 3.");
            }

            if (request.SkillProficiencies.Count != 2)
            {
                errors.Add("Guided origin must include exactly 2 skill proficiencies.");
            }

            if (request.FeatureNotes.Count == 0)
            {
                errors.Add("Guided origin must include at least one feature note.");
            }
        }
        else
        {
            if (request.SkillProficiencies.Count == 0)
            {
                warnings.Add("Fully custom origin has no skill proficiencies.");
            }

            if (abilityTotal == 0)
            {
                warnings.Add("Fully custom origin has no ability bonus entries.");
            }
        }

        return new ValidationResult(errors.Count == 0, errors, warnings);
    }

    public ValidationResult ValidateSpecies(CreateCustomSpeciesRequest request)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add("Species name is required.");
        }

        if (request.Mode == CustomContentMode.GuidedCustom)
        {
            if (request.WalkingSpeed is < 25 or > 40)
            {
                errors.Add("Guided species walking speed must be between 25 and 40.");
            }

            if (request.Traits.Count is < 1 or > 6)
            {
                errors.Add("Guided species must include 1-6 traits.");
            }

            if (string.IsNullOrWhiteSpace(request.Size))
            {
                errors.Add("Guided species size is required.");
            }

            if (request.Languages.Count < 1)
            {
                errors.Add("Guided species must include at least one language.");
            }
        }
        else
        {
            if (request.Traits.Count == 0)
            {
                warnings.Add("Fully custom species has no traits.");
            }

            if (request.WalkingSpeed <= 0)
            {
                warnings.Add("Fully custom species walking speed is non-positive.");
            }
        }

        return new ValidationResult(errors.Count == 0, errors, warnings);
    }
}
