namespace ShadowrunDiscordBot.Tests.Load;

using System.Diagnostics;

/// <summary>
/// Main load tester class
/// </summary>
public class LoadTester
{
    private readonly LoadTestScenarios _scenarios;
    
    public LoadTester(LoadTestConfig? config = null)
    {
        _scenarios = new LoadTestScenarios(config);
    }
    
    /// <summary>
    /// Run a comprehensive load test
    /// </summary>
    public async Task RunComprehensiveTest()
    {
        Console.WriteLine("=== Shadowrun Discord Bot Load Test ===");
        Console.WriteLine($"Started at: {DateTime.UtcNow:O}");
        Console.WriteLine();
        
        var totalStopwatch = Stopwatch.StartNew();
        
        // Run all scenarios
        var results = await _scenarios.RunAllScenarios();
        
        totalStopwatch.Stop();
        
        // Display results
        Console.WriteLine();
        Console.WriteLine("=== Load Test Results ===");
        Console.WriteLine();
        
        foreach (var result in results)
        {
            Console.WriteLine(result);
            Console.WriteLine();
        }
        
        // Summary
        var totalOperations = results.Sum(r => r.TotalOperations);
        var totalSuccesses = results.Sum(r => r.SuccessCount);
        var totalFailures = results.Sum(r => r.FailureCount);
        
        Console.WriteLine("=== Summary ===");
        Console.WriteLine($"Total Test Duration: {totalStopwatch.Elapsed.TotalSeconds:F2}s");
        Console.WriteLine($"Total Operations: {totalOperations}");
        Console.WriteLine($"Total Successes: {totalSuccesses} ({totalSuccesses * 100.0 / totalOperations:F1}%)");
        Console.WriteLine($"Total Failures: {totalFailures}");
        Console.WriteLine($"Overall Ops/sec: {totalOperations / totalStopwatch.Elapsed.TotalSeconds:F2}");
        Console.WriteLine();
        
        // Performance recommendations
        Console.WriteLine("=== Performance Recommendations ===");
        
        foreach (var result in results)
        {
            if (result.FailureCount > 0)
            {
                Console.WriteLine($"⚠️ {result.Scenario}: {result.FailureCount} failures detected");
            }
            
            if (result.OperationsPerSecond < 100)
            {
                Console.WriteLine($"⚠️ {result.Scenario}: Low throughput ({result.OperationsPerSecond:F2} ops/sec)");
            }
            else if (result.OperationsPerSecond > 1000)
            {
                Console.WriteLine($"✅ {result.Scenario}: Excellent throughput ({result.OperationsPerSecond:F2} ops/sec)");
            }
        }
        
        Console.WriteLine();
        Console.WriteLine($"Completed at: {DateTime.UtcNow:O}");
    }
    
    /// <summary>
    /// Run a specific scenario with custom parameters
    /// </summary>
    public async Task RunScenario(string scenarioName, int parameter)
    {
        Console.WriteLine($"Running scenario: {scenarioName} with parameter: {parameter}");
        
        LoadTestResult result = scenarioName.ToLower() switch
        {
            "character_creation" => await _scenarios.TestConcurrentCharacterCreation(parameter),
            "combat" => await _scenarios.TestLargeCombatSession(parameter),
            "dice_rolls" => await _scenarios.TestFrequentDiceRolls(parameter),
            "database" => await _scenarios.TestConcurrentDatabaseQueries(parameter),
            _ => throw new ArgumentException($"Unknown scenario: {scenarioName}")
        };
        
        Console.WriteLine(result);
    }
}
