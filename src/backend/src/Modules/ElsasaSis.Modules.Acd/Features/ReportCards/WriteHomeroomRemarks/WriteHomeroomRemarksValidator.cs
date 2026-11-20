using FluentValidation;

namespace ElsasaSis.Modules.Acd.Features.ReportCards.WriteHomeroomRemarks;

public sealed class WriteHomeroomRemarksValidator : AbstractValidator<WriteHomeroomRemarksRequest>
{
    public WriteHomeroomRemarksValidator()
    {
        RuleFor(x => x.Remarks)
            .MaximumLength(1000)
            .WithMessage("Homeroom remarks cannot exceed 1000 characters.");
    }
}
