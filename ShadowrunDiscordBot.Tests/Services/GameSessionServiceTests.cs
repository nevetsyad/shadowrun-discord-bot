using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ShadowrunDiscordBot.Models;
using ShadowrunDiscordBot.Services;
using Xunit;

namespace ShadowrunDiscordBot.Tests.Services;

/// <summary>
/// Unit tests for GameSessionService - tests session lifecycle
/// </summary>
public class GameSessionServiceTests
{
    private readonly Mock<DatabaseService> _databaseMock;
    private readonly Mock<ILogger<GameSessionService>> _loggerMock;
    private readonly GameSessionService _sessionService;

    public GameSessionServiceTests()
    {
        _databaseMock = new Mock<DatabaseService>();
        _loggerMock = new Mock<ILogger<GameSessionService>>();
        _sessionService = new GameSessionService(_databaseMock.Object, _loggerMock.Object);
    }

    #region StartSessionAsync Tests

    [Fact]
    public async Task StartSessionAsync_WithNoExistingSession_CreatesNewSession()
    {
        // Arrange
        const ulong guildId = 123456;
        const ulong channelId = 789012;
        const ulong gmUserId = 111222;
        GameSession? capturedSession = null;

        _databaseMock
            .Setup(x => x.GetActiveGameSessionAsync(channelId))
            .ReturnsAsync((GameSession?)null);

        _databaseMock
            .Setup(x => x.AddGameSessionAsync(It.IsAny<GameSession>()))
            .Callback<GameSession>(s => capturedSession = s)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sessionService.StartSessionAsync(guildId, channelId, gmUserId, "Test Session");

        // Assert
        result.Should().NotBeNull();
        result.DiscordGuildId.Should().Be(guildId);
        result.DiscordChannelId.Should().Be(channelId);
        result.GameMasterUserId.Should().Be(gmUserId);
        result.SessionName.Should().Be("Test Session");
        result.Status.Should().Be(SessionStatus.Active);
    }

