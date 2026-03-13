using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using ShadowrunDiscordBot.HealthChecks;
using HealthChecks.UI.Client;

namespace ShadowrunDiscordBot.Services;

/// <summary>
/// Web UI service for GM tools with authentication and rate limiting
/// Hosts the dashboard and API endpoints
/// </summary>
public class WebUIService : IHostedService
{
    private readonly BotConfig _config;
    private readonly ILogger<WebUIService> _logger;
    private readonly IServiceProvider _services;
    private WebApplication? _app;

    public WebUIService(
        BotConfig config,
        IServiceProvider services,
        ILogger<WebUIService> logger)
    {
        _config = config;
        _services = services;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting Web UI service on port {Port}...", _config.WebUI.Port);

            var builder = WebApplication.CreateBuilder();

            // Configure Kestrel
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(_config.WebUI.Port);
            });

            // Configure services
            ConfigureServices(builder.Services);

            _app = builder.Build();

            // Configure middleware
            ConfigureMiddleware(_app);

            // Start the web server
            await _app.StartAsync(cancellationToken);

            _logger.LogInformation("==========================================");
            _logger.LogInformation("🌐 Web UI Running at http://localhost:{Port}", _config.WebUI.Port);
            _logger.LogInformation("📚 API Docs at http://localhost:{Port}/api-docs", _config.WebUI.Port);
            _logger.LogInformation("==========================================");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Web UI service");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_app != null)
        {
            _logger.LogInformation("Stopping Web UI service...");
            await _app.StopAsync(cancellationToken);
            await _app.DisposeAsync();
            _logger.LogInformation("Web UI service stopped");
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_config.WebUI.JwtSecret))
            };
        });

        // Rate limiting
        services.AddRateLimiter(options =>
        {
            options.AddPolicy("FixedWindow", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = _config.RateLimiting.PermitLimit,
                        Window = _config.RateLimiting.Window,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10
                    }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = 429;
                await context.HttpContext.Response.WriteAsync(
                    "Too many requests. Please try again later.",
                    cancellationToken);
            };
        });

        // CORS
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });

        // Controllers - scan assembly for all controllers
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
            });

        // Add required services for controllers
        services.AddScoped<ShadowrunDbContext>(sp => 
        {
            var dbService = _services.GetRequiredService<DatabaseService>();
            // We need to get the internal context - use reflection or expose it
            // For now, create a new context with the same config
            var config = sp.GetRequiredService<BotConfig>();
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ShadowrunDbContext>()
                .UseSqlite(config.Database.ConnectionString)
                .Options;
            return new ShadowrunDbContext(options);
        });
        
        services.AddScoped<DiceService>(sp => _services.GetRequiredService<DiceService>());
        services.AddScoped<DatabaseService>(sp => _services.GetRequiredService<DatabaseService>());
        services.AddScoped<CharacterService>();
        services.AddScoped<CombatService>();
        services.AddScoped<MatrixService>();
        services.AddScoped<DashboardService>();

        // Swagger
        if (_config.WebUI.EnableSwagger)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Shadowrun Discord Bot API",
                    Version = "v1",
                    Description = "API for Shadowrun Discord Bot GM Tools - Character management, Combat tracking, Dice rolling"
                });
                
                // Include XML comments if available
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });
        }

        // Health Checks
        services.AddHealthChecks()
            .AddSqlite(_config.Database.ConnectionString, name: "database", tags: new[] { "db", "sqlite" })
            .AddCheck<DiscordHealthCheck>("discord", tags: new[] { "discord", "api" });

        // Health Checks UI
        services.AddHealthChecksUI(settings =>
        {
            settings.SetEvaluationTimeInSeconds(30);
            settings.MaximumHistoryEntriesPerEndpoint(50);
        }).AddInMemoryStorage();
    }

    private void ConfigureMiddleware(WebApplication app)
    {
        // Swagger
        if (_config.WebUI.EnableSwagger)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Shadowrun Bot API v1");
                c.RoutePrefix = "api-docs";
            });
        }

        // HTTPS redirection (disabled for local development)
        // app.UseHttpsRedirection();

        app.UseCors();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        // Map all controllers
        app.MapControllers();

        // Health check endpoints
        // Main health check with detailed response (for monitoring)
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        }).RequireRateLimiting("FixedWindow");

        // Readiness probe (K8s-style) - checks all dependencies
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("db") || check.Tags.Contains("discord"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        }).RequireRateLimiting("FixedWindow");

        // Liveness probe (K8s-style) - basic "is the app running" check
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false // Skip all checks, just return healthy if app is running
        }).RequireRateLimiting("FixedWindow");

        // K8s-style liveness endpoint
        app.MapHealthChecks("/healthz", new HealthCheckOptions
        {
            Predicate = _ => false
        }).RequireRateLimiting("FixedWindow");

        // Health Checks UI dashboard
        app.MapHealthChecksUI(options =>
        {
            options.UIPath = "/health-ui";
            options.ApiPath = "/health-ui-api";
        });

        // Note: Dashboard is served by DashboardController at "/" route
    }
}
