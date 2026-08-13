using System.Text.Json.Serialization;
using ProgramDesigner.Api.Mapping;
using ProgramDesigner.Core.Repositories;
using ProgramDesigner.Core.Services;
using ProgramDesigner.Core.Validators;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
    });

builder.Services.AddSingleton<IEducationProgramRepository, InMemoryEducationProgramRepository>();
builder.Services.AddSingleton<ProgramMapper>();

// Add validation services
builder.Services.AddTransient<PrerequisiteValidator>();
builder.Services.AddTransient<ReachabilityValidator>();
builder.Services.AddTransient<ProgramValidationService>();
builder.Services.AddTransient<ProgramSimulationService>();

// DEV-ONLY CORS policy: origins are read from Cors:AllowedOrigins in appsettings.
// This is intentionally permissive for local development. Before deploying to
// production, replace this with a restrictive named policy or remove it entirely.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevFrontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

// Apply CORS before routing so preflight OPTIONS requests are handled correctly.
app.UseCors("DevFrontend");

app.MapControllers();

app.Run();

// Make the implicit Program class public so test projects can access it
public partial class Program { }
