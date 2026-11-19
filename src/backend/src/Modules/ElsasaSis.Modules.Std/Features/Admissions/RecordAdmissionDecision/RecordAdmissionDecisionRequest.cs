namespace ElsasaSis.Modules.Std.Features.Admissions.RecordAdmissionDecision;

public sealed record RecordAdmissionDecisionRequest(
    string Decision,
    string? DecisionNotes = null);
