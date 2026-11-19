using ElsasaSis.Modules.Auth.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.Modules.Auth.Features.Auth.GetMe;

public sealed class GetMeHandler
{
    private readonly IAuthRepository _authRepository;
    private readonly ICurrentUser _currentUser;

    public GetMeHandler(IAuthRepository authRepository, ICurrentUser currentUser)
    {
        _authRepository = authRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<AuthUserDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty)
        {
            return Result<AuthUserDto>.Failure(AuthErrors.Unauthorized);
        }

        var user = await _authRepository.GetUserByIdAsync(_currentUser.UserId, cancellationToken);
        if (user is null)
        {
            return Result<AuthUserDto>.Failure(AuthErrors.Unauthorized);
        }

        if (!user.IsActive)
        {
            return Result<AuthUserDto>.Failure(AuthErrors.InactiveUser);
        }

        var permissions = RolePermissionCatalog.GetPermissionsForRoles(user.Roles);
        return Result<AuthUserDto>.Success(AuthUserDto.FromDomain(user, permissions));
    }
}
