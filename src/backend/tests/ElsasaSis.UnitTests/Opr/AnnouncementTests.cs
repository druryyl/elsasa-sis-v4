using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.UnitTests.Opr;

public sealed class AnnouncementTests
{
    [Fact]
    public void Create_WithValidInputs_SucceedsAsDraftWithTrimmedFields()
    {
        var yearId = UuidV7.NewGuid();
        var authorId = UuidV7.NewGuid();
        var utcNow = new DateTime(2026, 7, 10, 7, 0, 0, DateTimeKind.Utc);

        var result = Announcement.Create(
            yearId,
            "  First Day Welcome  ",
            "  Classes begin at 07:00 WIB.  ",
            authorId,
            "  System Administrator  ",
            AnnouncementAudience.All,
            utcNow);

        Assert.True(result.IsSuccess);
        var announcement = result.Value!;
        Assert.NotEqual(Guid.Empty, announcement.Id);
        Assert.Equal(yearId, announcement.AcademicYearId);
        Assert.Equal("First Day Welcome", announcement.Title);
        Assert.Equal("Classes begin at 07:00 WIB.", announcement.Content);
        Assert.Equal(authorId, announcement.AuthorId);
        Assert.Equal("System Administrator", announcement.AuthorName);
        Assert.Equal(AnnouncementAudience.All, announcement.TargetAudience);
        Assert.Equal(AnnouncementStatus.Draft, announcement.Status);
        Assert.Null(announcement.PublishedAt);
        Assert.Equal(utcNow, announcement.CreatedAt);
        Assert.Equal(utcNow, announcement.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenTitleIsBlank_FailsWithTitleRequired(string blankTitle)
    {
        var result = Announcement.Create(
            UuidV7.NewGuid(),
            blankTitle,
            "Body content",
            UuidV7.NewGuid(),
            "Admin",
            AnnouncementAudience.All);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Opr.Announcement.TitleRequired", result.Error.Code);
        Assert.True(result.Error.Errors!.ContainsKey("title"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenContentIsBlank_FailsWithContentRequired(string blankContent)
    {
        var result = Announcement.Create(
            UuidV7.NewGuid(),
            "Valid Title",
            blankContent,
            UuidV7.NewGuid(),
            "Admin",
            AnnouncementAudience.Teachers);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Opr.Announcement.ContentRequired", result.Error.Code);
        Assert.True(result.Error.Errors!.ContainsKey("content"));
    }

    [Theory]
    [InlineData("ALL", AnnouncementAudience.All)]
    [InlineData("teachers", AnnouncementAudience.Teachers)]
    [InlineData(" Students ", AnnouncementAudience.Students)]
    [InlineData("PARENTS", AnnouncementAudience.Parents)]
    public void AnnouncementAudienceExtensions_ParseAndToCode_MapsAllSupportedAudiences(
        string raw,
        AnnouncementAudience expected)
    {
        var result = AnnouncementAudienceExtensions.Parse(raw);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
        Assert.Equal(raw.Trim().ToUpperInvariant(), expected.ToCode());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ALUMNI")]
    public void AnnouncementAudienceExtensions_ParseInvalid_FailsWithInvalidAudience(string? raw)
    {
        var result = AnnouncementAudienceExtensions.Parse(raw);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Opr.Announcement.InvalidAudience", result.Error.Code);
        Assert.True(result.Error.Errors!.ContainsKey("targetAudience"));
    }

    [Fact]
    public void Update_WithValidInputs_UpdatesTitleContentAudienceAndUpdatedAt()
    {
        var createdAt = new DateTime(2026, 7, 10, 7, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2026, 7, 10, 9, 0, 0, DateTimeKind.Utc);
        var announcement = Announcement.Create(
            UuidV7.NewGuid(),
            "Draft Title",
            "Draft Content",
            UuidV7.NewGuid(),
            "Admin",
            AnnouncementAudience.All,
            createdAt).Value!;

        var result = announcement.Update(
            "  Updated Title  ",
            "  Updated Content  ",
            AnnouncementAudience.Teachers,
            updatedAt);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Title", announcement.Title);
        Assert.Equal("Updated Content", announcement.Content);
        Assert.Equal(AnnouncementAudience.Teachers, announcement.TargetAudience);
        Assert.Equal(AnnouncementStatus.Draft, announcement.Status);
        Assert.Equal(createdAt, announcement.CreatedAt);
        Assert.Equal(updatedAt, announcement.UpdatedAt);
    }

    [Fact]
    public void Update_WhenContentBlank_FailsAndPreservesOriginalState()
    {
        var announcement = Announcement.Create(
            UuidV7.NewGuid(),
            "Original Title",
            "Original Content",
            UuidV7.NewGuid(),
            "Admin",
            AnnouncementAudience.Parents).Value!;

        var result = announcement.Update("New Title", "   ", AnnouncementAudience.Students);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Opr.Announcement.ContentRequired", result.Error.Code);
        Assert.Equal("Original Title", announcement.Title);
        Assert.Equal("Original Content", announcement.Content);
        Assert.Equal(AnnouncementAudience.Parents, announcement.TargetAudience);
    }

    [Fact]
    public void Publish_WhenDraft_TransitionsToPublishedAndSetsPublishedAt()
    {
        var createdAt = new DateTime(2026, 7, 10, 7, 0, 0, DateTimeKind.Utc);
        var publishedAt = new DateTime(2026, 7, 11, 8, 15, 0, DateTimeKind.Utc);
        var announcement = Announcement.Create(
            UuidV7.NewGuid(),
            "Staff Meeting",
            "Mandatory meeting at 13:00.",
            UuidV7.NewGuid(),
            "Admin",
            AnnouncementAudience.Teachers,
            createdAt).Value!;

        var result = announcement.Publish(publishedAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(AnnouncementStatus.Published, announcement.Status);
        Assert.Equal(publishedAt, announcement.PublishedAt);
        Assert.Equal(publishedAt, announcement.UpdatedAt);
        Assert.Equal(createdAt, announcement.CreatedAt);
    }

    [Fact]
    public void Publish_WhenAlreadyPublished_FailsWithConflict409()
    {
        var firstPublishedAt = new DateTime(2026, 7, 11, 8, 15, 0, DateTimeKind.Utc);
        var secondAttemptAt = new DateTime(2026, 7, 12, 8, 15, 0, DateTimeKind.Utc);
        var announcement = Announcement.Create(
            UuidV7.NewGuid(),
            "Staff Meeting",
            "Mandatory meeting at 13:00.",
            UuidV7.NewGuid(),
            "Admin",
            AnnouncementAudience.Teachers).Value!;

        Assert.True(announcement.Publish(firstPublishedAt).IsSuccess);

        var secondResult = announcement.Publish(secondAttemptAt);

        Assert.True(secondResult.IsFailure);
        Assert.Equal(ErrorType.Conflict, secondResult.Error.Type);
        Assert.Equal("Opr.Announcement.AlreadyPublished", secondResult.Error.Code);
        Assert.Equal(firstPublishedAt, announcement.PublishedAt);
    }

    [Fact]
    public void Create_WhenAuthorMissingOrAudienceUndefined_FailsWithValidation()
    {
        var missingAuthor = Announcement.Create(
            UuidV7.NewGuid(),
            "Title",
            "Content",
            Guid.Empty,
            "Admin",
            AnnouncementAudience.All);

        Assert.True(missingAuthor.IsFailure);
        Assert.Equal(ErrorType.Validation, missingAuthor.Error.Type);
        Assert.Equal("Opr.Announcement.AuthorRequired", missingAuthor.Error.Code);

        var blankAuthorName = Announcement.Create(
            UuidV7.NewGuid(),
            "Title",
            "Content",
            UuidV7.NewGuid(),
            "   ",
            AnnouncementAudience.All);

        Assert.True(blankAuthorName.IsFailure);
        Assert.Equal("Opr.Announcement.AuthorRequired", blankAuthorName.Error.Code);

        var invalidAudience = Announcement.Create(
            UuidV7.NewGuid(),
            "Title",
            "Content",
            UuidV7.NewGuid(),
            "Admin",
            (AnnouncementAudience)999);

        Assert.True(invalidAudience.IsFailure);
        Assert.Equal(ErrorType.Validation, invalidAudience.Error.Type);
        Assert.Equal("Opr.Announcement.InvalidAudience", invalidAudience.Error.Code);
    }
}
