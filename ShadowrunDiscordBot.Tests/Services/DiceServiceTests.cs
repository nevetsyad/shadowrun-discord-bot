using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ShadowrunDiscordBot.Services;
using Xunit;

namespace ShadowrunDiscordBot.Tests.Services;

/// <summary>
/// Unit tests for DiceService - tests pure logic with no external dependencies
/// </summary>
public class DiceServiceTests : IDisposable
{
    private readonly DiceService _diceService;
    private readonly Mock<ILogger<DiceService>> _loggerMock;

    public DiceServiceTests()
    {
        _loggerMock = new Mock<ILogger<DiceService>>();
        _diceService = new DiceService(_loggerMock.Object);
    }

    public void Dispose()
    {
        _diceService.Dispose();
    }

    #region Basic Dice Rolling Tests

    [Fact]
    public void Roll_WithValidParameters_ReturnsResultInRange()
    {
        // Arrange
        const int numDice = 2;
        const int sides = 6;

        // Act
        var result = _diceService.Roll(numDice, sides);

        // Assert
        result.Total.Should().BeInRange(numDice, numDice * sides);
        result.Rolls.Should().HaveCount(numDice);
    }

    [Fact]
    public void Roll_WithSingleDie_ReturnsCorrectRange()
    {
        // Act
        var result = _diceService.Roll(1, 20);

        // Assert
        result.Total.Should().BeInRange(1, 20);
        result.Rolls.Should().ContainSingle();
    }

    #endregion

    #region Shadowrun Dice Tests

    [Fact]
    public void RollShadowrun_WithValidPool_ReturnsCorrectSuccessCount()
    {
        // Arrange
        const int poolSize = 10;

        // Act
        var result = _diceService.RollShadowrun(poolSize, 5);

        // Assert
        result.PoolSize.Should().Be(poolSize);
        result.Rolls.Should().HaveCount(poolSize);
        result.Successes.Should().BeInRange(0, poolSize);
    }

    [Fact]
    public void RollShadowrun_WithZeroPool_ReturnsEmptyResult()
    {
        // Act
        var result = _diceService.RollShadowrun(0);

        // Assert
        result.PoolSize.Should().Be(0);
        result.Rolls.Should().BeEmpty();
        result.Successes.Should().Be(0);
    }

    [Fact]
    public void RollShadowrun_WithNegativePool_ThrowsArgumentException()
    {
        // Act
        var act = () => _diceService.RollShadowrun(-1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Pool size must be non-negative*");
    }

    [Fact]
    public void RollShadowrun_WithPoolExceedingMax_ThrowsArgumentException()
    {
        // Act
        var act = () => _diceService.RollShadowrun(101);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot exceed*");
    }

    [Fact]
    public void RollShadowrun_WithPoolAtMax_DoesNotThrow()
    {
        // Act
        var act = () => _diceService.RollShadowrun(100);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Edge Dice Tests (Exploding)

    [Fact]
    public void RollEdge_WithValidPool_ReturnsResult()
    {
        // Arrange
        const int poolSize = 5;

        // Act
        var result = _diceService.RollEdge(poolSize);

        // Assert
        result.PoolSize.Should().Be(poolSize);
        result.Rolls.Should().HaveCountGreaterOrEqualTo(poolSize);
    }

    [Fact]
    public void RollEdge_WithZeroPool_ReturnsEmptyResult()
    {
        // Act
        var result = _diceService.RollEdge(0);

        // Assert
        result.PoolSize.Should().Be(0);
        result.Rolls.Should().BeEmpty();
    }

    [Fact]
    public void RollEdge_WithNegativePool_ThrowsArgumentException()
    {
        // Act
        var act = () => _diceService.RollEdge(-1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RollEdge_WithPoolExceedingMax_ThrowsArgumentException()
    {
        // Act
        var act = () => _diceService.RollEdge(101);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Friedman Dice Tests (Exploding)

    [Fact]
    public void RollFriedmanDice_WithValidPool_ReturnsResult()
    {
        // Arrange
        const int poolSize = 6;

        // Act
        var result = _diceService.RollFriedmanDice(poolSize);

        // Assert
        result.PoolSize.Should().Be(poolSize);
        result.Rolls.Should().HaveCountGreaterOrEqualTo(poolSize);
    }

    [Fact]
    public void RollFriedmanDice_WithZeroPool_ReturnsEmptyResult()
    {
        // Act
        var result = _diceService.RollFriedmanDice(0);

        // Assert
        result.PoolSize.Should().Be(0);
        result.Rolls.Should().BeEmpty();
    }

    [Fact]
    public void RollFriedmanDice_WithNegativePool_ThrowsArgumentException()
    {
        // Act
        var act = () => _diceService.RollFriedmanDice(-1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RollFriedmanDice_WithPoolExceedingMax_ThrowsArgumentException()
    {
        // Act
        var act = () => _diceService.RollFriedmanDice(101);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Initiative Tests

    [Fact]
    public void RollInitiative_WithValidParameters_ReturnsCorrectResult()
    {
        // Arrange
        const int baseInit = 8;
        const int diceCount = 2;

        // Act
        var result = _diceService.RollInitiative(baseInit, diceCount);

        // Assert
        result.Total.Should().BeGreaterOrEqualTo(baseInit + diceCount);
        result.Details.Should().Contain(baseInit.ToString());
    }

    [Fact]
    public void RollInitiative_CalculatesPassesCorrectly()
    {
        // Act
        var result = _diceService.RollInitiative(10, 2);

        // Assert
        result.Passes.Should().BeGreaterOrEqualTo(1);
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public void GetStatistics_AfterRolls_ReturnsValidStatistics()
    {
        // Arrange
        for (int i = 0; i < 100; i++)
        {
            _diceService.Roll(2, 6);
        }

        // Act
        var stats = _diceService.GetStatistics();

        // Assert
        stats.TotalRolls.Should().BeGreaterOrEqualTo(100);
        stats.AverageRoll.Should().BeInRange(2m, 12m);
    }

    [Fact]
    public void ResetStatistics_ClearsAllStats()
    {
        // Arrange
        for (int i = 0; i < 50; i++)
        {
            _diceService.Roll(1, 6);
        }

        // Act
        _diceService.ResetStatistics();
        var stats = _diceService.GetStatistics();

        // Assert
        stats.TotalRolls.Should().Be(0);
    }

    #endregion

    #region Pool Size Limit Tests (HIGH-003)

    [Theory]
    [InlineData(100)]  // Max allowed
    [InlineData(50)]
    [InlineData(10)]
    [InlineData(1)]
    public void RollShadowrun_WithValidPoolSizes_DoesNotThrow(int poolSize)
    {
        // Act
        var act = () => _diceService.RollShadowrun(poolSize);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(101)]  // Over max
    [InlineData(150)]
    [InlineData(1000)]
    public void RollShadowrun_WithExcessivePoolSizes_ThrowsArgumentException(int poolSize)
    {
        // Act
        var act = () => _diceService.RollShadowrun(poolSize);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot exceed*");
    }

    #endregion
}
