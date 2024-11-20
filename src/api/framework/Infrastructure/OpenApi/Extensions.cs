using System.Text.Json;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace FSH.Framework.Infrastructure.OpenApi;

internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Servers = new List<OpenApiServer>
        {
            new OpenApiServer
            {
                Url = "https://localhost:7000",
                Description = "Serveur de développement local"
            }
        };


        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (authenticationSchemes.Any(authScheme => authScheme.Name == IdentityConstants.BearerScheme))
        {
            var requirements = new Dictionary<string, OpenApiSecurityScheme>
            {
                ["bearerAuth"] = new()
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "JWT Authorization header using the Bearer scheme."
                }
            };

            var securityRequirement = new OpenApiSecurityRequirement
       {
           {
               new OpenApiSecurityScheme
               {
                   Reference = new OpenApiReference
                   {
                       Type = ReferenceType.SecurityScheme,
                       Id = "bearerAuth"
                   }
               },
               new string[] {}
           }
       };

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = requirements;
            document.SecurityRequirements.Add(securityRequirement);
        }
    }
}

public static class Extensions
{
    public static IServiceCollection ConfigureOpenApi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddEndpointsApiExplorer();
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        });

        services
            .AddApiVersioning(options =>
            {
                options.ReportApiVersions = true;
                options.DefaultApiVersion = new ApiVersion(1);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
            })
            .EnableApiVersionBinding();
        return services;
    }
    public static WebApplication UseOpenApi(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "docker")
        {
            app.MapOpenApi().AllowAnonymous();

            app.UseSwaggerUI(options =>
            {
                options.ConfigObject.Urls = new[]
                {
                    new UrlDescriptor
                    {
                        Name = "API v1",
                        Url = "/openapi/v1.json" // Forcer l'utilisation de 7000
                    }
                };

                options.DocExpansion(DocExpansion.None);
                options.DisplayRequestDuration();

                foreach (var description in app.DescribeApiVersions())
                {
                    options.SwaggerEndpoint(
                        $"/openapi/{description.GroupName}.json",
                        description.GroupName.ToUpperInvariant());
                }

            });

            app.MapScalarApiReference().AllowAnonymous();

        }
        return app;
    }
}
