using FluentValidation;
using Quater.Shared.Models;

namespace Quater.Backend.Core.Validators;

public class LabValidator : AbstractValidator<Lab>
{
    public LabValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Lab name is required")
            .MaximumLength(200).WithMessage("Lab name must not exceed 200 characters");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Lab location is required")
            .MaximumLength(500).WithMessage("Lab location must not exceed 500 characters");

        RuleFor(x => x.ContactInfo)
            .MaximumLength(500).WithMessage("Contact info must not exceed 500 characters");
    }
}
