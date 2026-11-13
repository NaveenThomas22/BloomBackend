using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Bloom.API.BackgroundServices;
using Bloom.API.Configuration;
using Bloom.API.Middleware;
using Bloom.API.Services;
using Bloom.BLL.Extensions;
using Bloom.Core.Constants;
using Bloom.Core.Interfaces;
using Bloom.DAL.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting Bloom API — My CAT Journey...");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog
    builder.Services.AddSerilog((services, configuration) => configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"));

    // Add Controllers & JSON serialization
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });

    // Configure Application Defaults
    builder.Services.Configure<AppDefaultsOptions>(builder.Configuration.GetSection(AppDefaultsOptions.SectionName));
    builder.Services.Configure<WebPushOptions>(builder.Configuration.GetSection(WebPushOptions.SectionName));

    // JWT Options & Authentication
    var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
    builder.Services.Configure<JwtOptions>(jwtSection);
    var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

    var secretKey = jwtOptions.Secret;
    if (string.IsNullOrWhiteSpace(secretKey))
    {
        if (builder.Environment.IsProduction())
        {
            throw new InvalidOperationException("JWT Secret Key must be explicitly and securely configured via environment variables or secret manager in production.");
        }
        secretKey = "Bloom_Sage_Garden_Super_Secure_Secret_Key_2026_Cat_Preparation_Platform_Token!";
    }

    var keyBytes = Encoding.UTF8.GetBytes(secretKey);
    if (keyBytes.Length < 32)
    {
        throw new InvalidOperationException($"JWT Secret Key must be at least 256 bits (32 bytes) in length. Current length is {keyBytes.Length} bytes.");
    }

    if (string.IsNullOrWhiteSpace(jwtOptions.Issuer))
    {
        throw new InvalidOperationException("JWT Issuer must be configured.");
    }

    if (string.IsNullOrWhiteSpace(jwtOptions.Audience))
    {
        throw new InvalidOperationException("JWT Audience must be configured.");
    }

    if (jwtOptions.AccessTokenLifetimeMinutes <= 0)
    {
        throw new InvalidOperationException("JWT AccessTokenLifetimeMinutes must be greater than zero.");
    }

    if (jwtOptions.RefreshTokenLifetimeDays <= 0)
    {
        throw new InvalidOperationException("JWT RefreshTokenLifetimeDays must be greater than zero.");
    }

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

    builder.Services.AddAuthorization();

    // Rate Limiting for sensitive auth endpoints
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddFixedWindowLimiter("AuthRateLimit", opt =>
        {
            opt.PermitLimit = 30;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueLimit = 0;
        });
    });

    // HttpContext & Identity Isolation
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    // Layer Registrations
    builder.Services.AddDataAccessLayer(builder.Configuration);
    builder.Services.AddBusinessLogicLayer();
    builder.Services.AddHostedService<RevisionStatusUpdateBackgroundService>();
    builder.Services.AddHostedService<ExamAutoSubmitBackgroundService>();
    builder.Services.AddHostedService<NotificationSchedulerBackgroundService>();

    // Cross-Cutting Concerns
    builder.Services.AddBloomCors(builder.Configuration);
    builder.Services.AddBloomSwagger();

    var app = builder.Build();

    // Configure the HTTP Request Pipeline
    app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bloom API v1");
            c.RoutePrefix = "swagger";
            c.DocumentTitle = "Bloom API Documentation";
        });
    }

    app.UseRouting();

    app.UseCors(AppConstants.CorsPolicyName);

    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Health Check Endpoint
    app.MapHealthChecks("/health");

    // Apply pending database schema migrations
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var migrator = scope.ServiceProvider.GetRequiredService<IDatabaseMigrator>();
            await migrator.MigrateAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not run automated migrations during startup: {Message}", ex.Message);
        }
    }

    Log.Information("Bloom API initialized successfully with Authentication & Security Engine.");

    app.Run();
}
catch (HostAbortedException)
{
    throw;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Bloom API terminated unexpectedly during startup.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
