using HouseholdManager.Infrastructure.Configuration;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace HouseholdManager.Api.Configuration
{
    /// <summary>
    /// Swagger configuration with Auth0 OAuth2 integration
    /// </summary>
    public static class SwaggerConfiguration
    {
        public static IServiceCollection AddSwaggerWithAuth0(
            this IServiceCollection services,
            Auth0Settings auth0Settings,
            string swaggerBaseUrl = "https://localhost:7246")
        {
            services.AddEndpointsApiExplorer();

            services.AddSwaggerGen(options =>
            {
                // API Info
                var returnToUrl = Uri.EscapeDataString($"{swaggerBaseUrl}/swagger");
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Household Manager API",
                    Version = "v1",
                    Description = $"""
                        REST API for Household Task Management System with Auth0 authentication.

                        **Authentication (OAuth2)**
                        1. Click 'Authorize' button below
                        2. Login with your Auth0 credentials
                        3. Your JWT token will be automatically added to all requests

                        **To switch accounts:**
                        - [Click here to logout from Auth0](https://{auth0Settings.Domain}/v2/logout?returnTo={returnToUrl}&client_id={auth0Settings.ClientId})
                        - Then refresh this page and click Authorize again

                        **Alternative: Manual Token (recommended for testing)**
                        1. Use the 'Bearer' authorization section instead of 'oauth2'
                        2. Get tokens from [Auth0 Dashboard → APIs → Test](https://{auth0Settings.Domain}/dashboard)
                        3. Paste different tokens to test different users

                        **Roles**
                        - **SystemAdmin** – Full system access  
                        - **User** – Access to own households and tasks
                        """,
                    Contact = new OpenApiContact
                    {
                        Name = "Household Manager Team",
                        Email = "support@householdmanager.com"
                    },
                    License = new OpenApiLicense
                    {
                        Name = "MIT License",
                        Url = new Uri("https://opensource.org/licenses/MIT")
                    }
                });

                // AUTH0 OAUTH2 FLOW (Authorization Code + PKCE)

                var auth0Domain = $"https://{auth0Settings.Domain}";

                options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Description = "Auth0 OAuth2 authentication. Click 'Authorize' and login with your credentials.",

                    Flows = new OpenApiOAuthFlows
                    {
                        // Authorization Code Flow with PKCE (recommended for SPAs and Swagger)
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri($"{auth0Domain}/authorize"),
                            TokenUrl = new Uri($"{auth0Domain}/oauth/token"),

                            Scopes = new Dictionary<string, string>
                            {
                                { "openid", "OpenID Connect" },
                                { "profile", "User profile information" },
                                { "email", "User email address" },
                                { "offline_access", "Refresh token" }
                            }
                        },

                        // Implicit Flow (fallback for older clients)
                        Implicit = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri($"{auth0Domain}/authorize"),

                            Scopes = new Dictionary<string, string>
                            {
                                { "openid", "OpenID Connect" },
                                { "profile", "User profile information" },
                                { "email", "User email address" }
                            }
                        }
                    }
                });

                // Apply OAuth2 security globally
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "oauth2"
                            }
                        },
                        new[] { "openid", "profile", "email" }
                    }
                });

                // FALLBACK: MANUAL BEARER TOKEN (for testing)

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = """
                        **Manual JWT token entry (RECOMMENDED for testing with multiple accounts)**

                        If OAuth2 login doesn't work or you want to quickly switch between users, use this method:

                        1. Get token from Auth0 Dashboard → APIs → Your API → Test tab  
                        2. Click 'Try' for different users to get their tokens
                        3. Paste token here (without 'Bearer' prefix)

                        Example:  
                        `eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...`

                        **Benefits:**
                        - No need to logout/login  
                        - Quick switching between test users  
                        - Works even when OAuth2 cookies are cached
                        """
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                        Array.Empty<string>()
                    }
                });

                // XML DOCUMENTATION

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
                }

                // ADDITIONAL SETTINGS

                options.UseAllOfToExtendReferenceSchemas();
                options.EnableAnnotations();

                // Group endpoints by tags
                options.TagActionsBy(api => new[]
                {
                    api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] ?? "Default"
                });

                options.DocInclusionPredicate((docName, apiDesc) => true);

                // Custom operation IDs
                options.CustomOperationIds(apiDesc =>
                {
                    var controllerName = apiDesc.ActionDescriptor.RouteValues["controller"];
                    var actionName = apiDesc.ActionDescriptor.RouteValues["action"];
                    return $"{controllerName}_{actionName}";
                });
            });

            return services;
        }

        public static IApplicationBuilder UseSwaggerWithAuth0(
            this IApplicationBuilder app,
            Auth0Settings auth0Settings)
        {
            app.UseSwagger();

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Household Manager API v1");
                options.RoutePrefix = "swagger";

                // AUTH0 OAUTH2 CONFIGURATION

                options.OAuthClientId(auth0Settings.ClientId);
                options.OAuthAppName("Household Manager API");
                options.OAuthUsePkce(); // Enable PKCE for security

                // Additional OAuth settings - SHOW ACCOUNT SELECTION
                options.OAuthAdditionalQueryStringParams(new Dictionary<string, string>
                {
                    { "audience", auth0Settings.Audience },
                    { "prompt", "select_account" } // Show account picker (allows switching accounts without re-login)
                });

                // UI SETTINGS

                options.DisplayRequestDuration();
                options.EnableDeepLinking();
                options.EnableFilter();
                options.ShowExtensions();

                // Collapse schemas by default
                options.DefaultModelsExpandDepth(-1);

                // Expand operations by tag
                options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);

                // Dark theme (optional)
                // options.InjectStylesheet("/swagger-ui/custom.css");


            });

            return app;
        }
    }
}
