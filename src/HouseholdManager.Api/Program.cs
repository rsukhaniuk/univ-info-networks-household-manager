using FluentValidation;
using HouseholdManager.Api.Filters;
using HouseholdManager.Api.Middleware;
using HouseholdManager.Api.Services;
using HouseholdManager.Application;
using HouseholdManager.Application.Interfaces.Services;
using HouseholdManager.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Api Layer Services (Infrastructure implementations at Api layer)
builder.Services.AddScoped<IFileSystemService, FileSystemService>();

// Application Layer
builder.Services.AddApplication();

// Infrastructure Layer  
builder.Services.AddInfrastructure(builder.Configuration);

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<
    HouseholdManager.Application.Validators.Household.UpsertHouseholdRequestValidator>();

// ValidationFilter
builder.Services.AddScoped<ValidationFilter>();

// Controllers with FluentValidation
builder.Services.AddControllers(options =>
{
    // Add ValidationFilter globally
    options.Filters.Add<ValidationFilter>();
});

builder.Services.Configure<ApiBehaviorOptions>(o =>
{
    o.SuppressModelStateInvalidFilter = true;
});

// OpenAPI / Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Household Manager API",
        Version = "v1",
        Description = "REST API for Household Task Management System",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Household Manager Team",
            Email = "support@householdmanager.com"
        }
    });

    // Include XML comments for Swagger documentation
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    // Add ProblemDetails schemas
    options.UseAllOfToExtendReferenceSchemas();
});

// TODO: Add Authentication (Auth0 JWT) - Lab 3 Phase 2
// builder.Services.AddAuthentication(...)
// builder.Services.AddAuthorization(...)

// CORS (allow all for development)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});


var app = builder.Build();

// Exception Handling Middleware
app.UseExceptionHandling();

// Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Household Manager API v1");
        options.RoutePrefix = "swagger"; // Swagger UI at /swagger
        options.DisplayRequestDuration();
    });
}

app.UseHttpsRedirection();

// CORS
app.UseCors("AllowAll");

// TODO Lab 3 Phase 2: Enable after Auth0 integration
// app.UseAuthentication();
// app.UseAuthorization();

app.MapControllers();

app.Run();
