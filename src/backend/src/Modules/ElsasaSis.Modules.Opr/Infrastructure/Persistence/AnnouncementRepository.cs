using Dapper;
using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Opr.Infrastructure.Persistence;

public sealed class AnnouncementRepository(ISqlConnectionFactory connectionFactory) : IAnnouncementRepository
{
    private const string SelectByIdSql = """
        SELECT id, academic_year_id, title, content, author_id, author_name, target_audience, status, published_at, created_at, updated_at
        FROM opr.announcements
        WHERE id = @Id;
        """;

    private const string InsertSql = """
        INSERT INTO opr.announcements (id, academic_year_id, title, content, author_id, author_name, target_audience, status, published_at, created_at, updated_at)
        VALUES (@Id, @AcademicYearId, @Title, @Content, @AuthorId, @AuthorName, @TargetAudience, @Status, @PublishedAt, @CreatedAt, @UpdatedAt);
        """;

    private const string UpdateSql = """
        UPDATE opr.announcements
        SET title = @Title,
            content = @Content,
            target_audience = @TargetAudience,
            status = @Status,
            published_at = @PublishedAt,
            updated_at = @UpdatedAt
        WHERE id = @Id;
        """;

    public async Task<Announcement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<AnnouncementRow>(
            new CommandDefinition(SelectByIdSql, new { Id = id }, cancellationToken: cancellationToken));

        if (row is null)
        {
            return null;
        }

        var audienceResult = AnnouncementAudienceExtensions.Parse(row.target_audience);
        if (audienceResult.IsFailure)
        {
            throw new InvalidOperationException($"Unsupported target_audience '{row.target_audience}' stored for announcement '{row.id}'.");
        }

        var statusResult = AnnouncementStatusExtensions.Parse(row.status);
        if (statusResult.IsFailure)
        {
            throw new InvalidOperationException($"Unsupported status '{row.status}' stored for announcement '{row.id}'.");
        }

        return Announcement.Rehydrate(
            row.id,
            row.academic_year_id,
            row.title,
            row.content,
            row.author_id,
            row.author_name,
            audienceResult.Value,
            statusResult.Value,
            row.published_at.HasValue ? DateTime.SpecifyKind(row.published_at.Value, DateTimeKind.Utc) : null,
            DateTime.SpecifyKind(row.created_at, DateTimeKind.Utc),
            DateTime.SpecifyKind(row.updated_at, DateTimeKind.Utc));
    }

    public async Task<Result> AddAsync(Announcement announcement, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            InsertSql,
            ToParameters(announcement),
            cancellationToken: cancellationToken));

        return Result.Success();
    }

    public async Task<Result> UpdateAsync(Announcement announcement, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            UpdateSql,
            ToParameters(announcement),
            cancellationToken: cancellationToken));

        return Result.Success();
    }

    private static object ToParameters(Announcement announcement) => new
    {
        announcement.Id,
        announcement.AcademicYearId,
        announcement.Title,
        announcement.Content,
        announcement.AuthorId,
        announcement.AuthorName,
        TargetAudience = announcement.TargetAudience.ToCode(),
        Status = announcement.Status.ToCode(),
        announcement.PublishedAt,
        announcement.CreatedAt,
        announcement.UpdatedAt
    };

    private sealed class AnnouncementRow
    {
        public Guid id { get; init; }
        public Guid academic_year_id { get; init; }
        public string title { get; init; } = string.Empty;
        public string content { get; init; } = string.Empty;
        public Guid author_id { get; init; }
        public string author_name { get; init; } = string.Empty;
        public string target_audience { get; init; } = string.Empty;
        public string status { get; init; } = string.Empty;
        public DateTime? published_at { get; init; }
        public DateTime created_at { get; init; }
        public DateTime updated_at { get; init; }
    }
}
