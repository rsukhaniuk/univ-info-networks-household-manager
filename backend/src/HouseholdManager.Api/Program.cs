using FluentValidation;
using HouseholdManager.Api.Configuration;
using HouseholdManager.Api.Filters;
using HouseholdManager.Api.Middleware;
using HouseholdManager.Api.Services;
using HouseholdManager.Application;
using HouseholdManager.Application.Interfaces.Services;
using HouseholdManager.Infrastructure;
using HouseholdManager.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Auth0Settings = HouseholdManager.Infrastructure.Configuration.Auth0Settings;

var builder = WebApplication.CreateBuilder(args);

// Configuration

// Bind Auth0 settings
var auth0Settings = builder.Configuration.GetSection("Auth0").Get<Auth0Settings>()
    ?? throw new InvalidOperationException("Auth0 configuration is missing in appsettings.json");

builder.Services.Configure<Auth0Settings>(builder.Configuration.GetSection("Auth0"));

// Authentication & Authorization (Auth0 JWT)

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://{auth0Settings.Domain}/";
        options.Audience = auth0Settings.Audience;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = "https://householdmanager.com/roles",
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };

        // Auth0 role mapping and event handlers
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var claimsIdentity = context.Principal?.Identity as ClaimsIdentity;
                if (claimsIdentity == null) return Task.CompletedTask;

                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();

                try
                {
                    // Extract Auth0 roles from custom claim
                    var rolesClaim = context.Principal?
                        .FindFirst("https://householdmanager.com/roles");

                    if (rolesClaim != null && !string.IsNullOrEmpty(rolesClaim.Value))
                    {
                        var rolesClaimValue = rolesClaim.Value.Trim();

                        // Check if it's a JSON array (starts with '[')
                        if (rolesClaimValue.StartsWith('['))
                        {
                            // Deserialize as JSON array
                            var roles = JsonSerializer.Deserialize<string[]>(rolesClaimValue);

                            if (roles != null && roles.Length > 0)
                            {
                                foreach (var role in roles)
                                {
                                    claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role));
                                    logger.LogDebug("Added role claim (from JSON array): {Role}", role);
                                }
                            }
                        }
                        else
                        {
                            // It's a simple string value (single role)
                            claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, rolesClaimValue));
                            logger.LogDebug("Added role claim (from string): {Role}", rolesClaimValue);
                        }
                    }

                    // Extract email from custom claim
                    var email = context.Principal?
                        .FindFirst("https://householdmanager.com/email")?.Value;

                    if (!string.IsNullOrEmpty(email))
                    {
                        claimsIdentity.AddClaim(new Claim(ClaimTypes.Email, email));
                    }

                    var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? context.Principal?.FindFirst("sub")?.Value;

                    logger.LogInformation(
                        "JWT token validated for user: {UserId}, Email: {Email}, Roles: {Roles}",
                        userId,
                        email,
                        rolesClaim?.Value ?? "none");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error mapping Auth0 roles to claims");
                }

                return Task.CompletedTask;
            },

            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception,
                    "JWT authentication failed: {Message}",
                    context.Exception.Message);
                return Task.CompletedTask;
            },

            OnChallenge = context =>
            {
                // Only log if there's an actual error (not just null challenge)
                if (!string.IsNullOrEmpty(context.Error) || !string.IsNullOrEmpty(context.ErrorDescription))
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<Program>>();
                    logger.LogWarning(
                        "JWT authentication challenge: {Error}, {ErrorDescription}",
                        context.Error,
                        context.ErrorDescription);
                }
                return Task.CompletedTask;
            }
        };
    });

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    // Policy for SystemAdmin role (from Auth0)
    options.AddPolicy("SystemAdminOnly", policy =>
        policy.RequireRole("SystemAdmin"));

    // Default policy - require authenticated user
    options.AddPolicy("AuthenticatedUser", policy =>
        policy.RequireAuthenticatedUser());
});

// Application Layers

// Api Layer Services (Infrastructure implementations at Api layer)
builder.Services.AddScoped<IFileSystemService, FileSystemService>();

// Add HttpContextAccessor (required by CalendarExportService)
builder.Services.AddHttpContextAccessor();

// Application Layer  
builder.Services.AddApplication();

// Infrastructure Layer  
builder.Services.AddInfrastructure(builder.Configuration);

// Validation & Filtering

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

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// SWAGGER WITH AUTH0 OAUTH2

Console.WriteLine($"[Auth0] Domain={auth0Settings.Domain}, Audience={auth0Settings.Audience}, ClientId={(string.IsNullOrWhiteSpace(auth0Settings.ClientId) ? "<EMPTY>" : auth0Settings.ClientId)}");

builder.Services.AddSwaggerWithAuth0(auth0Settings);

// CORS (allow Angular frontend)

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200", "https://localhost:4200", "https://127.0.0.1:4200"];

builder.Services.AddCors(options => 
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Build & Configure Middleware Pipeline

var app = builder.Build();

// Exception Handling Middleware (must be first)
app.UseExceptionHandling();

app.UseSwaggerWithAuth0(auth0Settings);

app.UseHttpsRedirection();

// Serve static files (for photo uploads in wwwroot/uploads)
app.UseStaticFiles();

// CORS (must be before Authentication/Authorization)
app.UseCors("AllowAngular");

// Authentication & Authorization (order matters!)
app.UseAuthentication();
app.UseMiddleware<UserSyncMiddleware>();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await HouseholdManager.Infrastructure.DependencyInjection.SeedDataAsync(services);
}

app.Run();