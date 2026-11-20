namespace ElsasaSis.Modules.Fin.Features.FeePolicy.GetChargeTypes;

public sealed record GetChargeTypesQuery(
    Guid? AcademicYearId = null,
    string? Frequency = null,
    string? Status = null);
