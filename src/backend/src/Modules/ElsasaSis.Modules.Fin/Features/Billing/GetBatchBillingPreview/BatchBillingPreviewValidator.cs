using System.Text.RegularExpressions;
using FluentValidation;

namespace ElsasaSis.Modules.Fin.Features.Billing.GetBatchBillingPreview;

public sealed partial class BatchBillingPreviewValidator : AbstractValidator<BatchBillingPreviewRequest>
{
    private static readonly Regex BillingMonthRegex = new(@"^\d{4}-(0[1-9]|1[0-2])$", RegexOptions.Compiled);

    public BatchBillingPreviewValidator()
    {
        RuleFor(x => x.AcademicYearId)
            .NotEmpty()
            .WithMessage("Academic year is required.");

        RuleFor(x => x.BillingMonth)
            .NotEmpty()
            .WithMessage("Billing month is required.")
            .Matches(BillingMonthRegex)
            .WithMessage("Billing month must be in YYYY-MM format.");
    }
}
