using ShadowrunDiscordBot.Models;
using Xunit;

namespace ShadowrunDiscordBot.Tests.Integration.Services;

/// <summary>
/// Integration tests for CombatService
/// </summary>
public class CombatServiceIntegrationTests : IntegrationTestBase
{
    private readonly CombatService _combatService;
    private const ulong TestChannelId = 123456789;
    private const ulong TestGuildId = 987654321;

    public CombatServiceIntegrationTests()
    {
        var logger = _serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CombatService>>();
        var diceLogger = _serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DiceService>>();
        var diceService = new DiceService(diceLogger);
        _combatService = new CombatService(_databaseService, diceService, logger);
    }

    protected override async Task SeedTestDataAsync()
    {
        // Create test characters
        var character = new ShadowrunCharacter
        {
            DiscordUserId = 111111111,
            Name = "Test Runner",
            Metatype = "Human",
            Archetype = "Street Samurai",
            Body = 5,
            Quickness = 6,
            Strength = 5,
            Charisma = 3,
            Intelligence = 4,
            Willpower = 4
        };

        await _databaseService.CreateCharacterAsync(character);
    }

    [Fact]
    public async Task CreateCombatSession_ValidChannel_ReturnsSession()
    {
        // Act
        var session = await _combatService.StartCombatAsync(TestChannelId, TestGuildId);

        // Assert
        Assert.NotNull(session);
        Assert.Equal(TestChannelId, session.DiscordChannelId);
        Assert.Equal(TestGuildId, session.DiscordGuildId);
        Assert.True(session.IsActive);
    }

    [Fact]
    public async Task CreateCombatSession_DuplicateChannel_ReturnsNull()
    {
        // Arrange
        await _combatService.StartCombatAsync(TestChannelId, TestGuildId);

        // Act - Try to create another session in same channel
        var secondSession = await _combatService.StartCombatAsync(TestChannelId, TestGuildId);

        // Assert
        Assert.Null(secondSession);
    }

    [Fact]
    public async Task AddParticipant_ValidSession_AddsParticipant()
    {
        // Arrange
        var session = await _combatService.StartCombatAsync(TestChannelId, TestGuildId);
        var character = await _databaseService.GetCharacterByNameAsync(111111111, "Test Runner");

        // Act
        var participant = await _combatService.AddParticipantAsync(
            session!.Id,
            character!.Id,
            "Test Runner",
            initiative: 15);

        // Assert
        Assert.NotNull(participant);
        Assert.Equal("Test Runner", participant.Name);
        Assert.Equal(15, participant.Initiative);
    }

    [Fact]
    public async Task GetActiveSession_WithActiveSession_ReturnsSession()
    {
        // Arrange
        await _combatService.StartCombatAsync(TestChannelId, TestGuildId);

        // Act
        var session = await _combatService.GetActiveSessionAsync(TestChannelId);

        // Assert
        Assert.NotNull(session);
        Assert.True(session.IsActive);
    }

    [Fact]
    public async Task GetActiveSession_NoActiveSession_ReturnsNull()
    {
        // Act
        var session = await _combatService.GetActiveSessionAsync(999999999);

        // Assert
        Assert.Null(session);
    }

    [Fact]
    public async Task EndCombat_ActiveSession_EndsSuccessfully()
    {
        // Arrange
        var session = await _combatService.StartCombatAsync(TestChannelId, TestGuildId);

        // Act
        await _combatService.EndCombatAsync(session!.Id);
        var endedSession = await _combatService.GetSessionAsync(session.Id);

        // Assert
        Assert.NotNull(endedSession);
        Assert.False(endedSession.IsActive);
        Assert.NotNull(endedSession.EndedAt);
    }

    [Fact]
    public async Task NextTurn_MultipleParticipants_AdvancesCorrectly()
    {
        // Arrange
        var session = await _combatService.StartCombatAsync(TestChannelId, TestGuildId);
        
        await _combatService.AddParticipantAsync(session!.Id, null, "NPC 1", initiative: 20);
        await _combatService.AddParticipantAsync(session!.Id, null, "NPC 2", initiative: 15);
        await _combatService.AddParticipantAsync(session!.Id, null, "NPC 3", initiative: 10);

        // Act
        var nextParticipant = await _combatService.NextTurnAsync(session.Id);

        // Assert
        Assert.NotNull(nextParticipant);
        // After sorting, NPC 1 should go first (highest initiative)
        Assert.Equal("NPC 1", nextParticipant.Name);
    }
}
