using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.Modules.Acd.Domain.Model;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Acd.Features.LearningStructure.CreateSubjectStage;

public sealed class CreateSubjectStageHandler(
    ISubjectRepository subjectRepository,
    ISubjectStageRepository stageRepository)
{
    public async Task<Result<SubjectStageDto>> HandleAsync(
        Guid subjectId,
        CreateSubjectStageRequest request,
        CancellationToken cancellationToken = default)
    {
        var subject = await subjectRepository.GetByIdAsync(subjectId, cancellationToken);
        if (subject is null)
        {
            return Result<SubjectStageDto>.Failure(LearningStructureErrors.SubjectNotFound(subjectId));
        }

        var trimmedName = request.Name?.Trim() ?? string.Empty;
        if (await stageRepository.ExistsByNameAsync(subjectId, trimmedName, excludeId: null, cancellationToken))
        {
            return Result<SubjectStageDto>.Failure(LearningStructureErrors.DuplicateStageName(trimmedName));
        }

        if (await stageRepository.ExistsBySequenceOrderAsync(subjectId, request.SequenceOrder, excludeId: null, cancellationToken))
        {
            return Result<SubjectStageDto>.Failure(LearningStructureErrors.DuplicateStageSequence(request.SequenceOrder));
        }

        var createResult = SubjectStage.Create(
            subject.Id,
            subject.Name,
            request.Name,
            request.SequenceOrder,
            request.Description);

        if (createResult.IsFailure)
        {
            return Result<SubjectStageDto>.Failure(createResult.Error);
        }

        var stage = createResult.Value!;
        var addResult = await stageRepository.AddAsync(stage, cancellationToken);
        if (addResult.IsFailure)
        {
            return Result<SubjectStageDto>.Failure(addResult.Error);
        }

        return Result<SubjectStageDto>.Success(stage.ToDto());
    }
}
