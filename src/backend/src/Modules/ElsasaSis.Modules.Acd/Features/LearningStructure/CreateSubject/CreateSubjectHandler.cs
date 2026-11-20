using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.Modules.Acd.Domain.Model;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Acd.Features.LearningStructure.CreateSubject;

public sealed class CreateSubjectHandler(ISubjectRepository repository)
{
    public async Task<Result<SubjectDto>> HandleAsync(
        CreateSubjectRequest request,
        CancellationToken cancellationToken = default)
    {
        var trimmedName = request.Name?.Trim() ?? string.Empty;
        if (await repository.ExistsByNameAsync(trimmedName, excludeId: null, cancellationToken))
        {
            return Result<SubjectDto>.Failure(LearningStructureErrors.DuplicateSubjectName(trimmedName));
        }

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var trimmedCode = request.Code.Trim();
            if (await repository.ExistsByCodeAsync(trimmedCode, excludeId: null, cancellationToken))
            {
                return Result<SubjectDto>.Failure(LearningStructureErrors.DuplicateSubjectCode(trimmedCode));
            }
        }

        var createResult = Subject.Create(request.Name, request.Code);
        if (createResult.IsFailure)
        {
            return Result<SubjectDto>.Failure(createResult.Error);
        }

        var subject = createResult.Value!;
        var addResult = await repository.AddAsync(subject, cancellationToken);
        if (addResult.IsFailure)
        {
            return Result<SubjectDto>.Failure(addResult.Error);
        }

        return Result<SubjectDto>.Success(subject.ToDto());
    }
}
