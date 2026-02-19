using FluentValidation;
using Quater.Backend.Core.DTOs;

namespace Quater.Backend.Core.Validators;

/// <summary>
/// Validator for UpdateParameterDto
/// </summary>
public class UpdateParameterDtoValidator : AbstractValidator<UpdateParameterDto>
{
    public UpdateParameterDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Parameter name is required")
            .MaximumLength(100).WithMessage("Parameter name must not exceed 100 characters");

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("Unit is required")
            .MaximumLength(20).WithMessage("Unit must not exceed 20 characters");

        RuleFor(x => x.WhoThreshold)
            .GreaterThanOrEqualTo(0).WithMessage("WHO threshold must be non-negative")
            .When(x => x.WhoThreshold.HasValue);

        RuleFor(x => x.MinValue)
            .LessThan(x => x.MaxValue).WithMessage("Minimum value must be less than maximum value")
            .When(x => x.MinValue.HasValue && x.MaxValue.HasValue);

        RuleFor(x => x.MaxValue)
            .GreaterThan(x => x.MinValue).WithMessage("Maximum value must be greater than minimum value")
            .When(x => x.MinValue.HasValue && x.MaxValue.HasValue);

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
