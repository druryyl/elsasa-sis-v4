using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Opr.Features.Timetables.ManageSmkBlockRotations;

public sealed class DeleteSmkBlockHandler(IStudyGroupTimetableRepository timetableRepository)
{
    public async Task<Result> HandleAsync(
        Guid studyGroupId,
        Guid blockId,
        CancellationToken cancellationToken = default)
    {
        var block = await timetableRepository.GetByIdAsync(blockId, cancellationToken);
        if (block is null || block.StudyGroupId != studyGroupId)
        {
            return Result.Failure(TimetableErrors.BlockNotFound(blockId));
        }

        await timetableRepository.DeleteAsync(blockId, cancellationToken);
        return Result.Success();
    }
}
