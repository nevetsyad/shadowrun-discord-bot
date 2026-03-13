using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ShadowrunDiscordBot.Models;
using ShadowrunDiscordBot.Services;
using Xunit;

namespace ShadowrunDiscordBot.Tests.Services;

/// <summary>
/// Unit tests for CombatService - tests basic state management
/// </summary>
public class CombatServiceTests
{
    private readonly Mock<DatabaseService> _databaseMock;
    private readonly Mock<ILogger<CombatService>> _loggerMock;
    private readonly DiceService _diceService;
    private readonly CombatService _combatService;

    public CombatServiceTests()
    {
        _databaseMock = new Mock<DatabaseService>();
        _loggerMock = new Mock<ILogger<CombatService>>();
        var diceLoggerMock = new Mock<ILogger<DiceService>>();
        _diceService = new DiceService(diceLoggerMock.Object);
        _combatService = new CombatService(_databaseMock.Object, _diceService, _loggerMock.Object);
    }

    #region StartCombatAsync Tests

    [Fact]
    public async Task StartCombatAsync_WithNoExistingSession_CreatesNewSession()
    {
        // Arrange
        const ulong guildId = 123456;
        const ulong channelId = 789012;
        CombatSession? capturedSession = null;

        _databaseMock
            .Setup(x => x.GetActiveCombatSessionAsync(channelId))
            .ReturnsAsync((CombatSession?)null);

        _databaseMock
            .Setup(x => x.AddCombatSessionAsync(It.IsAny<CombatSession>()))
            .Callback<CombatSession>(s => capturedSession = s)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _combatService.StartCombatAsync(guildId, channelId);

        // Assert
        result.Should().NotBeNull();
        result.DiscordGuildId.Should().Be(guildId);
        result.DiscordChannelId.Should().Be(channelId);
        result.IsActive.Should().BeTrue();
        result.CurrentPass.Should().Be(1);
        result.CurrentTurn.Should().Be(1);
    }

