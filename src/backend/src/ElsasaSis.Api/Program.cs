using System.Text;
using ElsasaSis.Api.Extensions;
using ElsasaSis.Api.Infrastructure;
using ElsasaSis.Api.Middleware;
using ElsasaSis.Api.Security;
using ElsasaSis.Contracts.Academic;
using ElsasaSis.Database;
using ElsasaSis.Modules.Acd;
using ElsasaSis.Modules.Auth;
using ElsasaSis.Modules.Auth.Infrastructure;
using ElsasaSis.Modules.Dev;
using ElsasaSis.Modules.Fin;
using ElsasaSis.Modules.Opr;
using ElsasaSis.Modules.Std;
using ElsasaSis.Modules.Tch;
using ElsasaSis.SharedKernel.Events;
using ElsasaSis.SharedKernel.Persistence;
using ElsasaSis.SharedKernel.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using AuthPolicies = ElsasaSis.Modules.Auth.Policies;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

builder.Services.AddHttpContextAccessor();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblies(
        typeof(AuthAssemblyMarker).Assembly,
        typeof(OprAssemblyMarker).Assembly,
        typeof(StdAssemblyMarker).Assembly,
        typeof(TchAssemblyMarker).Assembly,
        typeof(AcdAssemblyMarker).Assembly,
        typeof(FinAssemblyMarker).Assembly,
        typeof(DevAssemblyMarker).Assembly));

builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddOprModule();
builder.Services.AddStdModule();
builder.Services.AddTchModule();
builder.Services.AddAcdModule();
builder.Services.AddFinModule();
builder.Services.AddDevModule();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = string.IsNullOrWhiteSpace(jwtOptions.SigningKey)
                ? null
                : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "preferred_username",
            RoleClaimType = "roles"
        };
    });

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in ElsasaSis.Modules.Auth.Permissions.All)
    {
        options.AddPolicy(permission, policy =>
            policy.RequireAssertion(ctx =>
                ctx.User.Claims
                    .Where(c => c.Type == "permissions")
                    .Any(c => string.Equals(c.Value, permission, StringComparison.Ordinal))));
    }

    options.AddPolicy(ElsasaSis.Modules.Auth.Permissions.Academic.Monitoring.View, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permissions", ElsasaSis.Modules.Auth.Permissions.Academic.Monitoring.View));

    options.AddPolicy(ElsasaSis.Modules.Auth.Permissions.Students.Children.View, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permissions", ElsasaSis.Modules.Auth.Permissions.Students.Children.View));

    static bool HasAnyPermission(AuthorizationHandlerContext ctx, params string[] permissions) =>
        ctx.User.Claims
            .Where(c => c.Type == "permissions")
            .Any(c => permissions.Contains(c.Value, StringComparer.Ordinal));

    options.AddPolicy(AuthPolicies.Students.Admissions.DecideOrApprove, policy =>
        policy.RequireAssertion(ctx => HasAnyPermission(ctx,
            ElsasaSis.Modules.Auth.Permissions.Students.Admissions.Decide,
            ElsasaSis.Modules.Auth.Permissions.Students.Admissions.Approve)));

    options.AddPolicy(AuthPolicies.Students.DirectoryOrOwnRoster, policy =>
        policy.RequireAssertion(ctx => HasAnyPermission(ctx,
            ElsasaSis.Modules.Auth.Permissions.Students.Directory.View,
            ElsasaSis.Modules.Auth.Permissions.Students.Directory.Manage,
            ElsasaSis.Modules.Auth.Permissions.Students.Roster.ViewOwn)));

    options.AddPolicy(AuthPolicies.Operations.Calendar.ViewOrManage, policy =>
        policy.RequireAssertion(ctx => HasAnyPermission(ctx,
            ElsasaSis.Modules.Auth.Permissions.Operations.Calendar.View,
            ElsasaSis.Modules.Auth.Permissions.Operations.Calendar.Manage)));

    options.AddPolicy(AuthPolicies.Operations.Rooms.ViewOrManage, policy =>
        policy.RequireAssertion(ctx => HasAnyPermission(ctx,
            ElsasaSis.Modules.Auth.Permissions.Operations.Rooms.View,
            ElsasaSis.Modules.Auth.Permissions.Operations.Rooms.Manage)));

    options.AddPolicy(AuthPolicies.Operations.Announcements.ViewOrManage, policy =>
        policy.RequireAssertion(ctx => HasAnyPermission(ctx,
            ElsasaSis.Modules.Auth.Permissions.Operations.Announcements.View,
            ElsasaSis.Modules.Auth.Permissions.Operations.Announcements.Create,
            ElsasaSis.Modules.Auth.Permissions.Operations.Announcements.Publish)));

    options.AddPolicy(AuthPolicies.Teaching.Assignments.ViewOrOversight, policy =>
        policy.RequireAssertion(ctx => HasAnyPermission(ctx,
            ElsasaSis.Modules.Auth.Permissions.Teaching.Assignments.View,
            ElsasaSis.Modules.Auth.Permissions.Teaching.Assignments.Manage,
            ElsasaSis.Modules.Auth.Permissions.Teaching.Assignments.Oversight)));

    options.AddPolicy(AuthPolicies.Operations.Timetables.ViewOrManageOrOwn, policy =>
        policy.RequireAssertion(ctx => HasAnyPermission(ctx,
            ElsasaSis.Modules.Auth.Permissions.Operations.Timetables.View,
            ElsasaSis.Modules.Auth.Permissions.Operations.Timetables.Manage,
            ElsasaSis.Modules.Auth.Permissions.Operations.Timetables.ViewOwn)));

    options.AddPolicy(AuthPolicies.Academic.Curriculum.ViewOrManage, policy =>
        policy.RequireAssertion(ctx => HasAnyPermission(ctx,
            ElsasaSis.Modules.Auth.Permissions.Academic.Curriculum.View,
            ElsasaSis.Modules.Auth.Permissions.Academic.Curriculum.Manage)));

    options.AddPolicy(AuthPolicies.Academic.Attendance.ViewOrRecord, policy =>
        policy.RequireAssertion(ctx => HasAnyPermission(ctx,
            ElsasaSis.Modules.Auth.Permissions.Academic.Attendance.View,
            ElsasaSis.Modules.Auth.Permissions.Academic.Attendance.Record)));

    options.AddPolicy(AuthPolicies.Academic.Scorebook.ViewOrManage, policy =>
        policy.RequireAssertion(ctx => HasAnyPermission(ctx,
            ElsasaSis.Modules.Auth.Permissions.Academic.Scorebook.View,
            ElsasaSis.Modules.Auth.Permissions.Academic.Scorebook.Manage)));

    options.AddPolicy(AuthPolicies.Academic.ReportCards.ViewOrManage, policy =>
        policy.RequireAssertion(ctx => HasAnyPermission(ctx,
            ElsasaSis.Modules.Auth.Permissions.Academic.ReportCards.View,
            ElsasaSis.Modules.Auth.Permissions.Academic.ReportCards.Generate,
            ElsasaSis.Modules.Auth.Permissions.Academic.ReportCards.ProvideNotes,
            ElsasaSis.Modules.Auth.Permissions.Academic.ReportCards.Remarks,
            ElsasaSis.Modules.Auth.Permissions.Academic.ReportCards.Finalize,
            ElsasaSis.Modules.Auth.Permissions.Academic.ReportCards.Override)));

    options.AddPolicy(AuthPolicies.Development.Pastoral.RecordOrView, policy =>
        policy.RequireAssertion(ctx => HasAnyPermission(ctx,
            ElsasaSis.Modules.Auth.Permissions.Development.Behaviour.Record,
            ElsasaSis.Modules.Auth.Permissions.Development.Discipline.Record,
            ElsasaSis.Modules.Auth.Permissions.Development.Discipline.Escalate,
            ElsasaSis.Modules.Auth.Permissions.Development.Achievements.Record,
            ElsasaSis.Modules.Auth.Permissions.Development.Dossier.View)));

    options.AddPolicy(AuthPolicies.Finance.Cashier.Manage, policy =>
        policy.RequireAssertion(ctx => HasAnyPermission(ctx,
            ElsasaSis.Modules.Auth.Permissions.Finance.Payments.Record,
            ElsasaSis.Modules.Auth.Permissions.Finance.Deposits.Allocate,
            ElsasaSis.Modules.Auth.Permissions.Finance.Statements.View)));

    options.AddPolicy(AuthPolicies.Finance.Reports.View, policy =>
        policy.RequireAssertion(ctx => HasAnyPermission(ctx,
            ElsasaSis.Modules.Auth.Permissions.Finance.Reports.View)));

    options.AddPolicy(AuthPolicies.Students.BillingLookup, policy =>
        policy.RequireAssertion(ctx => HasAnyPermission(ctx,
            ElsasaSis.Modules.Auth.Permissions.Finance.Payments.Record,
            ElsasaSis.Modules.Auth.Permissions.Finance.Billing.Generate,
            ElsasaSis.Modules.Auth.Permissions.Students.Directory.View)));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ElsasaSis API",
        Version = "v1"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter JWT Bearer token",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = JwtBearerDefaults.AuthenticationScheme
        }
    };

    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    var connectionString = app.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        var adminPassword = app.Configuration["Seed:AdminPassword"];
        await DatabaseMigrator.MigrateAndSeedAsync(connectionString, adminPassword, isDevelopment: true);
    }
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .AllowAnonymous();

app.MapDiscoveredEndpoints(
    typeof(AuthAssemblyMarker).Assembly,
    typeof(OprAssemblyMarker).Assembly,
    typeof(StdAssemblyMarker).Assembly,
    typeof(TchAssemblyMarker).Assembly,
    typeof(AcdAssemblyMarker).Assembly,
    typeof(FinAssemblyMarker).Assembly,
    typeof(DevAssemblyMarker).Assembly,
    typeof(Program).Assembly);

app.Run();

public partial class Program;
