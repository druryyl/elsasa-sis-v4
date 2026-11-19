using System.Security.Claims;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.Api.Security;

/// <summary>
/// Resolves <see cref="ICurrentUser"/> from the current <see cref="HttpContext.User"/> claims.
/// </summary>
public sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public Guid UserId => ParseGuidClaim("sub") ?? ParseGuidClaim(ClaimTypes.NameIdentifier) ?? Guid.Empty;

    public string Username =>
        GetFirstClaimValue("preferred_username")
        ?? GetFirstClaimValue(ClaimTypes.Name)
        ?? string.Empty;

    public string FullName =>
        GetFirstClaimValue("name")
        ?? GetFirstClaimValue(ClaimTypes.GivenName)
        ?? string.Empty;

    public IReadOnlyList<string> Roles => GetClaimValues("roles", ClaimTypes.Role);

    public IReadOnlyList<string> Permissions => GetClaimValues("permissions");

    public Guid? TeacherId => ParseGuidClaim("teacherId");

    public Guid? StudentId => ParseGuidClaim("studentId");

    public Guid? GuardianId => ParseGuidClaim("guardianId");

    private string? GetFirstClaimValue(string claimType) =>
        User?.FindFirst(claimType)?.Value;

    private Guid? ParseGuidClaim(string claimType)
    {
        var value = GetFirstClaimValue(claimType);
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }

    private IReadOnlyList<string> GetClaimValues(params string[] claimTypes)
    {
        var user = User;
        if (user is null)
        {
            return [];
        }

        return user.Claims
            .Where(c => claimTypes.Contains(c.Type, StringComparer.Ordinal))
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
