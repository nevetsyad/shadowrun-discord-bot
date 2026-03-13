namespace ShadowrunDiscordBot.Tests.Load;

using System.Diagnostics;

/// <summary>
/// Load test scenarios for the Shadowrun Discord Bot
/// </summary>
public class LoadTestScenarios
{
    private readonly LoadTestConfig _config;
    
    public LoadTestScenarios(LoadTestConfig? config = null)
    {
        _config = config ?? new LoadTestConfig();
    }
    
    /// <summary>
    /// Scenario 1: Multiple users creating characters simultaneously
    /// </summary>
    public async Task<LoadTestResult> TestConcurrentCharacterCreation(int userCount)
    {
        Console.WriteLine($"[Scenario 1] Testing concurrent character creation with {userCount} users");
        
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task<bool>>();
        var successCount = 0;
        var failureCount = 0;
        
        // Simulate concurrent character creation
        for (int i = 0; i < userCount; i++)
        {
            var userId = (ulong)i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    // Simulate character creation
                    await Task.Delay(Random.Shared.Next(10, 100)); // Simulate processing time
                    
                    // In a real test, this would call the character creation service
                    // For now, we'll just simulate it
                    Interlocked.Increment(ref successCount);
                    return true;
                }
                catch
                {
                    Interlocked.Increment(ref failureCount);
                    return false;
                }
            }));
        }
        
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        return new LoadTestResult
        {
            Scenario = "Concurrent Character Creation",
            TotalOperations = userCount,
            SuccessCount = successCount,
            FailureCount = failureCount,
            Duration = stopwatch.Elapsed,
            OperationsPerSecond = userCount / stopwatch.Elapsed.TotalSeconds
        };
    }
    
    /// <summary>
    /// Scenario 2: Large combat session with many participants
    /// </summary>
    public async Task<LoadTestResult> TestLargeCombatSession(int participantCount)
    {
        Console.WriteLine($"[Scenario 2] Testing large combat session with {participantCount} participants");
        
        var stopwatch = Stopwatch.StartNew();
        var operations = 0;
        var failures = 0;
        
        // Simulate combat initialization
        for (int round = 0; round < 3; round++)
        {
            // Simulate initiative rolls for all participants
            for (int i = 0; i < participantCount; i++)
            {
                try
                {
                    // Simulate dice roll
                    await Task.Delay(Random.Shared.Next(1, 10));
                    operations++;
                }
                catch
                {
                    failures++;
                }
            }
            
            // Simulate multiple combat passes
            for (int pass = 0; pass < 4; pass++)
            {
                for (int i = 0; i < participantCount; i++)
                {
                    try
                    {
                        // Simulate combat action
                        await Task.Delay(Random.Shared.Next(1, 10));
                        operations++;
                    }
                    catch
                    {
                        failures++;
                    }
                }
            }
        }
        
        stopwatch.Stop();
        
        return new LoadTestResult
        {
            Scenario = "Large Combat Session",
            TotalOperations = operations,
            SuccessCount = operations - failures,
            FailureCount = failures,
            Duration = stopwatch.Elapsed,
            OperationsPerSecond = operations / stopwatch.Elapsed.TotalSeconds
        };
    }
    
    /// <summary>
    /// Scenario 3: Frequent dice rolls under load
    /// </summary>
    public async Task<LoadTestResult> TestFrequentDiceRolls(int rollCount)
    {
        Console.WriteLine($"[Scenario 3] Testing {rollCount} frequent dice rolls");
        
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task<bool>>();
        var successCount = 0;
        var failureCount = 0;
        
        for (int i = 0; i < rollCount; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    // Simulate dice roll
                    await Task.Delay(Random.Shared.Next(1, 5));
                    Interlocked.Increment(ref successCount);
                    return true;
                }
                catch
                {
                    Interlocked.Increment(ref failureCount);
                    return false;
                }
            }));
        }
        
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        return new LoadTestResult
        {
            Scenario = "Frequent Dice Rolls",
            TotalOperations = rollCount,
            SuccessCount = successCount,
            FailureCount = failureCount,
            Duration = stopwatch.Elapsed,
            OperationsPerSecond = rollCount / stopwatch.Elapsed.TotalSeconds
        };
    }
    
    /// <summary>
    /// Scenario 4: Concurrent database queries
    /// </summary>
    public async Task<LoadTestResult> TestConcurrentDatabaseQueries(int queryCount)
    {
        Console.WriteLine($"[Scenario 4] Testing {queryCount} concurrent database queries");
        
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task<bool>>();
        var successCount = 0;
        var failureCount = 0;
        
        for (int i = 0; i < queryCount; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    // Simulate database query
                    await Task.Delay(Random.Shared.Next(5, 50));
                    Interlocked.Increment(ref successCount);
                    return true;
                }
                catch
                {
                    Interlocked.Increment(ref failureCount);
                    return false;
                }
            }));
        }
        
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        return new LoadTestResult
        {
            Scenario = "Concurrent Database Queries",
            TotalOperations = queryCount,
            SuccessCount = successCount,
            FailureCount = failureCount,
            Duration = stopwatch.Elapsed,
            OperationsPerSecond = queryCount / stopwatch.Elapsed.TotalSeconds
        };
    }
    
    /// <summary>
    /// Run all load test scenarios
    /// </summary>
    public async Task<List<LoadTestResult>> RunAllScenarios()
    {
        var results = new List<LoadTestResult>
        {
            await TestConcurrentCharacterCreation(_config.ConcurrentUsers),
            await TestLargeCombatSession(_config.CombatParticipants),
            await TestFrequentDiceRolls(_config.DiceRollCount),
            await TestConcurrentDatabaseQueries(_config.DatabaseQueryCount)
        };
        
        return results;
    }
}

/// <summary>
/// Configuration for load tests
/// </summary>
public class LoadTestConfig
{
    public int ConcurrentUsers { get; set; } = 100;
    public int CombatParticipants { get; set; } = 50;
    public int DiceRollCount { get; set; } = 1000;
    public int DatabaseQueryCount { get; set; } = 500;
    public int Duration { get; set; } = 60; // seconds
}

/// <summary>
/// Result of a load test scenario
/// </summary>
public class LoadTestResult
{
    public string Scenario { get; set; } = string.Empty;
    public int TotalOperations { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public TimeSpan Duration { get; set; }
    public double OperationsPerSecond { get; set; }
    
    public override string ToString()
    {
        return $"[{Scenario}]\n" +
               $"  Total Operations: {TotalOperations}\n" +
               $"  Success: {SuccessCount} ({SuccessCount * 100.0 / TotalOperations:F1}%)\n" +
               $"  Failures: {FailureCount}\n" +
               $"  Duration: {Duration.TotalSeconds:F2}s\n" +
               $"  Ops/sec: {OperationsPerSecond:F2}";
    }
}
