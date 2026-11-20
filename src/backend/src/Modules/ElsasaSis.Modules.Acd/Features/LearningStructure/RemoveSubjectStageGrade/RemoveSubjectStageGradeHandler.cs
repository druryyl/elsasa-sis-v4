using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Acd.Features.LearningStructure.RemoveSubjectStageGrade;

public sealed class RemoveSubjectStageGradeHandler(ISubjectStageRepository stageRepository)
{
    public async Task<Result> HandleAsync(
        Guid subjectId,
        Guid stageId,
        Guid mappingId,
        CancellationToken cancellationToken = default)
    {
        var stage = await stageRepository.GetByIdAsync(stageId, cancellationToken);
        if (stage is null || stage.SubjectId != subjectId)
        {
            return Result.Failure(LearningStructureErrors.SubjectStageNotFound(stageId));
        }

        var removeResult = stage.RemoveGradeMapping(mappingId);
        if (removeResult.IsFailure)
        {
            return removeResult;
        }

        return await stageRepository.UpdateAsync(stage, cancellationToken);
    }
}
