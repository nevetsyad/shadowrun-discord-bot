using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShadowrunDiscordBot.Services;

namespace ShadowrunDiscordBot.Tests.Integration;

/// <summary>
/// Base class for integration tests providing database setup and teardown
/// </summary>
public abstract class IntegrationTestBase : IDisposable
{
    protected readonly ShadowrunDbContext _context;
    protected readonly DatabaseService _databaseService;
    protected readonly IServiceScope _scope;
    protected readonly IServiceProvider _serviceProvider;
    private bool _disposed;

    protected IntegrationTestBase()
    {
        var services = new ServiceCollection();

        // Setup in-memory database
        services.AddDbContext<ShadowrunDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

        // Add logging
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();

        _context = _scope.ServiceProvider.GetRequiredService<ShadowrunDbContext>();
        
        // Create database
        _context.Database.EnsureCreated();
        
        // Create database service with test context
        var config = new BotConfig
        {
            Database = new DatabaseConfig
            {
                ConnectionString = "DataSource=:memory:",
                EnableSensitiveDataLogging = true
            }
        };
        
        var logger = _scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DatabaseService>>();
        _databaseService = new DatabaseService(config, logger);
    }

    /// <summary>
    /// Reset the database to a clean state
    /// </summary>
    protected async Task ResetDatabaseAsync()
    {
        await _context.Database.EnsureDeletedAsync().ConfigureAwait(false);
        await _context.Database.EnsureCreatedAsync().ConfigureAwait(false);
        await SeedTestDataAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Seed test data - override in derived classes
    /// </summary>
    protected virtual Task SeedTestDataAsync()
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Database.EnsureDeleted();
                _context.Dispose();
                _scope.Dispose();
            }
            _disposed = true;
        }
    }
}
