using ShadowrunDiscordBot.Models;
using Xunit;

namespace ShadowrunDiscordBot.Tests.Integration;

/// <summary>
/// Integration tests for DatabaseService
/// </summary>
public class DatabaseServiceIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task InitializeAsync_CreatesDatabase()
    {
        // Act
        await _databaseService.InitializeAsync();

        // Assert - Should not throw
        Assert.True(_context.Database.CanConnect());
    }

    [Fact]
    public async Task CreateCharacter_WithCyberware_SavesCorrectly()
    {
        // Arrange
        var character = new ShadowrunCharacter
        {
            DiscordUserId = 111111111,
            Name = "Chromed",
            Metatype = "Human"
        };

        // Act
        await _databaseService.CreateCharacterAsync(character);
        await _databaseService.AddCyberwareAsync(character.Id, new CharacterCyberware
        {
            Name = "Wired Reflexes",
            Category = "Cyberware",
            EssenceCost = 2.0m,
            Rating = 2
        });

        // Assert
        var loaded = await _databaseService.GetCharacterAsync(character.Id);
        Assert.NotNull(loaded);
        Assert.Single(loaded.Cyberware);
        Assert.Equal("Wired Reflexes", loaded.Cyberware.First().Name);
    }

    [Fact]
    public async Task CreateCombatSession_WithParticipants_PersistsCorrectly()
    {
        // Arrange
        const ulong channelId = 12345;
        const ulong guildId = 67890;

        // Act
        var session = await _databaseService.CreateCombatSessionAsync(channelId, guildId);
        
        await _databaseService.AddCombatParticipantAsync(new CombatParticipant
        {
            CombatSessionId = session.Id,
            Name = "Runner 1",
            Initiative = 15,
            PhysicalDamage = 0,
            StunDamage = 0
        });

        // Assert
        var loaded = await _databaseService.GetCombatSessionAsync(session.Id);
        Assert.NotNull(loaded);
        Assert.Single(loaded.Participants);
        Assert.Equal("Runner 1", loaded.Participants.First().Name);
    }

    [Fact]
    public async Task Transaction_RollbackOnFailure_RevertsChanges()
    {
        // Arrange
        var character = new ShadowrunCharacter
        {
            DiscordUserId = 222222222,
            Name = "Transaction Test",
            Metatype = "Human"
        };
        await _databaseService.CreateCharacterAsync(character);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () =>
        {
            await _databaseService.ExecuteInTransactionAsync(async () =>
            {
                character.Name = "Changed Name";
                await _databaseService.UpdateCharacterAsync(character);
                throw new Exception("Simulated failure");
            });
        });

        // Verify rollback
        var unchanged = await _databaseService.GetCharacterAsync(character.Id);
        Assert.NotNull(unchanged);
        Assert.Equal("Transaction Test", unchanged.Name);
    }

    [Fact]
    public async Task Transaction_CommitOnSuccess_PersistsChanges()
    {
        // Arrange
        var character = new ShadowrunCharacter
        {
            DiscordUserId = 333333333,
            Name = "Commit Test",
            Metatype = "Human"
        };
        await _databaseService.CreateCharacterAsync(character);

        // Act
        await _databaseService.ExecuteInTransactionAsync(async () =>
        {
            character.Name = "Committed Name";
            await _databaseService.UpdateCharacterAsync(character);
        });

        // Assert
        var committed = await _databaseService.GetCharacterAsync(character.Id);
        Assert.NotNull(committed);
        Assert.Equal("Committed Name", committed.Name);
    }

    [Fact]
    public async Task MatrixOperations_CreateAndRetrieve_WorksCorrectly()
    {
        // Arrange
        var character = new ShadowrunCharacter
        {
            DiscordUserId = 444444444,
            Name = "Decker",
            Metatype = "Human"
        };
        await _databaseService.CreateCharacterAsync(character);

        var deck = new Cyberdeck
        {
            CharacterId = character.Id,
            Name = "Cyber-6",
            MPCP = 6,
            ActiveMemory = 100,
            Hardening = 4
        };
        await _databaseService.CreateCyberdeckAsync(deck);

        // Act
        var loaded = await _databaseService.GetCyberdeckAsync(deck.Id);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal("Cyber-6", loaded.Name);
        Assert.Equal(6, loaded.MPCP);
    }

    [Fact]
    public async Task GetAllCharacters_ReturnsAllCharacters()
    {
        // Arrange
        await _databaseService.CreateCharacterAsync(new ShadowrunCharacter
        {
            DiscordUserId = 1,
            Name = "Char 1",
            Metatype = "Human"
        });
        await _databaseService.CreateCharacterAsync(new ShadowrunCharacter
        {
            DiscordUserId = 2,
            Name = "Char 2",
            Metatype = "Elf"
        });
        await _databaseService.CreateCharacterAsync(new ShadowrunCharacter
        {
            DiscordUserId = 3,
            Name = "Char 3",
            Metatype = "Ork"
        });

        // Act
        var all = await _databaseService.GetAllCharactersAsync();

        // Assert
        Assert.Equal(3, all.Count);
    }
}
