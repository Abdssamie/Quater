using FluentValidation;
using Quater.Backend.Core.Models;

namespace Quater.Backend.Core.Validators;

public class TestResultValidator : AbstractValidator<TestResult>
{
    public TestResultValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.SampleId)
            .NotEmpty().WithMessage("Sample ID is required");

        RuleFor(x => x.ParameterName)
            .NotEmpty().WithMessage("Parameter name is required")
            .MaximumLength(100).WithMessage("Parameter name must not exceed 100 characters");

        RuleFor(x => x.Value)
            .NotNull().WithMessage("Value is required");

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("Unit is required")
            .MaximumLength(20).WithMessage("Unit must not exceed 20 characters");

        RuleFor(x => x.TestDate)
            .LessThanOrEqualTo(x => timeProvider.GetUtcNow().DateTime).WithMessage("Test date cannot be in the future");

        RuleFor(x => x.TechnicianName)
            .NotEmpty().WithMessage("Technician name is required")
            .MaximumLength(100).WithMessage("Technician name must not exceed 100 characters");
            
        RuleFor(x => x.TestMethod)
            .IsInEnum().WithMessage("Invalid test method");
            
        RuleFor(x => x.ComplianceStatus)
            .IsInEnum().WithMessage("Invalid compliance status");
    }
}
