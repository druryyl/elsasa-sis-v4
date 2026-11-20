using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Acd.Features.LearningStructure.MapSubjectStageGrade;

public sealed class MapSubjectStageGradeHandler(ISubjectStageRepository stageRepository)
{
    public async Task<Result<SubjectStageGradeDto>> HandleAsync(
        Guid subjectId,
        Guid stageId,
        MapSubjectStageGradeRequest request,
        CancellationToken cancellationToken = default)
    {
        var stage = await stageRepository.GetByIdAsync(stageId, cancellationToken);
        if (stage is null || stage.SubjectId != subjectId)
        {
            return Result<SubjectStageGradeDto>.Failure(LearningStructureErrors.SubjectStageNotFound(stageId));
        }

        var addResult = stage.AddGradeMapping(request.SchoolLevel, request.Grade);
        if (addResult.IsFailure)
        {
            return Result<SubjectStageGradeDto>.Failure(addResult.Error);
        }

        var saveResult = await stageRepository.UpdateAsync(stage, cancellationToken);
        if (saveResult.IsFailure)
        {
            return Result<SubjectStageGradeDto>.Failure(saveResult.Error);
        }

        return Result<SubjectStageGradeDto>.Success(addResult.Value!.ToDto());
    }
}
