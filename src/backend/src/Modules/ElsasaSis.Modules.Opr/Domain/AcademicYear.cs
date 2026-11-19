using ElsasaSis.Contracts.Operational.Events;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Opr.Domain;

public sealed class AcademicYear : AggregateRoot<Guid>
{
    private readonly List<OperationalPeriod> _periods = [];

    public string Name { get; private set; } = string.Empty;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public AcademicYearStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<OperationalPeriod> Periods => _periods.AsReadOnly();

    private AcademicYear()
    {
    }

    private AcademicYear(
        Guid id,
        string name,
        DateOnly startDate,
        DateOnly endDate,
        AcademicYearStatus status,
        DateTime createdAt,
        DateTime updatedAt,
        IEnumerable<OperationalPeriod> periods)
        : base(id)
    {
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        _periods.AddRange(periods);
    }

    public static Result<AcademicYear> Create(
        string name,
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyList<OperationalPeriodInput> periods,
        DateTime? utcNow = null)
    {
        var validationResult = ValidateDefinition(name, startDate, endDate, periods);
        if (validationResult.IsFailure)
        {
            return Result<AcademicYear>.Failure(validationResult.Error);
        }

        var now = utcNow ?? DateTime.UtcNow;
        var yearId = UuidV7.NewGuid();
        var periodEntities = periods
            .Select(p => OperationalPeriod.Create(yearId, p.Name, p.StartDate, p.EndDate, now))
            .ToList();

        var academicYear = new AcademicYear(
            yearId,
            name.Trim(),
            startDate,
            endDate,
            AcademicYearStatus.Inactive,
            now,
            now,
            periodEntities);

        return Result<AcademicYear>.Success(academicYear);
    }

    public Result Update(
        string name,
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyList<OperationalPeriodInput> periods,
        DateTime? utcNow = null)
    {
        var validationResult = ValidateDefinition(name, startDate, endDate, periods);
        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        // OQ-003: Reject removing the currently ACTIVE operational period.
        var activePeriod = _periods.FirstOrDefault(p => p.Status == OperationalPeriodStatus.Active);
        if (activePeriod is not null && !periods.Any(p => p.Id == activePeriod.Id))
        {
            return Result.Failure(OprErrors.CannotRemoveActivePeriod(activePeriod.Id));
        }

        var now = utcNow ?? DateTime.UtcNow;
        var existingById = _periods.ToDictionary(p => p.Id);
        var updatedPeriods = new List<OperationalPeriod>(periods.Count);

        foreach (var input in periods)
        {
            if (input.Id.HasValue && existingById.TryGetValue(input.Id.Value, out var existing))
            {
                existing.UpdateDetails(input.Name, input.StartDate, input.EndDate, now);
                updatedPeriods.Add(existing);
            }
            else
            {
                updatedPeriods.Add(OperationalPeriod.Create(Id, input.Name, input.StartDate, input.EndDate, now));
            }
        }

        Name = name.Trim();
        StartDate = startDate;
        EndDate = endDate;
        UpdatedAt = now;
        _periods.Clear();
        _periods.AddRange(updatedPeriods);

        return Result.Success();
    }

    public Result Activate(DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        Status = AcademicYearStatus.Active;
        UpdatedAt = now;

        RaiseDomainEvent(new AcademicYearActivatedDomainEvent(Id, Name, StartDate, EndDate));

        var activePeriod = _periods.FirstOrDefault(p => p.Status == OperationalPeriodStatus.Active);
        if (activePeriod is null)
        {
            var earliestPeriod = _periods
                .OrderBy(p => p.StartDate)
                .ThenBy(p => p.EndDate)
                .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            if (earliestPeriod is not null)
            {
                earliestPeriod.Activate(now);
                RaiseDomainEvent(new OperationalPeriodActivatedDomainEvent(
                    Id,
                    earliestPeriod.Id,
                    earliestPeriod.Name,
                    earliestPeriod.StartDate,
                    earliestPeriod.EndDate));
            }
        }

        return Result.Success();
    }

    public Result ActivatePeriod(Guid periodId, DateTime? utcNow = null)
    {
        if (Status != AcademicYearStatus.Active)
        {
            return Result.Failure(OprErrors.YearNotActiveForPeriodActivation(Id));
        }

        var targetPeriod = _periods.FirstOrDefault(p => p.Id == periodId);
        if (targetPeriod is null)
        {
            return Result.Failure(OprErrors.PeriodNotFound(periodId));
        }

        var now = utcNow ?? DateTime.UtcNow;
        foreach (var period in _periods.Where(p => p.Id != periodId && p.Status == OperationalPeriodStatus.Active))
        {
            period.Deactivate(now);
        }

        targetPeriod.Activate(now);
        UpdatedAt = now;

        RaiseDomainEvent(new OperationalPeriodActivatedDomainEvent(
            Id,
            targetPeriod.Id,
            targetPeriod.Name,
            targetPeriod.StartDate,
            targetPeriod.EndDate));

        return Result.Success();
    }

    public static AcademicYear Rehydrate(
        Guid id,
        string name,
        DateOnly startDate,
        DateOnly endDate,
        AcademicYearStatus status,
        DateTime createdAt,
        DateTime updatedAt,
        IEnumerable<OperationalPeriod> periods) =>
        new(id, name, startDate, endDate, status, createdAt, updatedAt, periods);

    private static Result ValidateDefinition(
        string name,
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyList<OperationalPeriodInput>? periods)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(OprErrors.YearNameRequired());
        }

        // BR-OPS-005: Academic Year end date must be strictly after start date.
        if (endDate <= startDate)
        {
            return Result.Failure(OprErrors.InvalidDateRange("endDate"));
        }

        // BR-OPS-002: An Academic Year must contain at least one Operational Period.
        if (periods is null || periods.Count == 0)
        {
            return Result.Failure(OprErrors.AtLeastOnePeriodRequired());
        }

        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var period in periods)
        {
            if (string.IsNullOrWhiteSpace(period.Name))
            {
                return Result.Failure(OprErrors.PeriodNameRequired());
            }

            var trimmedPeriodName = period.Name.Trim();

            // BR-OPS-006: Operational Period end date must be strictly after start date.
            if (period.EndDate <= period.StartDate)
            {
                return Result.Failure(OprErrors.InvalidDateRange("periods"));
            }

            // BR-OPS-003: Operational Period dates must fall within parent Academic Year bounds.
            if (period.StartDate < startDate || period.EndDate > endDate)
            {
                return Result.Failure(OprErrors.PeriodOutOfBounds(trimmedPeriodName));
            }

            if (!seenNames.Add(trimmedPeriodName))
            {
                return Result.Failure(OprErrors.DuplicatePeriodName(trimmedPeriodName));
            }
        }

        // BR-OPS-004: Operational Periods within an Academic Year must not overlap.
        var sortedPeriods = periods
            .OrderBy(p => p.StartDate)
            .ThenBy(p => p.EndDate)
            .ToList();

        for (var i = 0; i < sortedPeriods.Count - 1; i++)
        {
            var current = sortedPeriods[i];
            var next = sortedPeriods[i + 1];

            if (next.StartDate <= current.EndDate)
            {
                return Result.Failure(OprErrors.OverlappingPeriods(current.Name.Trim(), next.Name.Trim()));
            }
        }

        return Result.Success();
    }
}