    [Fact]
    public async Task StartSessionAsync_WithExistingSession_ThrowsInvalidOperationException()
    {
        // Arrange
        const ulong guildId = 123456;
        const ulong channelId = 789012;
        const ulong gmUserId = 111222;

        _databaseMock
            .Setup(x => x.GetActiveGameSessionAsync(channelId))
            .ReturnsAsync(new GameSession { Id = 1, Status = SessionStatus.Active });

        // Act
        var act = async () => await _sessionService.StartSessionAsync(guildId, channelId, gmUserId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    #endregion

    #region EndSessionAsync Tests

    [Fact]
    public async Task EndSessionAsync_WithActiveSession_EndsSession()
    {
        // Arrange
        const ulong channelId = 789012;
        var session = new GameSession
        {
            Id = 1,
            DiscordChannelId = channelId,
            Status = SessionStatus.Active,
            StartedAt = DateTime.UtcNow.AddHours(-2)
        };

        _databaseMock
            .Setup(x => x.GetActiveGameSessionAsync(channelId))
            .ReturnsAsync(session);

        _databaseMock
            .Setup(x => x.UpdateGameSessionAsync(It.IsAny<GameSession>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sessionService.EndSessionAsync(channelId);

        // Assert
        result.Status.Should().Be(SessionStatus.Ended);
        result.EndedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task EndSessionAsync_WithNoActiveSession_ThrowsInvalidOperationException()
    {
        // Arrange
        const ulong channelId = 789012;

        _databaseMock
            .Setup(x => x.GetActiveGameSessionAsync(channelId))
            .ReturnsAsync((GameSession?)null);

        // Act
        var act = async () => await _sessionService.EndSessionAsync(channelId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No active session*");
    }

    #endregion

    #region PauseSessionAsync Tests

    [Fact]
    public async Task PauseSessionAsync_WithActiveSession_PausesSession()
    {
        // Arrange
        const ulong channelId = 789012;
        var session = new GameSession
        {
            Id = 1,
            DiscordChannelId = channelId,
            Status = SessionStatus.Active
        };

        _databaseMock
            .Setup(x => x.GetActiveGameSessionAsync(channelId))
            .ReturnsAsync(session);

        _databaseMock
            .Setup(x => x.UpdateGameSessionAsync(It.IsAny<GameSession>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sessionService.PauseSessionAsync(channelId);

        // Assert
        result.Status.Should().Be(SessionStatus.Paused);
    }

    #endregion

    #region ResumeSessionAsync Tests

    [Fact]
    public async Task ResumeSessionAsync_WithPausedSession_ResumesSession()
    {
        // Arrange
        const ulong channelId = 789012;
        var session = new GameSession
        {
            Id = 1,
            DiscordChannelId = channelId,
            Status = SessionStatus.Paused
        };

        _databaseMock
            .Setup(x => x.GetPausedGameSessionAsync(channelId))
            .ReturnsAsync(session);

        _databaseMock
            .Setup(x => x.UpdateGameSessionAsync(It.IsAny<GameSession>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sessionService.ResumeSessionAsync(channelId);

        // Assert
        result.Status.Should().Be(SessionStatus.Active);
        result.LastActivityAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ResumeSessionAsync_WithNoPausedSession_ThrowsInvalidOperationException()
    {
        // Arrange
        const ulong channelId = 789012;

        _databaseMock
            .Setup(x => x.GetPausedGameSessionAsync(channelId))
            .ReturnsAsync((GameSession?)null);

        // Act
        var act = async () => await _sessionService.ResumeSessionAsync(channelId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No paused session*");
    }

    #endregion

    #region Participant Management Tests

    [Fact]
    public async Task AddParticipantAsync_WithNoActiveSession_ThrowsInvalidOperationException()
    {
        // Arrange
        const ulong channelId = 789012;
        const ulong userId = 111222;

        _databaseMock
            .Setup(x => x.GetActiveGameSessionAsync(channelId))
            .ReturnsAsync((GameSession?)null);

        // Act
        var act = async () => await _sessionService.AddParticipantAsync(channelId, userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No active session*");
    }

    [Fact]
    public async Task AddParticipantAsync_WithExistingParticipant_ReturnsExisting()
    {
        // Arrange
        const ulong channelId = 789012;
        const ulong userId = 111222;
        var existingParticipant = new SessionParticipant
        {
            Id = 1,
            DiscordUserId = userId,
            IsActive = true
        };

        var session = new GameSession
        {
            Id = 1,
            DiscordChannelId = channelId,
            Status = SessionStatus.Active,
            Participants = new List<SessionParticipant> { existingParticipant }
        };

        _databaseMock
            .Setup(x => x.GetActiveGameSessionAsync(channelId))
            .ReturnsAsync(session);

        // Act
        var result = await _sessionService.AddParticipantAsync(channelId, userId);

        // Assert
        result.Should().Be(existingParticipant);
    }

    [Fact]
    public async Task GetActiveParticipantsAsync_WithNullParticipants_ReturnsEmptyList()
    {
        // Arrange - tests CRIT-001 fix for null Participants
        const ulong channelId = 789012;
        var session = new GameSession
        {
            Id = 1,
            DiscordChannelId = channelId,
            Status = SessionStatus.Active,
            Participants = null! // Null - tests CRIT-001 fix
        };

        _databaseMock
            .Setup(x => x.GetActiveGameSessionAsync(channelId))
            .ReturnsAsync(session);

        // Act
        var result = await _sessionService.GetActiveParticipantsAsync(channelId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region Break Status Tests

    [Fact]
    public async Task CheckBreakStatusAsync_WithNoActiveSession_ReturnsNoSession()
    {
        // Arrange
        const ulong channelId = 789012;

        _databaseMock
            .Setup(x => x.GetActiveGameSessionAsync(channelId))
            .ReturnsAsync((GameSession?)null);

        // Act
        var result = await _sessionService.CheckBreakStatusAsync(channelId);

        // Assert
        result.HasActiveSession.Should().BeFalse();
    }

    [Fact]
    public async Task CheckBreakStatusAsync_WithRecentActivity_ReturnsNotOnBreak()
    {
        // Arrange
        const ulong channelId = 789012;
        var session = new GameSession
        {
            Id = 1,
            DiscordChannelId = channelId,
            Status = SessionStatus.Active,
            LastActivityAt = DateTime.UtcNow.AddMinutes(-5) // Recent activity
        };

        _databaseMock
            .Setup(x => x.GetActiveGameSessionAsync(channelId))
            .ReturnsAsync(session);

        // Act
        var result = await _sessionService.CheckBreakStatusAsync(channelId);

        // Assert
        result.HasActiveSession.Should().BeTrue();
        result.IsOnBreak.Should().BeFalse();
    }

    [Fact]
    public async Task CheckBreakStatusAsync_WithOldActivity_ReturnsOnBreak()
    {
        // Arrange
        const ulong channelId = 789012;
        var session = new GameSession
        {
            Id = 1,
            DiscordChannelId = channelId,
            Status = SessionStatus.Active,
            LastActivityAt = DateTime.UtcNow.AddMinutes(-45) // Old activity
        };

        _databaseMock
            .Setup(x => x.GetActiveGameSessionAsync(channelId))
            .ReturnsAsync(session);

        // Act
        var result = await _sessionService.CheckBreakStatusAsync(channelId);

        // Assert
        result.HasActiveSession.Should().BeTrue();
        result.IsOnBreak.Should().BeTrue();
    }

    #endregion

    #region Session Progress Tests

    [Fact]
    public async Task GetSessionProgressAsync_WithNullParticipants_ReturnsZeroParticipants()
    {
        // Arrange - tests CRIT-001 fix for null collections
        const ulong channelId = 789012;
        var session = new GameSession
        {
            Id = 1,
            DiscordChannelId = channelId,
            Status = SessionStatus.Active,
            StartedAt = DateTime.UtcNow.AddHours(-1),
            Participants = null!,
            NarrativeEvents = null!,
            PlayerChoices = null!,
            ActiveMissions = null!
        };

        _databaseMock
            .Setup(x => x.GetActiveGameSessionAsync(channelId))
            .ReturnsAsync(session);

        // Act
        var result = await _sessionService.GetSessionProgressAsync(channelId);

        // Assert
        result.ActiveParticipants.Should().Be(0);
        result.NarrativeEvents.Should().Be(0);
        result.PlayerChoices.Should().Be(0);
    }

    #endregion
}
