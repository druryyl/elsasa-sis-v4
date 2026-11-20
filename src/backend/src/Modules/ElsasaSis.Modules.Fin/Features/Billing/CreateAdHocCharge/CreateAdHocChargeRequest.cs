namespace ElsasaSis.Modules.Fin.Features.Billing.CreateAdHocCharge;

public sealed record CreateAdHocChargeRequest(
    Guid AcademicYearId,
    Guid StudentId,
    Guid ChargeTypeId,
    string Description,
    decimal UnitAmount,
    int Quantity = 1,
    DateOnly? DueDate = null,
    string? BillingMonth = null);
