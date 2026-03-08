# Enabling Automatic Break Detection

The SessionIdleDetectionService is an optional background service that automatically pauses sessions after 30 minutes of inactivity.

## Quick Setup

### Option 1: Enable in Program.cs

Add this line to your `Program.cs` in the `ConfigureServices` method:

```csharp
// In Program.cs, ConfigureServices method

// Session Management System (Phase 4)
services.AddSingleton<SessionManagementService>();

// OPTIONAL: Enable automatic break detection
services.AddSessionIdleDetection();  // <-- Add this line
```

### Option 2: Manual Registration

Or register it manually:

```csharp
services.AddHostedService<SessionIdleDetectionService>();
```

## How It Works

1. **Runs every 5 minutes**: The service checks all active sessions
2. **Detects inactivity**: Sessions with no activity for 30+ minutes are paused
3. **Creates break records**: Each auto-pause is logged with reason "Automatic break - no activity detected"
4. **Logs operations**: All auto-pauses are logged for review

## Configuration

The service uses these default settings (defined in SessionManagementService.cs):

```csharp
private static readonly TimeSpan BreakThreshold = TimeSpan.FromMinutes(30);
private static readonly TimeSpan AutoResumeTimeout = TimeSpan.FromHours(2);
```

To customize these values, you would need to:
1. Make them configurable in appsettings.json
2. Create a configuration class
3. Inject it into SessionManagementService

## Example: Custom Configuration

1. Add to `appsettings.json`:
```json
{
  "SessionManagement": {
    "BreakThresholdMinutes": 30,
    "CheckIntervalMinutes": 5,
    "AutoResumeTimeoutHours": 2
  }
}
```

2. Create configuration class:
```csharp
public class SessionManagementConfig
{
    public int BreakThresholdMinutes { get; set; } = 30;
    public int CheckIntervalMinutes { get; set; } = 5;
    public int AutoResumeTimeoutHours { get; set; } = 2;
}
```

3. Register and use:
```csharp
// In Program.cs
services.Configure<SessionManagementConfig>(
    context.Configuration.GetSection("SessionManagement"));

// In SessionManagementService constructor
public SessionManagementService(
    DatabaseService database,
    GameSessionService sessionService,
    NarrativeContextService narrativeService,
    IOptions<SessionManagementConfig> config,
    ILogger<SessionManagementService> logger)
{
    _breakThreshold = TimeSpan.FromMinutes(config.Value.BreakThresholdMinutes);
    // ...
}
```

## Discord Notifications (Optional)

To send Discord notifications when sessions are auto-paused, you would need to:

1. Inject DiscordSocketClient into SessionIdleDetectionService
2. Send a message to the session's channel

Example:
```csharp
public class SessionIdleDetectionService : BackgroundService
{
    private readonly DiscordSocketClient _discord;
    // ...

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // ...
        var autoBreaks = await sessionManagement.CheckForIdleSessionsAsync();

        foreach (var sessionBreak in autoBreaks)
        {
            var channel = _discord.GetChannel(sessionBreak.DiscordChannelId) as IMessageChannel;
            if (channel != null)
            {
                await channel.SendMessageAsync(
                    "⚠️ This session has been automatically paused due to inactivity.\n" +
                    "Use `/session resume` to continue when you're ready.");
            }
        }
    }
}
```

## Monitoring

Check logs for auto-pause activity:
```
[Information] Checking for idle sessions...
[Information] Auto-paused 2 idle sessions
[Information] Session 15 has been idle for 35 minutes, auto-pausing
```

## Testing

1. Start a session: `/session start name:"Test"`
2. Wait 30+ minutes (or temporarily lower the threshold for testing)
3. Check logs to see if auto-pause occurred
4. Resume session: `/session resume`
5. View break statistics: `/session stats`

## Disabling

To disable automatic break detection:
1. Remove the `services.AddSessionIdleDetection();` line from Program.cs
2. Restart the bot

Manual breaks (`/session break`) will still work normally.

## Performance Impact

- **Minimal**: Runs every 5 minutes, only queries active sessions
- **Efficient**: Uses indexed database queries
- **Non-blocking**: Async operations don't block the main bot

## When to Use

**Enable automatic detection when:**
- Running long campaigns where breaks are common
- Want to prevent "zombie" sessions from staying active
- Need accurate time tracking (auto-paused time doesn't count as active)

**Disable when:**
- Sessions are intentionally long with low activity
- Prefer manual break management only
- Don't want automatic interruptions

## Summary

- **Optional**: Not required for Phase 4 functionality
- **Easy to enable**: One line in Program.cs
- **Configurable**: Thresholds can be customized
- **Safe**: Manual breaks still work with or without it
