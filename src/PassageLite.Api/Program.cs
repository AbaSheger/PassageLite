using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PassageLite.Application.Interfaces;
using PassageLite.Application.Services;
using PassageLite.Domain.Interfaces;
using PassageLite.Infrastructure;
using PassageLite.Infrastructure.Data;
using PassageLite.Infrastructure.Messaging;
using Serilog;
using Azure.Messaging.ServiceBus;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PassageLite API",
        Version = "v1",
        Description = "Access Control Management API"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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
            Array.Empty<string>()
        }
    });
});

// Configure DbContext (provider selectable via Database:Provider config, defaults to Postgres)
var databaseProvider = builder.Configuration["Database:Provider"] ?? "Postgres";
var useSqlServer = databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase);
var connectionString = useSqlServer
    ? builder.Configuration.GetConnectionString("SqlServerConnection")
    : builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (useSqlServer)
    {
        options.UseSqlServer(connectionString);
    }
    else
    {
        options.UseNpgsql(connectionString);
    }
});

// Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)) { KeyId = "passagelite-key" }
        };
    });

builder.Services.AddAuthorization();

// Register services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAreaService, AreaService>();

var azureServiceBusConnectionString = builder.Configuration["AzureServiceBus:ConnectionString"];
var accessGrantedQueueOrTopicName = builder.Configuration["AzureServiceBus:AccessGrantedQueueOrTopicName"];
builder.Services.Configure<AzureServiceBusOptions>(builder.Configuration.GetSection("AzureServiceBus"));

if (!string.IsNullOrWhiteSpace(azureServiceBusConnectionString) &&
    !string.IsNullOrWhiteSpace(accessGrantedQueueOrTopicName))
{
    builder.Services.AddSingleton(new ServiceBusClient(azureServiceBusConnectionString));
    builder.Services.AddScoped<IAccessEventPublisher, AzureServiceBusAccessEventPublisher>();
}
else
{
    builder.Services.AddScoped<IAccessEventPublisher, NoOpAccessEventPublisher>();
}

builder.Services.AddScoped<IAccessService, AccessService>();

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
    // Apply migrations and seed data in development
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DbSeeder.SeedAsync(context, logger);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Content(
    """
    <!doctype html>
    <html lang="en">
    <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <title>PassageLite API</title>
        <style>
            :root {
                color-scheme: light dark;
                font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
                color: #172033;
                background: #f5f7fb;
            }

            body {
                margin: 0;
                min-height: 100vh;
                display: grid;
                place-items: center;
                padding: 32px;
            }

            main {
                width: min(760px, 100%);
                background: #ffffff;
                border: 1px solid #d9e0ea;
                border-radius: 8px;
                padding: 40px;
                box-shadow: 0 18px 48px rgba(23, 32, 51, 0.08);
            }

            h1 {
                margin: 0 0 8px;
                font-size: clamp(2rem, 5vw, 3.25rem);
                line-height: 1;
            }

            p {
                margin: 0 0 28px;
                color: #4f5d75;
                font-size: 1.05rem;
            }

            ul {
                margin: 0 0 32px;
                padding-left: 20px;
                color: #273449;
                line-height: 1.8;
            }

            nav {
                display: flex;
                flex-wrap: wrap;
                gap: 12px;
            }

            a {
                color: #0f5bd8;
                font-weight: 650;
                text-decoration: none;
            }

            nav a {
                border: 1px solid #b8c7dc;
                border-radius: 6px;
                padding: 10px 14px;
                background: #f9fbff;
            }

            a:hover {
                text-decoration: underline;
            }

            @media (prefers-color-scheme: dark) {
                :root {
                    color: #edf2f7;
                    background: #111827;
                }

                main {
                    background: #172033;
                    border-color: #2b384f;
                    box-shadow: none;
                }

                p,
                ul {
                    color: #c7d2e2;
                }

                a {
                    color: #8bb8ff;
                }

                nav a {
                    background: #1d2940;
                    border-color: #3a4860;
                }
            }
        </style>
    </head>
    <body>
        <main>
            <h1>PassageLite API</h1>
            <p>.NET 8 Web API for access control management</p>
            <ul>
                <li>JWT authentication and role-based authorization</li>
                <li>SQL Server support</li>
                <li>Azure App Service deployment</li>
                <li>GitHub Actions CI/CD</li>
                <li>Azure Service Bus event publishing support</li>
            </ul>
            <nav aria-label="Project links">
                <a href="/health">Health</a>
                <a href="/swagger">Swagger</a>
                <a href="https://github.com/AbaSheger/PassageLite">GitHub repository</a>
            </nav>
        </main>
    </body>
    </html>
    """,
    "text/html"));

app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var payload = new
        {
            status = report.Status.ToString(),
            entries = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                exception = e.Value.Exception?.ToString()
            })
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
});

app.Run();

// Make the implicit Program class public for testing
public partial class Program { }
