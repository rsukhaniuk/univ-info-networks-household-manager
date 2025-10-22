using HouseholdManager.Api.Services;
using HouseholdManager.Application.Interfaces.Services;
using HouseholdManager.Application;
using HouseholdManager.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container

// Api Layer Services (Infrastructure implementations at Api layer)
builder.Services.AddScoped<IFileSystemService, FileSystemService>();

// Application Layer
builder.Services.AddApplication();

// Infrastructure Layer  
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// TODO: Add Authentication (Auth0 JWT)
// builder.Services.AddAuthentication(...)
// builder.Services.AddAuthorization(...)

// TODO: Add CORS

// builder.Services.AddCors(...)

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
