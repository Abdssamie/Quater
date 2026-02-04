using FluentValidation;
using Quater.Backend.Core.DTOs;

namespace Quater.Backend.Core.Validators;

/// <summary>
/// Validator for CreateSampleDto
/// </summary>
public class CreateSampleDtoValidator : AbstractValidator<CreateSampleDto>
{
    public CreateSampleDtoValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.CollectorName)
            .NotEmpty().WithMessage("Collector name is required")
            .MaximumLength(100).WithMessage("Collector name must not exceed 100 characters");

        RuleFor(x => x.LocationLatitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90");

        RuleFor(x => x.LocationLongitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180");

        RuleFor(x => x.LocationDescription)
            .MaximumLength(200).WithMessage("Location description must not exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.LocationDescription));

        RuleFor(x => x.LocationHierarchy)
            .MaximumLength(500).WithMessage("Location hierarchy must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.LocationHierarchy));

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        RuleFor(x => x.CollectionDate)
            .LessThanOrEqualTo(x => timeProvider.GetUtcNow().DateTime)
            .WithMessage("Collection date cannot be in the future");

        RuleFor(x => x.LabId)
            .NotEmpty().WithMessage("Lab ID is required");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid sample type");
    }
}
