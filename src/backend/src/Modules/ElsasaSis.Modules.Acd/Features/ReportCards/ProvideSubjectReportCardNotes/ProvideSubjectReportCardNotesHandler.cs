using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.Modules.Acd.Domain.ReportCards;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.Modules.Acd.Features.ReportCards.ProvideSubjectReportCardNotes;

public sealed class ProvideSubjectReportCardNotesHandler(
    IReportCardRepository reportCardRepository,
    ICurrentUser currentUser)
{
    public async Task<Result> HandleAsync(
        Guid reportCardId,
        ProvideSubjectNotesRequest request,
        CancellationToken cancellationToken = default)
    {
        var card = await reportCardRepository.GetByIdAsync(reportCardId, cancellationToken);
        if (card is null)
        {
            return Result.Failure(AcdErrors.ReportCards.ReportCardNotFound(reportCardId));
        }

        if (card.Status != ReportCardStatus.Draft)
        {
            return Result.Failure(AcdErrors.ReportCards.AlreadyFinalized(card.Id));
        }

        var entries = await reportCardRepository.GetEntriesByReportCardIdAsync(card.Id, cancellationToken);
        var entry = entries.FirstOrDefault(e => e.SubjectStageId == request.SubjectStageId);
        if (entry is null)
        {
            return Result.Failure(AcdErrors.ReportCards.EntryNotFound());
        }

        Guid teacherId;
        string teacherName;

        if (currentUser.TeacherId.HasValue && currentUser.TeacherId.Value != Guid.Empty)
        {
            var isAssigned = await reportCardRepository.HasTeacherAssignmentAsync(
                currentUser.TeacherId.Value,
                card.StudyGroupId ?? Guid.Empty,
                entry.SubjectId,
                card.OperationalPeriodId,
                cancellationToken);

            if (!isAssigned)
            {
                return Result.Failure(AcdErrors.ReportCards.TeacherNotAssignedToSubject(
                    currentUser.TeacherId.Value,
                    request.SubjectStageId));
            }

            teacherId = currentUser.TeacherId.Value;
            teacherName = !string.IsNullOrWhiteSpace(currentUser.FullName)
                ? currentUser.FullName
                : currentUser.Username;
        }
        else
        {
            teacherId = currentUser.UserId;
            teacherName = !string.IsNullOrWhiteSpace(currentUser.FullName)
                ? currentUser.FullName
                : (!string.IsNullOrWhiteSpace(currentUser.Username) ? currentUser.Username : "Teacher");
        }

        var updateResult = entry.UpdateTeacherNotes(teacherId, teacherName, request.TeacherNotes, DateTime.UtcNow);
        if (!updateResult.IsSuccess)
        {
            return updateResult;
        }

        await reportCardRepository.UpdateEntryNotesAsync(entry, cancellationToken);

        return Result.Success();
    }
}
