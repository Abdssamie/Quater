using FluentValidation;
using Quater.Shared.Models;

namespace Quater.Backend.Core.Validators;

public class ParameterValidator : AbstractValidator<Parameter>
{
    public ParameterValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Parameter name is required")
            .MaximumLength(100).WithMessage("Parameter name must not exceed 100 characters");

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("Unit is required")
            .MaximumLength(50).WithMessage("Unit must not exceed 50 characters");

        RuleFor(x => x.MinValue)
            .LessThan(x => x.MaxValue)
            .When(x => x.MinValue.HasValue && x.MaxValue.HasValue)
            .WithMessage("Minimum value must be less than maximum value");

        RuleFor(x => x.Threshold)
            .GreaterThan(0).When(x => x.Threshold.HasValue)
            .WithMessage("WHO threshold must be positive");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");
    }
}
