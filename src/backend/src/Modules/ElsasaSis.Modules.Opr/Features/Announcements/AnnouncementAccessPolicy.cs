using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.Modules.Opr.Features.Announcements;

internal static class AnnouncementAccessPolicy
{
    public static bool HasCreateOrPublish(ICurrentUser currentUser) =>
        currentUser.Permissions.Contains(Permissions.Operations.Announcements.Create, StringComparer.Ordinal) ||
        currentUser.Permissions.Contains(Permissions.Operations.Announcements.Publish, StringComparer.Ordinal);

    public static bool IsTeacherOrHomeroomOnly(ICurrentUser currentUser)
    {
        var hasTeacherRole = currentUser.Roles.Any(r =>
            string.Equals(r, "TEACHER", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, "HOMEROOM_TEACHER", StringComparison.OrdinalIgnoreCase));

        var hasElevatedStaffRole = currentUser.Roles.Any(r =>
            string.Equals(r, "SCHOOL_ADMIN", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, "PRINCIPAL", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, "FINANCE_OFFICER", StringComparison.OrdinalIgnoreCase));

        return hasTeacherRole && !hasElevatedStaffRole;
    }

    public static bool CanView(ICurrentUser currentUser, string status, string targetAudience)
    {
        if (!HasCreateOrPublish(currentUser) &&
            !string.Equals(status, "PUBLISHED", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (IsTeacherOrHomeroomOnly(currentUser) &&
            !string.Equals(targetAudience, "ALL", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(targetAudience, "TEACHERS", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
}
