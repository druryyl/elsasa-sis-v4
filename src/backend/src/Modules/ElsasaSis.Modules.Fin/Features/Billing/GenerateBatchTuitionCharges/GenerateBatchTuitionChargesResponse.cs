namespace ElsasaSis.Modules.Fin.Features.Billing.GenerateBatchTuitionCharges;

public sealed record GenerateBatchTuitionChargesResponse(
    int GeneratedCount,
    int SkippedCount);
