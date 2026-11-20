using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Acd.Features.LearningStructure.CreateLearningUnit;

public sealed class CreateLearningUnitHandler(ISubjectStageRepository stageRepository)
{
    public async Task<Result<LearningUnitDto>> HandleAsync(
        Guid subjectId,
        Guid stageId,
        CreateLearningUnitRequest request,
        CancellationToken cancellationToken = default)
    {
        var stage = await stageRepository.GetByIdAsync(stageId, cancellationToken);
        if (stage is null || stage.SubjectId != subjectId)
        {
            return Result<LearningUnitDto>.Failure(LearningStructureErrors.SubjectStageNotFound(stageId));
        }

        var addResult = stage.AddLearningUnit(
            request.Name,
            request.SequenceOrder,
            request.UnitType,
            request.Description);

        if (addResult.IsFailure)
        {
            return Result<LearningUnitDto>.Failure(addResult.Error);
        }

        var saveResult = await stageRepository.UpdateAsync(stage, cancellationToken);
        if (saveResult.IsFailure)
        {
            return Result<LearningUnitDto>.Failure(saveResult.Error);
        }

        return Result<LearningUnitDto>.Success(addResult.Value!.ToDto());
    }
}
