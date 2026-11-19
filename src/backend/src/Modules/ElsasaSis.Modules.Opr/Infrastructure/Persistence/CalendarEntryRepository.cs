using System.Data.Common;
using Dapper;
using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Opr.Infrastructure.Persistence;

public sealed class CalendarEntryRepository(ISqlConnectionFactory connectionFactory) : ICalendarEntryRepository
{
    private const string UniqueYearDateConstraint = "UQ_calendar_entries_year_date";

    private const string SelectByIdSql = """
        SELECT id, academic_year_id, entry_date, day_type, description, created_at, updated_at
        FROM opr.calendar_entries
        WHERE id = @Id;
        """;

    private const string ExistsByYearAndDateSql = """
        SELECT CASE WHEN EXISTS (
            SELECT 1
            FROM opr.calendar_entries
            WHERE academic_year_id = @AcademicYearId
              AND entry_date = @EntryDate
              AND (@ExcludeId IS NULL OR id <> @ExcludeId)
        ) THEN 1 ELSE 0 END;
        """;

    private const string InsertSql = """
        INSERT INTO opr.calendar_entries (id, academic_year_id, entry_date, day_type, description, created_at, updated_at)
        VALUES (@Id, @AcademicYearId, @EntryDate, @DayType, @Description, @CreatedAt, @UpdatedAt);
        """;

    private const string UpdateSql = """
        UPDATE opr.calendar_entries
        SET entry_date = @EntryDate,
            day_type = @DayType,
            description = @Description,
            updated_at = @UpdatedAt
        WHERE id = @Id;
        """;

    private const string DeleteSql = """
        DELETE FROM opr.calendar_entries
        WHERE id = @Id;
        """;

    public async Task<CalendarEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<CalendarEntryRow>(
            new CommandDefinition(SelectByIdSql, new { Id = id }, cancellationToken: cancellationToken));

        if (row is null)
        {
            return null;
        }

        var dayTypeResult = CalendarDayTypeExtensions.Parse(row.day_type);
        if (dayTypeResult.IsFailure)
        {
            throw new InvalidOperationException($"Unsupported calendar day_type '{row.day_type}' stored for entry '{row.id}'.");
        }

        return CalendarEntry.Rehydrate(
            row.id,
            row.academic_year_id,
            DateOnly.FromDateTime(row.entry_date),
            dayTypeResult.Value,
            row.description,
            DateTime.SpecifyKind(row.created_at, DateTimeKind.Utc),
            DateTime.SpecifyKind(row.updated_at, DateTimeKind.Utc));
    }

    public async Task<bool> ExistsByAcademicYearAndDateAsync(
        Guid academicYearId,
        DateOnly entryDate,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                ExistsByYearAndDateSql,
                new
                {
                    AcademicYearId = academicYearId,
                    EntryDate = entryDate.ToDateTime(TimeOnly.MinValue),
                    ExcludeId = excludeId
                },
                cancellationToken: cancellationToken));
    }

    public async Task<Result> AddAsync(CalendarEntry entry, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                InsertSql,
                ToParameters(entry),
                cancellationToken: cancellationToken));

            return Result.Success();
        }
        catch (DbException ex) when (SqlExceptionHelper.IsUniqueConstraintViolation(ex, UniqueYearDateConstraint))
        {
            return Result.Failure(OprErrors.DuplicateCalendarEntryDate(entry.AcademicYearId, entry.EntryDate));
        }
    }

    public async Task<Result> UpdateAsync(CalendarEntry entry, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                UpdateSql,
                ToParameters(entry),
                cancellationToken: cancellationToken));

            return Result.Success();
        }
        catch (DbException ex) when (SqlExceptionHelper.IsUniqueConstraintViolation(ex, UniqueYearDateConstraint))
        {
            return Result.Failure(OprErrors.DuplicateCalendarEntryDate(entry.AcademicYearId, entry.EntryDate));
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            DeleteSql,
            new { Id = id },
            cancellationToken: cancellationToken));
    }

    private static object ToParameters(CalendarEntry entry) => new
    {
        entry.Id,
        entry.AcademicYearId,
        EntryDate = entry.EntryDate.ToDateTime(TimeOnly.MinValue),
        DayType = entry.DayType.ToCode(),
        entry.Description,
        entry.CreatedAt,
        entry.UpdatedAt
    };

    private sealed class CalendarEntryRow
    {
        public Guid id { get; init; }
        public Guid academic_year_id { get; init; }
        public DateTime entry_date { get; init; }
        public string day_type { get; init; } = string.Empty;
        public string? description { get; init; }
        public DateTime created_at { get; init; }
        public DateTime updated_at { get; init; }
    }
}
