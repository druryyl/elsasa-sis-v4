using ElsasaSis.Modules.Auth.Domain;
using ElsasaSis.Modules.Auth.Features.Auth.GetMe;
using ElsasaSis.Modules.Auth.Features.Auth.Login;
using ElsasaSis.Modules.Auth.Features.Auth.Logout;
using ElsasaSis.Modules.Auth.Features.Auth.Refresh;
using ElsasaSis.Modules.Auth.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ElsasaSis.Modules.Auth;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<PasswordHasher<object>>();

        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IRefreshTokenService, RefreshTokenService>();
        services.AddSingleton<RefreshTokenCookieService>();

        services.AddScoped<IAuthRepository, AuthRepository>();

        services.AddScoped<LoginHandler>();
        services.AddScoped<RefreshHandler>();
        services.AddScoped<LogoutHandler>();
        services.AddScoped<GetMeHandler>();

        services.AddScoped<IValidator<LoginRequest>, LoginValidator>();

        return services;
    }
}