    [Fact]
    public async Task StartCombatAsync_WithExistingSession_ThrowsInvalidOperationException()
    {
        // Arrange
        const ulong guildId = 123456;
        const ulong channelId = 789012;

        _databaseMock
            .Setup(x => x.GetActiveCombatSessionAsync(channelId))
            .ReturnsAsync(new CombatSession { Id = 1, IsActive = true });

        // Act
        var act = async () => await _combatService.StartCombatAsync(guildId, channelId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already in progress*");
    }

    #endregion

    #region EndCombatAsync Tests

    [Fact]
    public async Task EndCombatAsync_WithActiveSession_EndsSession()
    {
        // Arrange
        const ulong channelId = 789012;
        var session = new CombatSession
        {
            Id = 1,
            DiscordChannelId = channelId,
            IsActive = true,
            StartedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        _databaseMock
            .Setup(x => x.GetActiveCombatSessionAsync(channelId))
            .ReturnsAsync(session);

        _databaseMock
            .Setup(x => x.UpdateCombatSessionAsync(It.IsAny<CombatSession>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _combatService.EndCombatAsync(channelId);

        // Assert
        result.IsActive.Should().BeFalse();
        result.EndedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task EndCombatAsync_WithNoActiveSession_ThrowsInvalidOperationException()
    {
        // Arrange
        const ulong channelId = 789012;

        _databaseMock
            .Setup(x => x.GetActiveCombatSessionAsync(channelId))
            .ReturnsAsync((CombatSession?)null);

        // Act
        var act = async () => await _combatService.EndCombatAsync(channelId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No active combat session*");
    }

    #endregion

    #region NextTurnAsync Tests - Null Reference Fixes (CRIT-001, CRIT-002)

    [Fact]
    public async Task NextTurnAsync_WithNoParticipants_ThrowsInvalidOperationException()
    {
        // Arrange
        const ulong channelId = 789012;
        var session = new CombatSession
        {
            Id = 1,
            DiscordChannelId = channelId,
            IsActive = true,
            Participants = new List<CombatParticipant>() // Empty list
        };

        _databaseMock
            .Setup(x => x.GetActiveCombatSessionAsync(channelId))
            .ReturnsAsync(session);

        // Act
        var act = async () => await _combatService.NextTurnAsync(channelId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No combatants*");
    }

    [Fact]
    public async Task NextTurnAsync_WithNullParticipants_ThrowsInvalidOperationException()
    {
        // Arrange
        const ulong channelId = 789012;
        var session = new CombatSession
        {
            Id = 1,
            DiscordChannelId = channelId,
            IsActive = true,
            Participants = null! // Null - tests CRIT-001 fix
        };

        _databaseMock
            .Setup(x => x.GetActiveCombatSessionAsync(channelId))
            .ReturnsAsync(session);

        // Act
        var act = async () => await _combatService.NextTurnAsync(channelId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No combatants*");
    }

    [Fact]
    public async Task NextTurnAsync_WithNoActiveSession_ThrowsInvalidOperationException()
    {
        // Arrange
        const ulong channelId = 789012;

        _databaseMock
            .Setup(x => x.GetActiveCombatSessionAsync(channelId))
            .ReturnsAsync((CombatSession?)null);

        // Act
        var act = async () => await _combatService.NextTurnAsync(channelId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No active combat session*");
    }

    #endregion

    #region RemoveCombatantAsync Tests - Null Reference Fixes (CRIT-001)

    [Fact]
    public async Task RemoveCombatantAsync_WithNullParticipants_ThrowsInvalidOperationException()
    {
        // Arrange
        const ulong channelId = 789012;
        var session = new CombatSession
        {
            Id = 1,
            DiscordChannelId = channelId,
            IsActive = true,
            Participants = null! // Null - tests CRIT-001 fix
        };

        _databaseMock
            .Setup(x => x.GetActiveCombatSessionAsync(channelId))
            .ReturnsAsync(session);

        // Act
        var act = async () => await _combatService.RemoveCombatantAsync(channelId, "TestNPC");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task RemoveCombatantAsync_WithNonExistentCombatant_ThrowsInvalidOperationException()
    {
        // Arrange
        const ulong channelId = 789012;
        var session = new CombatSession
        {
            Id = 1,
            DiscordChannelId = channelId,
            IsActive = true,
            Participants = new List<CombatParticipant>
            {
                new() { Id = 1, Name = "ExistingNPC" }
            }
        };

        _databaseMock
            .Setup(x => x.GetActiveCombatSessionAsync(channelId))
            .ReturnsAsync(session);

        // Act
        var act = async () => await _combatService.RemoveCombatantAsync(channelId, "NonExistentNPC");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    #endregion

    #region ExecuteAttackAsync Tests - Null Reference Fixes (CRIT-001)

    [Fact]
    public async Task ExecuteAttackAsync_WithNullParticipants_ThrowsForAttacker()
    {
        // Arrange
        const ulong channelId = 789012;
        var session = new CombatSession
        {
            Id = 1,
            DiscordChannelId = channelId,
            IsActive = true,
            Participants = null! // Null - tests CRIT-001 fix
        };

        _databaseMock
            .Setup(x => x.GetActiveCombatSessionAsync(channelId))
            .ReturnsAsync(session);

        // Act
        var act = async () => await _combatService.ExecuteAttackAsync(
            channelId, "Attacker", "Target", 10);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Attacker*not found*");
    }

    [Fact]
    public async Task ExecuteAttackAsync_WithNonExistentTarget_ThrowsInvalidOperationException()
    {
        // Arrange
        const ulong channelId = 789012;
        var session = new CombatSession
        {
            Id = 1,
            DiscordChannelId = channelId,
            IsActive = true,
            Participants = new List<CombatParticipant>
            {
                new() { Id = 1, Name = "Attacker" }
            }
        };

        _databaseMock
            .Setup(x => x.GetActiveCombatSessionAsync(channelId))
            .ReturnsAsync(session);

        // Act
        var act = async () => await _combatService.ExecuteAttackAsync(
            channelId, "Attacker", "NonExistentTarget", 10);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Target*not found*");
    }

    #endregion

    #region API Method Tests

    [Fact]
    public async Task GetActiveCombatAsync_WithNoActiveCombat_ReturnsNull()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.GetAnyActiveCombatSessionAsync())
            .ReturnsAsync((CombatSession?)null);

        // Act
        var result = await _combatService.GetActiveCombatAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllCombatSessionsAsync_ReturnsSessionSummaries()
    {
        // Arrange
        var sessions = new List<CombatSession>
        {
            new() { Id = 1, IsActive = true, Participants = new List<CombatParticipant>() },
            new() { Id = 2, IsActive = false, Participants = new List<CombatParticipant>() }
        };

        _databaseMock
            .Setup(x => x.GetRecentCombatSessionsAsync(10))
            .ReturnsAsync(sessions);

        // Act
        var result = await _combatService.GetAllCombatSessionsAsync(10);

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion
}
