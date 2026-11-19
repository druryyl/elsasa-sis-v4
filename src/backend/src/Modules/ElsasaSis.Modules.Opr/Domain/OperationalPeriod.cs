using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Opr.Domain;

public sealed class OperationalPeriod : Entity<Guid>
{
    public Guid AcademicYearId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public OperationalPeriodStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private OperationalPeriod()
    {
    }

    private OperationalPeriod(
        Guid id,
        Guid academicYearId,
        string name,
        DateOnly startDate,
        DateOnly endDate,
        OperationalPeriodStatus status,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        AcademicYearId = academicYearId;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    internal static OperationalPeriod Create(
        Guid academicYearId,
        string name,
        DateOnly startDate,
        DateOnly endDate,
        DateTime utcNow) =>
        new(
            UuidV7.NewGuid(),
            academicYearId,
            name.Trim(),
            startDate,
            endDate,
            OperationalPeriodStatus.Inactive,
            utcNow,
            utcNow);

    public static OperationalPeriod Rehydrate(
        Guid id,
        Guid academicYearId,
        string name,
        DateOnly startDate,
        DateOnly endDate,
        OperationalPeriodStatus status,
        DateTime createdAt,
        DateTime updatedAt) =>
        new(id, academicYearId, name, startDate, endDate, status, createdAt, updatedAt);

    internal void UpdateDetails(string name, DateOnly startDate, DateOnly endDate, DateTime utcNow)
    {
        Name = name.Trim();
        StartDate = startDate;
        EndDate = endDate;
        UpdatedAt = utcNow;
    }

    internal void Activate(DateTime utcNow)
    {
        Status = OperationalPeriodStatus.Active;
        UpdatedAt = utcNow;
    }

    internal void Deactivate(DateTime utcNow)
    {
        Status = OperationalPeriodStatus.Inactive;
        UpdatedAt = utcNow;
    }
}
