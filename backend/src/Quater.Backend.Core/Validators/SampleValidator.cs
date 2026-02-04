using FluentValidation;
using Quater.Shared.Models;

namespace Quater.Backend.Core.Validators;

public class SampleValidator : AbstractValidator<Sample>
{
    public SampleValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.CollectorName)
            .NotEmpty().WithMessage("Collector name is required")
            .MaximumLength(100).WithMessage("Collector name must not exceed 100 characters");

        RuleFor(x => x.Location)
            .NotNull().WithMessage("Location is required");

        RuleFor(x => x.Location.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90")
            .When(x => x.Location != null);

        RuleFor(x => x.Location.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180")
            .When(x => x.Location != null);

        RuleFor(x => x.Location.Description)
            .MaximumLength(200).WithMessage("Location description must not exceed 200 characters")
            .When(x => x.Location != null);

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters");

        RuleFor(x => x.CollectionDate)
            .LessThanOrEqualTo(x => timeProvider.GetUtcNow().DateTime).WithMessage("Collection date cannot be in the future");

        RuleFor(x => x.LabId)
            .NotEmpty().WithMessage("Lab ID is required");
    }
}
