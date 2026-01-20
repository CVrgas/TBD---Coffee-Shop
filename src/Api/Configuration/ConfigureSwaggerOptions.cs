using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Api.Configuration;

/// <summary>
/// Configures Swagger options for API versioning.
/// </summary>
/// <param name="provider">The API version description provider.</param>
public class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) : IConfigureOptions<SwaggerGenOptions>
{
    /// <summary>
    /// Configures the Swagger generation options.
    /// </summary>
    /// <param name="options">The Swagger generation options.</param>
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
        }
    }

    private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
    {
        var info = new OpenApiInfo()
        {
            Title = "My API",
            Version = description.ApiVersion.ToString(),
            Description = "An API designed for scalable versioning.",
        };
        
        if(description.IsDeprecated) info.Description += " This API version has been deprecated.";
        return info;
    }
}