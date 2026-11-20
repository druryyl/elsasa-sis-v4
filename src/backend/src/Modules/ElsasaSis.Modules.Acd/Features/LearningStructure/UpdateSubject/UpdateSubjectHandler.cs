using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Acd.Features.LearningStructure.UpdateSubject;

public sealed class UpdateSubjectHandler(ISubjectRepository repository)
{
    public async Task<Result<SubjectDto>> HandleAsync(
        Guid id,
        UpdateSubjectRequest request,
        CancellationToken cancellationToken = default)
    {
        var subject = await repository.GetByIdAsync(id, cancellationToken);
        if (subject is null)
        {
            return Result<SubjectDto>.Failure(LearningStructureErrors.SubjectNotFound(id));
        }

        var trimmedName = request.Name?.Trim() ?? string.Empty;
        if (await repository.ExistsByNameAsync(trimmedName, excludeId: id, cancellationToken))
        {
            return Result<SubjectDto>.Failure(LearningStructureErrors.DuplicateSubjectName(trimmedName));
        }

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var trimmedCode = request.Code.Trim();
            if (await repository.ExistsByCodeAsync(trimmedCode, excludeId: id, cancellationToken))
            {
                return Result<SubjectDto>.Failure(LearningStructureErrors.DuplicateSubjectCode(trimmedCode));
            }
        }

        var updateResult = subject.Update(request.Name, request.Code);
        if (updateResult.IsFailure)
        {
            return Result<SubjectDto>.Failure(updateResult.Error);
        }

        var saveResult = await repository.UpdateAsync(subject, cancellationToken);
        if (saveResult.IsFailure)
        {
            return Result<SubjectDto>.Failure(saveResult.Error);
        }

        return Result<SubjectDto>.Success(subject.ToDto());
    }
}
