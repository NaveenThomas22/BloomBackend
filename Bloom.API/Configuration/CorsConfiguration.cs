using Bloom.Core.Constants;

namespace Bloom.API.Configuration;

public static class CorsConfiguration
{
    public static IServiceCollection AddBloomCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
            ?? new[] { "http://localhost:5173", "http://localhost:3000" };

        services.AddCors(options =>
        {
            options.AddPolicy(AppConstants.CorsPolicyName, policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        return services;
    }
}

