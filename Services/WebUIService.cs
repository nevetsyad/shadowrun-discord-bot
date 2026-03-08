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

            _logger.LogInformation("Web UI service started successfully on port {Port}", _config.WebUI.Port);
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

        // Controllers
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
            });

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
                    Description = "API for Shadowrun Discord Bot GM Tools"
                });
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

        app.MapControllers();

        // Health check endpoint
        app.MapGet("/health", () => Results.Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow
        })).RequireRateLimiting("FixedWindow");

        // Root endpoint
        app.MapGet("/", () => Results.Redirect("/api-docs"));
    }
}

/// <summary>
/// API Controller base with common functionality
/// </summary>
[Microsoft.AspNetCore.Mvc.ApiController]
[Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
public abstract class BaseApiController : Microsoft.AspNetCore.Mvc.ControllerBase
{
    protected IActionResult Success(object data)
    {
        return Ok(new { success = true, data });
    }

    protected IActionResult Error(string message, int statusCode = 400)
    {
        return StatusCode(statusCode, new { success = false, error = message });
    }
}

/// <summary>
/// Characters API Controller
/// </summary>
[Microsoft.AspNetCore.Mvc.Route("api/characters")]
public class CharactersController : BaseApiController
{
    private readonly DatabaseService _db;

    public CharactersController(DatabaseService db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ulong? userId)
    {
        try
        {
            if (!userId.HasValue)
                return Error("User ID is required");

            var characters = await _db.GetUserCharactersAsync(userId.Value);
            return Success(characters);
        }
        catch (Exception ex)
        {
            return Error($"Failed to list characters: {ex.Message}", 500);
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        try
        {
            var character = await _db.GetCharacterAsync(id);
            if (character == null)
                return Error("Character not found", 404);

            return Success(character);
        }
        catch (Exception ex)
        {
            return Error($"Failed to get character: {ex.Message}", 500);
        }
    }
}

/// <summary>
/// Combat API Controller
/// </summary>
[Microsoft.AspNetCore.Mvc.Route("api/combat")]
public class CombatController : BaseApiController
{
    private readonly DatabaseService _db;

    public CombatController(DatabaseService db)
    {
        _db = db;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive([FromQuery] ulong channelId)
    {
        try
        {
            var session = await _db.GetActiveCombatSessionAsync(channelId);
            return Success(session ?? new { active = false });
        }
        catch (Exception ex)
        {
            return Error($"Failed to get active combat: {ex.Message}", 500);
        }
    }
}

/// <summary>
/// Dice API Controller
/// </summary>
[Microsoft.AspNetCore.Mvc.Route("api/dice")]
public class DiceController : BaseApiController
{
    private readonly DiceService _dice;

    public DiceController(DiceService dice)
    {
        _dice = dice;
    }

    [HttpPost("roll")]
    public IActionResult Roll([FromBody] DiceRollRequest request)
    {
        try
        {
            var result = _dice.ParseAndRoll(request.Notation);
            return Success(result);
        }
        catch (Exception ex)
        {
            return Error($"Failed to roll dice: {ex.Message}");
        }
    }

    [HttpPost("shadowrun")]
    public IActionResult ShadowrunRoll([FromBody] ShadowrunRollRequest request)
    {
        try
        {
            var result = _dice.RollShadowrun(request.PoolSize, request.TargetNumber);
            return Success(result);
        }
        catch (Exception ex)
        {
            return Error($"Failed to roll Shadowrun dice: {ex.Message}");
        }
    }
}

public record DiceRollRequest
{
    public string Notation { get; init; } = "1d6";
}

public record ShadowrunRollRequest
{
    public int PoolSize { get; init; }
    public int TargetNumber { get; init; } = 4;
}
