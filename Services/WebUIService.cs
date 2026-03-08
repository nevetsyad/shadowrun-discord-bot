using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

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

        // Health check endpoint
        app.MapGet("/health", () => Results.Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        })).RequireRateLimiting("FixedWindow");

        // Note: Dashboard is served by DashboardController at "/" route
    }
}
