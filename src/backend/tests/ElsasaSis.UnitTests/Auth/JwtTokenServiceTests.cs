using System.IdentityModel.Tokens.Jwt;
using ElsasaSis.Modules.Auth;
using ElsasaSis.Modules.Auth.Domain;
using ElsasaSis.Modules.Auth.Infrastructure;
using Microsoft.Extensions.Options;

namespace ElsasaSis.UnitTests.Auth;

public sealed class JwtTokenServiceTests
{
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Fact]
    public void GenerateAccessToken_EmitsExpectedClaimsAndOmitsOrgUnitId()
    {
        var fixedNow = new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);
        var options = Options.Create(new JwtOptions
        {
            SigningKey = "super-secret-signing-key-at-least-32-chars",
            Issuer = "ElsasaSis.Test",
            Audience = "ElsasaSis.Client.Test",
            AccessTokenExpirationMinutes = 15
        });

        var service = new JwtTokenService(options, new FixedTimeProvider(fixedNow));

        var userId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var user = AuthUser.Rehydrate(
            userId,
            "teacher1",
            "hashed",
            "teacher1@elsasa.local",
            "Budi Santoso",
            true,
            [RolePermissionCatalog.Roles.SchoolAdmin],
            new UserActors(teacherId, null, null),
            fixedNow.UtcDateTime,
            fixedNow.UtcDateTime);

        var permissions = RolePermissionCatalog.GetPermissionsForRoles(user.Roles);
        var result = service.GenerateAccessToken(user, permissions);

        Assert.Equal(fixedNow.UtcDateTime.AddMinutes(15), result.ExpiresAtUtc);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        Assert.Equal("ElsasaSis.Test", jwt.Issuer);
        Assert.Contains("ElsasaSis.Client.Test", jwt.Audiences);
        Assert.Equal(userId.ToString(), jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("teacher1", jwt.Claims.First(c => c.Type == "preferred_username").Value);
        Assert.Equal("Budi Santoso", jwt.Claims.First(c => c.Type == "name").Value);

        var roleClaims = jwt.Claims.Where(c => c.Type == "roles").Select(c => c.Value).ToArray();
        Assert.Equal([RolePermissionCatalog.Roles.SchoolAdmin], roleClaims);

        var permissionClaims = jwt.Claims.Where(c => c.Type == "permissions").Select(c => c.Value).ToArray();
        Assert.Contains(Permissions.Operations.Calendar.View, permissionClaims);
        Assert.Contains(Permissions.Operations.Calendar.Manage, permissionClaims);

        Assert.Equal(teacherId.ToString(), jwt.Claims.First(c => c.Type == "teacherId").Value);
        Assert.DoesNotContain(jwt.Claims, c => c.Type == "studentId");
        Assert.DoesNotContain(jwt.Claims, c => c.Type == "guardianId");
        Assert.DoesNotContain(jwt.Claims, c => c.Type == "org_unit_id");
    }
}
