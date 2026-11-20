using ElsasaSis.Modules.Opr.Features.Announcements;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.UnitTests.Opr;

public sealed class AnnouncementAccessPolicyTests
{
    [Fact]
    public void IsParentOnly_WhenUserHasOnlyParentRole_ReturnsTrue()
    {
        var user = new TestUser(roles: ["PARENT"]);
        Assert.True(AnnouncementAccessPolicy.IsParentOnly(user));
    }

    [Fact]
    public void IsParentOnly_WhenUserHasParentAndTeacherRoles_ReturnsFalse()
    {
        var user = new TestUser(roles: ["PARENT", "TEACHER"]);
        Assert.False(AnnouncementAccessPolicy.IsParentOnly(user));
    }

    [Fact]
    public void IsParentOnly_WhenUserIsSchoolAdmin_ReturnsFalse()
    {
        var user = new TestUser(roles: ["SCHOOL_ADMIN"]);
        Assert.False(AnnouncementAccessPolicy.IsParentOnly(user));
    }

    [Fact]
    public void IsStudentOnly_WhenUserHasOnlyStudentRole_ReturnsTrue()
    {
        var user = new TestUser(roles: ["STUDENT"]);
        Assert.True(AnnouncementAccessPolicy.IsStudentOnly(user));
    }

    [Fact]
    public void IsStudentOnly_WhenUserHasStudentAndAdminRoles_ReturnsFalse()
    {
        var user = new TestUser(roles: ["STUDENT", "SCHOOL_ADMIN"]);
        Assert.False(AnnouncementAccessPolicy.IsStudentOnly(user));
    }

    [Fact]
    public void IsStudentOnly_WhenUserIsTeacher_ReturnsFalse()
    {
        var user = new TestUser(roles: ["TEACHER"]);
        Assert.False(AnnouncementAccessPolicy.IsStudentOnly(user));
    }

    [Theory]
    [InlineData("ALL", true)]
    [InlineData("PARENTS", true)]
    [InlineData("STUDENTS", false)]
    [InlineData("TEACHERS", false)]
    public void CanView_ForParent_AllowsOnlyAllAndParentsPublishedBulletins(string targetAudience, bool expected)
    {
        var parentUser = new TestUser(roles: ["PARENT"]);

        var canViewPublished = AnnouncementAccessPolicy.CanView(parentUser, "PUBLISHED", targetAudience);
        Assert.Equal(expected, canViewPublished);

        // Drafts are always hidden from parents
        var canViewDraft = AnnouncementAccessPolicy.CanView(parentUser, "DRAFT", targetAudience);
        Assert.False(canViewDraft);
    }

    [Theory]
    [InlineData("ALL", true)]
    [InlineData("STUDENTS", true)]
    [InlineData("PARENTS", false)]
    [InlineData("TEACHERS", false)]
    public void CanView_ForStudent_AllowsOnlyAllAndStudentsPublishedBulletins(string targetAudience, bool expected)
    {
        var studentUser = new TestUser(roles: ["STUDENT"]);

        var canViewPublished = AnnouncementAccessPolicy.CanView(studentUser, "PUBLISHED", targetAudience);
        Assert.Equal(expected, canViewPublished);

        // Drafts are always hidden from students
        var canViewDraft = AnnouncementAccessPolicy.CanView(studentUser, "DRAFT", targetAudience);
        Assert.False(canViewDraft);
    }

    [Theory]
    [InlineData("ALL", true)]
    [InlineData("TEACHERS", true)]
    [InlineData("PARENTS", false)]
    [InlineData("STUDENTS", false)]
    public void CanView_ForTeacherOnly_AllowsOnlyAllAndTeachersPublishedBulletins(string targetAudience, bool expected)
    {
        var teacherUser = new TestUser(roles: ["TEACHER"]);

        var canViewPublished = AnnouncementAccessPolicy.CanView(teacherUser, "PUBLISHED", targetAudience);
        Assert.Equal(expected, canViewPublished);

        var canViewDraft = AnnouncementAccessPolicy.CanView(teacherUser, "DRAFT", targetAudience);
        Assert.False(canViewDraft);
    }

    [Fact]
    public void CanView_ForAdminWithPublishPermission_CanViewAnyStatusAndAudience()
    {
        var adminUser = new TestUser(
            roles: ["SCHOOL_ADMIN"],
            permissions: ["Permissions.Operations.Announcements.Publish"]);

        Assert.True(AnnouncementAccessPolicy.CanView(adminUser, "DRAFT", "STUDENTS"));
        Assert.True(AnnouncementAccessPolicy.CanView(adminUser, "PUBLISHED", "TEACHERS"));
        Assert.True(AnnouncementAccessPolicy.CanView(adminUser, "PUBLISHED", "PARENTS"));
        Assert.True(AnnouncementAccessPolicy.CanView(adminUser, "PUBLISHED", "ALL"));
    }

    private sealed class TestUser(
        IReadOnlyList<string>? roles = null,
        IReadOnlyList<string>? permissions = null) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid UserId => Guid.NewGuid();
        public string Username => "test.user";
        public string FullName => "Test User";
        public IReadOnlyList<string> Roles { get; } = roles ?? [];
        public IReadOnlyList<string> Permissions { get; } = permissions ?? [];
        public Guid? TeacherId => null;
        public Guid? StudentId => null;
        public Guid? GuardianId => null;
    }
}
