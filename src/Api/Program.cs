using Api.Common;
using Api.Configuration;
using Api.Middlewares;
using Api.Modules.Auth;
using Api.Modules.Catalog;
using Api.Modules.Inventory;
using Api.Modules.Order;
using Api.Modules.Payment;
using Application.Common.Abstractions.Envelope;
using Asp.Versioning;
using Infrastructure;
using Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

builder.AddCustomHealthCheck();

builder.Services.AddApiVersioning(opts =>
    {
        opts.DefaultApiVersion = new ApiVersion(1);
        opts.ReportApiVersions = true;
        opts.AssumeDefaultVersionWhenUnspecified = true;
        opts.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("X-api-version"));
    })
    .AddApiExplorer(opts =>
    {
        opts.GroupNameFormat = "'v'VVV";
        opts.SubstituteApiVersionInUrl = true;
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter your JWT token below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            []
        }
    });
});

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddCatalog();
builder.Services.AddProductCategory();
builder.Services.AddInventory();
builder.Services.AddAuth();
builder.Services.AddOrder();
builder.Services.AddPayment();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<Infrastructure.Persistence.MyDbContext>();
    
    if (app.Environment.IsDevelopment())
    {
        if (context.Database.HasPendingModelChanges())
        {
            Console.WriteLine("⚠️ WARNING: Your C# entities have changed, but no migration has been created!");
            Console.WriteLine("   Run 'dotnet ef migrations add <Name>' to fix this.");
        }
        
        if (context.Database.GetPendingMigrations().Any())
        {
            Console.WriteLine("⚠️ WARNING: You have pending migrations not applied to the database!");
            Console.WriteLine("   Run 'dotnet ef database update' to fix this.");
        }
    }
}

app.UseMiddleware<ExceptionMiddleware>(); // Global Error Handling

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication(); // Decodes the JWT
app.UseAuthorization();  // Checks the Role claims

app.UseInfrastructureLogging(); // Enriched serilog.

app.UseOutputCache();

var versionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
    .HasApiVersion(new ApiVersion(2))
    .ReportApiVersions()
    .Build();

var routes = app.MapGroup("/api/v{version:apiVersion}")
    .WithApiVersionSet(versionSet)
    .AddEndpointFilter<EnvelopeFilter>() // map Envelope => IResult
    .RequireRateLimiting("per-user");

routes.MapAuth();
routes.MapProduct();
routes.MapProductCategory();
routes.MapInventory();
routes.MapOrder();
routes.MapPayment();

// Swagger.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var descriptions = app.DescribeApiVersions();
        foreach (var description in descriptions)
        {
            var url = $"/swagger/{description.GroupName}/swagger.json";
            var name = description.GroupName.ToUpperInvariant();
            
            options.SwaggerEndpoint(url, name);
        }
        
        options.RoutePrefix = string.Empty;
    });
}

app.MapCustomHealthCheck();

app.Run();

public partial class Program { }