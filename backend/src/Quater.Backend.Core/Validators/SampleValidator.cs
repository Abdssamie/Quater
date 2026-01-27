using FluentValidation;
using Quater.Backend.Core.Models;

namespace Quater.Backend.Core.Validators;

public class SampleValidator : AbstractValidator<Sample>
{
    public SampleValidator()
    {
        RuleFor(x => x.CollectorName)
            .NotEmpty().WithMessage("Collector name is required")
            .MaximumLength(100).WithMessage("Collector name must not exceed 100 characters");

        RuleFor(x => x.LocationLatitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90");

        RuleFor(x => x.LocationLongitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180");

        RuleFor(x => x.LocationDescription)
            .MaximumLength(200).WithMessage("Location description must not exceed 200 characters");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters");

        RuleFor(x => x.CollectionDate)
            .LessThanOrEqualTo(x => DateTime.UtcNow).WithMessage("Collection date cannot be in the future");
            
        RuleFor(x => x.LabId)
            .NotEmpty().WithMessage("Lab ID is required");
    }
}
