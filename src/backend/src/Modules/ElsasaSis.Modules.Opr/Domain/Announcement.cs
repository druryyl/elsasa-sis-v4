using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Opr.Domain;

public sealed class Announcement : AggregateRoot<Guid>
{
    public Guid AcademicYearId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public Guid AuthorId { get; private set; }
    public string AuthorName { get; private set; } = string.Empty;
    public AnnouncementAudience TargetAudience { get; private set; }
    public AnnouncementStatus Status { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Announcement()
    {
    }

    private Announcement(
        Guid id,
        Guid academicYearId,
        string title,
        string content,
        Guid authorId,
        string authorName,
        AnnouncementAudience targetAudience,
        AnnouncementStatus status,
        DateTime? publishedAt,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        AcademicYearId = academicYearId;
        Title = title;
        Content = content;
        AuthorId = authorId;
        AuthorName = authorName;
        TargetAudience = targetAudience;
        Status = status;
        PublishedAt = publishedAt;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Result<Announcement> Create(
        Guid academicYearId,
        string title,
        string content,
        Guid authorId,
        string authorName,
        AnnouncementAudience targetAudience,
        DateTime? utcNow = null)
    {
        if (academicYearId == Guid.Empty)
        {
            return Result<Announcement>.Failure(OprErrors.AnnouncementAcademicYearRequired());
        }

        if (authorId == Guid.Empty || string.IsNullOrWhiteSpace(authorName))
        {
            return Result<Announcement>.Failure(OprErrors.AnnouncementAuthorRequired());
        }

        var validationResult = ValidateContentAndAudience(title, content, targetAudience);
        if (validationResult.IsFailure)
        {
            return Result<Announcement>.Failure(validationResult.Error);
        }

        var now = utcNow ?? DateTime.UtcNow;
        var announcement = new Announcement(
            UuidV7.NewGuid(),
            academicYearId,
            title.Trim(),
            content.Trim(),
            authorId,
            authorName.Trim(),
            targetAudience,
            AnnouncementStatus.Draft,
            null,
            now,
            now);

        return Result<Announcement>.Success(announcement);
    }

    public Result Update(
        string title,
        string content,
        AnnouncementAudience targetAudience,
        DateTime? utcNow = null)
    {
        var validationResult = ValidateContentAndAudience(title, content, targetAudience);
        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        Title = title.Trim();
        Content = content.Trim();
        TargetAudience = targetAudience;
        UpdatedAt = utcNow ?? DateTime.UtcNow;

        return Result.Success();
    }

    public Result Publish(DateTime? utcNow = null)
    {
        if (Status == AnnouncementStatus.Published)
        {
            return Result.Failure(OprErrors.AnnouncementAlreadyPublished(Id));
        }

        var now = utcNow ?? DateTime.UtcNow;
        Status = AnnouncementStatus.Published;
        PublishedAt = now;
        UpdatedAt = now;

        return Result.Success();
    }

    public static Announcement Rehydrate(
        Guid id,
        Guid academicYearId,
        string title,
        string content,
        Guid authorId,
        string authorName,
        AnnouncementAudience targetAudience,
        AnnouncementStatus status,
        DateTime? publishedAt,
        DateTime createdAt,
        DateTime updatedAt) =>
        new(
            id,
            academicYearId,
            title,
            content,
            authorId,
            authorName,
            targetAudience,
            status,
            publishedAt,
            createdAt,
            updatedAt);

    private static Result ValidateContentAndAudience(
        string title,
        string content,
        AnnouncementAudience targetAudience)
    {
        // BR-OPS-013: An Announcement must have a non-empty title and content.
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure(OprErrors.AnnouncementTitleRequired());
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return Result.Failure(OprErrors.AnnouncementContentRequired());
        }

        // BR-OPS-012: An Announcement must specify exactly one Target Audience.
        if (!Enum.IsDefined(targetAudience))
        {
            return Result.Failure(OprErrors.InvalidAnnouncementAudience(targetAudience.ToString()));
        }

        return Result.Success();
    }
}
