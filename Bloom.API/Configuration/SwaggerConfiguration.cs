using Microsoft.OpenApi;

namespace Bloom.API.Configuration;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddBloomSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Bloom API — My CAT Journey",
                Version = "v1",
                Description = "A tranquil, mobile-first CAT preparation, 24-hour life management, and productivity platform."
            });

            c.DescribeAllParametersInCamelCase();

            // Add JWT Bearer Security Definition
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Enter JWT Bearer token format: Bearer {your token}",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });

            c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer"),
                    new List<string>()
                }
            });
        });

        return services;
    }
}
