using ElsasaSis.Modules.Std.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;

namespace ElsasaSis.Modules.Std.Features.Admissions.RecordAdmissionDecision;

public sealed class RecordAdmissionDecisionHandler(
    ICandidateRepository candidateRepository,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<Result<CandidateDto>> HandleAsync(
        Guid id,
        RecordAdmissionDecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        var candidate = await candidateRepository.GetByIdAsync(id, cancellationToken);
        if (candidate is null)
        {
            return Result<CandidateDto>.Failure(StdErrors.CandidateNotFound(id));
        }

        var normalizedDecision = request.Decision?.Trim().ToUpperInvariant();
        var decisionResult = normalizedDecision switch
        {
            "ACCEPTED" => candidate.Accept(request.DecisionNotes),
            "REJECTED" => candidate.Reject(request.DecisionNotes),
            _ => Result.Failure(DomainError.Validation(
                "Std.Candidate.DecisionInvalid",
                "Decision must be ACCEPTED or REJECTED.",
                "decision"))
        };

        if (decisionResult.IsFailure)
        {
            return Result<CandidateDto>.Failure(decisionResult.Error);
        }

        await candidateRepository.UpdateAsync(candidate, cancellationToken);

        if (candidate.DomainEvents.Count > 0)
        {
            await domainEventDispatcher.DispatchAsync(candidate.DomainEvents, cancellationToken);
            candidate.ClearDomainEvents();
        }

        return Result<CandidateDto>.Success(candidate.ToDto());
    }
}
