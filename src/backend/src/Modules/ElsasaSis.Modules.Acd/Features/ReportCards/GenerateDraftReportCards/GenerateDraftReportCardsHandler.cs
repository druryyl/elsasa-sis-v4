using ElsasaSis.Contracts.Operational;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.Modules.Acd.Domain.ReportCards;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Acd.Features.ReportCards.GenerateDraftReportCards;

public sealed class GenerateDraftReportCardsHandler(
    IReportCardRepository reportCardRepository,
    IOperationalContract operationalContract,
    IStudentContract studentContract)
{
    public async Task<Result<GenerateDraftReportCardsResponse>> HandleAsync(
        GenerateDraftReportCardsRequest request,
        CancellationToken cancellationToken = default)
    {
        var period = await operationalContract.GetOperationalPeriodByIdAsync(request.OperationalPeriodId, cancellationToken);
        if (period is null)
        {
            return Result<GenerateDraftReportCardsResponse>.Failure(
                AcdErrors.ReportCards.OperationalPeriodNotFound(request.OperationalPeriodId));
        }

        var studyGroup = await studentContract.GetStudyGroupByIdAsync(request.StudyGroupId, cancellationToken);
        if (studyGroup is null)
        {
            return Result<GenerateDraftReportCardsResponse>.Failure(
                AcdErrors.ReportCards.StudyGroupNotFound(request.StudyGroupId));
        }

        var placedStudents = await studentContract.GetActiveStudentsByScopeAsync(
            request.StudyGroupId,
            null,
            null,
            cancellationToken);

        if (placedStudents.Count == 0)
        {
            return Result<GenerateDraftReportCardsResponse>.Success(
                new GenerateDraftReportCardsResponse(0, 0));
        }

        var allStudentIds = placedStudents.Select(s => s.StudentId).ToList();
        var existingCardStudentIds = await reportCardRepository.GetExistingCardStudentIdsAsync(
            period.Id,
            allStudentIds,
            cancellationToken);

        var studentsToGenerate = placedStudents
            .Where(s => !existingCardStudentIds.Contains(s.StudentId))
            .ToList();

        var skippedCount = existingCardStudentIds.Count;

        if (studentsToGenerate.Count == 0)
        {
            return Result<GenerateDraftReportCardsResponse>.Success(
                new GenerateDraftReportCardsResponse(0, skippedCount));
        }

        var toGenerateIds = studentsToGenerate.Select(s => s.StudentId).ToList();

        var nisnMap = await reportCardRepository.GetStudentNisnsAsync(toGenerateIds, cancellationToken);

        var draftEntries = await reportCardRepository.ComputeDraftCardEntriesAsync(
            studyGroup.Id,
            period.Id,
            studyGroup.SchoolLevel,
            studyGroup.Grade,
            toGenerateIds,
            cancellationToken);

        var entriesByStudent = draftEntries
            .GroupBy(e => e.StudentId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var now = DateTime.UtcNow;
        var batchToInsert = new List<(ReportCard ReportCard, IEnumerable<ReportCardEntry> Entries)>();

        foreach (var student in studentsToGenerate)
        {
            nisnMap.TryGetValue(student.StudentId, out var studentNisn);

            var createCardResult = ReportCard.CreateDraft(
                studentId: student.StudentId,
                studentName: student.StudentName,
                studentNis: student.StudentNis,
                studentNisn: studentNisn,
                academicYearId: period.AcademicYearId,
                academicYearName: period.AcademicYearName,
                operationalPeriodId: period.Id,
                operationalPeriodName: period.Name,
                studyGroupId: studyGroup.Id,
                studyGroupName: studyGroup.Name,
                homeroomTeacherId: studyGroup.HomeroomTeacherId,
                homeroomTeacherName: studyGroup.HomeroomTeacherName,
                utcNow: now);

            if (!createCardResult.IsSuccess || createCardResult.Value is null)
            {
                return Result<GenerateDraftReportCardsResponse>.Failure(createCardResult.Error);
            }

            var card = createCardResult.Value;

            if (entriesByStudent.TryGetValue(student.StudentId, out var studentEntries))
            {
                foreach (var entryData in studentEntries)
                {
                    var entryResult = ReportCardEntry.Create(
                        reportCardId: card.Id,
                        subjectId: entryData.SubjectId,
                        subjectName: entryData.SubjectName,
                        subjectCode: entryData.SubjectCode,
                        subjectStageId: entryData.SubjectStageId,
                        subjectStageName: entryData.SubjectStageName,
                        numericScore: entryData.NumericScore,
                        letterGrade: null,
                        teacherId: entryData.TeacherId,
                        teacherName: entryData.TeacherName,
                        teacherNotes: null,
                        utcNow: now);

                    if (!entryResult.IsSuccess || entryResult.Value is null)
                    {
                        return Result<GenerateDraftReportCardsResponse>.Failure(entryResult.Error);
                    }

                    card.AddEntry(entryResult.Value);
                }
            }

            batchToInsert.Add((card, card.Entries));
        }

        await reportCardRepository.InsertBatchAsync(batchToInsert, cancellationToken);

        return Result<GenerateDraftReportCardsResponse>.Success(
            new GenerateDraftReportCardsResponse(batchToInsert.Count, skippedCount));
    }
}
