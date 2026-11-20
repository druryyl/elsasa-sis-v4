using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Opr.Domain.Model;

public sealed class StudyGroupTimetable : AggregateRoot<Guid>
{
    public Guid OperationalPeriodId { get; private set; }
    public Guid StudyGroupId { get; private set; }
    public string StudyGroupName { get; private set; } = string.Empty;
    public string ScheduleType { get; private set; } = string.Empty;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private StudyGroupTimetable()
    {
    }

    private StudyGroupTimetable(
        Guid id,
        Guid operationalPeriodId,
        Guid studyGroupId,
        string studyGroupName,
        string scheduleType,
        DateOnly startDate,
        DateOnly endDate,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        OperationalPeriodId = operationalPeriodId;
        StudyGroupId = studyGroupId;
        StudyGroupName = studyGroupName;
        ScheduleType = scheduleType;
        StartDate = startDate;
        EndDate = endDate;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Result<StudyGroupTimetable> Create(
        Guid operationalPeriodId,
        Guid studyGroupId,
        string studyGroupName,
        string scheduleType,
        DateOnly startDate,
        DateOnly endDate,
        DateTime? utcNow = null)
    {
        var validation = Validate(operationalPeriodId, studyGroupId, studyGroupName, scheduleType, startDate, endDate);
        if (validation.IsFailure)
        {
            return Result<StudyGroupTimetable>.Failure(validation.Error);
        }

        var normalizedType = ScheduleTypeExtensions.ParseBlockScheduleType(scheduleType).Value.ToCode();
        var now = utcNow ?? DateTime.UtcNow;

        var block = new StudyGroupTimetable(
            UuidV7.NewGuid(),
            operationalPeriodId,
            studyGroupId,
            studyGroupName.Trim(),
            normalizedType,
            startDate,
            endDate,
            now,
            now);

        return Result<StudyGroupTimetable>.Success(block);
    }

    public Result Update(
        string scheduleType,
        DateOnly startDate,
        DateOnly endDate,
        DateTime? utcNow = null)
    {
        var parseTypeResult = ScheduleTypeExtensions.ParseBlockScheduleType(scheduleType);
        if (parseTypeResult.IsFailure)
        {
            return Result.Failure(parseTypeResult.Error);
        }

        if (startDate > endDate)
        {
            return Result.Failure(TimetableErrors.InvalidDateRange());
        }

        ScheduleType = parseTypeResult.Value.ToCode();
        StartDate = startDate;
        EndDate = endDate;
        UpdatedAt = utcNow ?? DateTime.UtcNow;

        return Result.Success();
    }

    public static StudyGroupTimetable Rehydrate(
        Guid id,
        Guid operationalPeriodId,
        Guid studyGroupId,
        string studyGroupName,
        string scheduleType,
        DateOnly startDate,
        DateOnly endDate,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new StudyGroupTimetable(
            id,
            operationalPeriodId,
            studyGroupId,
            studyGroupName,
            scheduleType,
            startDate,
            endDate,
            createdAt,
            updatedAt);
    }

    private static Result Validate(
        Guid operationalPeriodId,
        Guid studyGroupId,
        string studyGroupName,
        string scheduleType,
        DateOnly startDate,
        DateOnly endDate)
    {
        if (operationalPeriodId == Guid.Empty)
        {
            return Result.Failure(TimetableErrors.FieldRequired(nameof(operationalPeriodId)));
        }

        if (studyGroupId == Guid.Empty)
        {
            return Result.Failure(TimetableErrors.FieldRequired(nameof(studyGroupId)));
        }

        if (string.IsNullOrWhiteSpace(studyGroupName))
        {
            return Result.Failure(TimetableErrors.FieldRequired(nameof(studyGroupName)));
        }

        var parseTypeResult = ScheduleTypeExtensions.ParseBlockScheduleType(scheduleType);
        if (parseTypeResult.IsFailure)
        {
            return Result.Failure(parseTypeResult.Error);
        }

        if (startDate > endDate)
        {
            return Result.Failure(TimetableErrors.InvalidDateRange());
        }

        return Result.Success();
    }
}
