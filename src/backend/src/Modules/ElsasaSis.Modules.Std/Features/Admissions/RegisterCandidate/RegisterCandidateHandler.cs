using ElsasaSis.Contracts.Operational;
using ElsasaSis.Modules.Std.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;

namespace ElsasaSis.Modules.Std.Features.Admissions.RegisterCandidate;

public sealed class RegisterCandidateHandler(
    ICandidateRepository candidateRepository,
    IOperationalContract operationalContract,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<Result<CandidateDto>> HandleAsync(
        RegisterCandidateRequest request,
        CancellationToken cancellationToken = default)
    {
        var academicYear = await operationalContract.GetAcademicYearByIdAsync(request.AdmissionYearId, cancellationToken);
        if (academicYear is null)
        {
            return Result<CandidateDto>.Failure(StdErrors.AcademicYearNotFound(request.AdmissionYearId));
        }

        var genderResult = StdDomainParsing.ParseGender(request.Gender);
        if (genderResult.IsFailure)
        {
            return Result<CandidateDto>.Failure(genderResult.Error);
        }

        var levelResult = StdDomainParsing.ParseSchoolLevel(request.TargetSchoolLevel, "targetSchoolLevel");
        if (levelResult.IsFailure)
        {
            return Result<CandidateDto>.Failure(levelResult.Error);
        }

        if (request.Guardians is null || request.Guardians.Count == 0)
        {
            return Result<CandidateDto>.Failure(StdErrors.AtLeastOneGuardianRequired());
        }

        var guardianInputs = new List<GuardianInput>(request.Guardians.Count);
        for (var i = 0; i < request.Guardians.Count; i++)
        {
            var guardian = request.Guardians[i];
            if (guardian is null)
            {
                return Result<CandidateDto>.Failure(StdErrors.GuardianFullNameRequired());
            }

            var relationshipResult = StdDomainParsing.ParseRelationshipType(
                guardian.RelationshipType,
                $"guardians[{i}].relationshipType");
            if (relationshipResult.IsFailure)
            {
                return Result<CandidateDto>.Failure(relationshipResult.Error);
            }

            guardianInputs.Add(new GuardianInput(
                guardian.FullName,
                relationshipResult.Value,
                guardian.Phone,
                guardian.Email,
                guardian.Address));
        }

        var registerResult = Candidate.Register(
            academicYear.Id,
            academicYear.Name,
            request.FullName,
            request.Nickname,
            request.DateOfBirth,
            genderResult.Value,
            request.Phone,
            request.Email,
            request.Address,
            levelResult.Value,
            request.TargetGrade,
            guardianInputs);

        if (registerResult.IsFailure)
        {
            return Result<CandidateDto>.Failure(registerResult.Error);
        }

        var candidate = registerResult.Value!;
        await candidateRepository.AddAsync(candidate, cancellationToken);

        if (candidate.DomainEvents.Count > 0)
        {
            await domainEventDispatcher.DispatchAsync(candidate.DomainEvents, cancellationToken);
            candidate.ClearDomainEvents();
        }

        return Result<CandidateDto>.Success(candidate.ToDto());
    }
}
