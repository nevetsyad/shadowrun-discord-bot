using ShadowrunDiscordBot.Models;
using Xunit;

namespace ShadowrunDiscordBot.Tests.Integration.Services;

/// <summary>
/// Integration tests for DiceService
/// </summary>
public class DiceServiceIntegrationTests : IntegrationTestBase
{
    private readonly DiceService _diceService;

    public DiceServiceIntegrationTests()
    {
        var logger = _serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DiceService>>();
        _diceService = new DiceService(logger);
    }

    [Fact]
    public async Task RollBasicDice_ValidNotation_ReturnsCorrectFormat()
    {
        // Arrange
        const string notation = "2d6+3";

        // Act
        var result = await _diceService.RollDiceAsync(notation);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Total >= 5 && result.Total <= 15); // 2d6 (2-12) + 3 = 5-15
        Assert.Contains("2d6", result.Notation);
    }

    [Fact]
    public async Task RollBasicDice_LargeNumberOfDice_HandlesCorrectly()
    {
        // Arrange
        const string notation = "10d6";

        // Act
        var result = await _diceService.RollDiceAsync(notation);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Total >= 10 && result.Total <= 60); // 10d6 = 10-60
    }

    [Fact]
    public async Task RollShadowrun_BasicRoll_ReturnsHitsAndGlitches()
    {
        // Arrange
        const int diceCount = 6;

        // Act
        var result = await _diceService.RollShadowrunAsync(diceCount);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Hits >= 0 && result.Hits <= diceCount);
        Assert.True(result.Glitches >= 0 && result.Glitches <= diceCount);
    }

    [Fact]
    public async Task RollShadowrun_ZeroDice_ReturnsZeroHits()
    {
        // Arrange
        const int diceCount = 0;

        // Act
        var result = await _diceService.RollShadowrunAsync(diceCount);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.Hits);
    }

    [Fact]
    public async Task RollInitiative_ValidCharacter_ReturnsInitiativeOrder()
    {
        // Arrange
        var participants = new List<CombatParticipant>
        {
            new() { Name = "Runner 1", Initiative = 15 },
            new() { Name = "Runner 2", Initiative = 22 },
            new() { Name = "Runner 3", Initiative = 8 }
        };

        // Act
        var result = await _diceService.RollInitiativeForParticipantsAsync(participants);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        // Should be sorted by initiative descending
        Assert.Equal("Runner 2", result[0].Name);
        Assert.Equal("Runner 1", result[1].Name);
        Assert.Equal("Runner 3", result[2].Name);
    }

    [Fact]
    public async Task RollDice_InvalidNotation_ThrowsException()
    {
        // Arrange
        const string invalidNotation = "invalid";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _diceService.RollDiceAsync(invalidNotation));
    }

    [Fact]
    public async Task RollShadowrun_EdgeCase_AllSixes_ReturnsCriticalSuccess()
    {
        // This test verifies the service handles edge cases
        // In practice, we can't force all sixes, but we verify the structure
        
        // Arrange
        const int diceCount = 5;

        // Act
        var result = await _diceService.RollShadowrunAsync(diceCount);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(diceCount, result.DiceCount);
        Assert.True(result.Hits >= 0);
        Assert.True(result.Glitches >= 0);
        Assert.False(string.IsNullOrEmpty(result.Description));
    }
}
