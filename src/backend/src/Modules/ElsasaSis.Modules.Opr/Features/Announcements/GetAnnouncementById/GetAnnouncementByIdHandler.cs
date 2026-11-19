using Dapper;
using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.Modules.Opr.Features.Announcements.GetAnnouncementById;

public sealed class GetAnnouncementByIdHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string SelectSql = """
        SELECT
            a.id,
            a.academic_year_id,
            COALESCE(ay.name, N'') AS academic_year_name,
            a.title,
            a.content,
            a.author_id,
            a.author_name,
            a.target_audience,
            a.status,
            a.published_at,
            a.created_at,
            a.updated_at
        FROM opr.announcements a
        LEFT JOIN opr.academic_years ay ON ay.id = a.academic_year_id
        WHERE a.id = @Id;
        """;

    public async Task<Result<AnnouncementDto>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<AnnouncementRow>(
            new CommandDefinition(SelectSql, new { Id = id }, cancellationToken: cancellationToken));

        if (row is null)
        {
            return Result<AnnouncementDto>.Failure(OprErrors.AnnouncementNotFound(id));
        }

        if (!AnnouncementAccessPolicy.CanView(currentUser, row.status, row.target_audience))
        {
            return Result<AnnouncementDto>.Failure(OprErrors.AnnouncementForbidden(id));
        }

        return Result<AnnouncementDto>.Success(new AnnouncementDto(
            row.id,
            row.academic_year_id,
            row.academic_year_name,
            row.title,
            row.content,
            row.author_id,
            row.author_name,
            row.target_audience,
            row.status,
            row.published_at.HasValue ? DateTime.SpecifyKind(row.published_at.Value, DateTimeKind.Utc) : null,
            DateTime.SpecifyKind(row.created_at, DateTimeKind.Utc),
            DateTime.SpecifyKind(row.updated_at, DateTimeKind.Utc)));
    }

    private sealed class AnnouncementRow
    {
        public Guid id { get; init; }
        public Guid academic_year_id { get; init; }
        public string academic_year_name { get; init; } = string.Empty;
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
