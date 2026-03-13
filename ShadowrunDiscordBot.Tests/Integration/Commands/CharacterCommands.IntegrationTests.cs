using ShadowrunDiscordBot.Models;
using Xunit;

namespace ShadowrunDiscordBot.Tests.Integration.Commands;

/// <summary>
/// Integration tests for character commands
/// </summary>
public class CharacterCommandsIntegrationTests : IntegrationTestBase
{
    private const ulong TestUserId = 123456789012345678;

    [Fact]
    public async Task CreateCharacter_ValidData_CreatesSuccessfully()
    {
        // Arrange
        var character = new ShadowrunCharacter
        {
            DiscordUserId = TestUserId,
            Name = "Test Character",
            Metatype = "Human",
            Archetype = "Street Samurai",
            Body = 5,
            Quickness = 6,
            Strength = 5,
            Charisma = 3,
            Intelligence = 4,
            Willpower = 4
        };

        // Act
        var created = await _databaseService.CreateCharacterAsync(character);

        // Assert
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal("Test Character", created.Name);
        Assert.Equal(TestUserId, created.DiscordUserId);
    }

    [Fact]
    public async Task GetCharacter_ExistingCharacter_ReturnsCharacter()
    {
        // Arrange
        var character = new ShadowrunCharacter
        {
            DiscordUserId = TestUserId,
            Name = "Find Me",
            Metatype = "Elf",
            Archetype = "Decker"
        };
        await _databaseService.CreateCharacterAsync(character);

        // Act
        var found = await _databaseService.GetCharacterByNameAsync(TestUserId, "Find Me");

        // Assert
        Assert.NotNull(found);
        Assert.Equal("Find Me", found.Name);
    }

    [Fact]
    public async Task GetCharacter_NonExistent_ReturnsNull()
    {
        // Act
        var found = await _databaseService.GetCharacterByNameAsync(TestUserId, "NonExistent");

        // Assert
        Assert.Null(found);
    }

    [Fact]
    public async Task GetUserCharacters_MultipleCharacters_ReturnsAll()
    {
        // Arrange
        await _databaseService.CreateCharacterAsync(new ShadowrunCharacter
        {
            DiscordUserId = TestUserId,
            Name = "Character 1",
            Metatype = "Human"
        });
        await _databaseService.CreateCharacterAsync(new ShadowrunCharacter
        {
            DiscordUserId = TestUserId,
            Name = "Character 2",
            Metatype = "Ork"
        });
        await _databaseService.CreateCharacterAsync(new ShadowrunCharacter
        {
            DiscordUserId = 999999999,
            Name = "Other User Character",
            Metatype = "Dwarf"
        });

        // Act
        var characters = await _databaseService.GetUserCharactersAsync(TestUserId);

        // Assert
        Assert.Equal(2, characters.Count);
        Assert.All(characters, c => Assert.Equal(TestUserId, c.DiscordUserId));
    }

    [Fact]
    public async Task UpdateCharacter_ValidUpdate_SavesChanges()
    {
        // Arrange
        var character = new ShadowrunCharacter
        {
            DiscordUserId = TestUserId,
            Name = "Update Test",
            Body = 3,
            Karma = 0
        };
        await _databaseService.CreateCharacterAsync(character);

        // Act
        character.Body = 5;
        character.Karma = 10;
        await _databaseService.UpdateCharacterAsync(character);
        
        var updated = await _databaseService.GetCharacterByNameAsync(TestUserId, "Update Test");

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(5, updated.Body);
        Assert.Equal(10, updated.Karma);
    }

    [Fact]
    public async Task DeleteCharacter_ExistingCharacter_RemovesSuccessfully()
    {
        // Arrange
        var character = new ShadowrunCharacter
        {
            DiscordUserId = TestUserId,
            Name = "Delete Me",
            Metatype = "Human"
        };
        await _databaseService.CreateCharacterAsync(character);

        // Act
        var deleted = await _databaseService.DeleteCharacterAsync(character.Id);
        var found = await _databaseService.GetCharacterByNameAsync(TestUserId, "Delete Me");

        // Assert
        Assert.True(deleted);
        Assert.Null(found);
    }

    [Fact]
    public async Task DeleteCharacter_NonExistent_ReturnsFalse()
    {
        // Act
        var deleted = await _databaseService.DeleteCharacterAsync(99999);

        // Assert
        Assert.False(deleted);
    }

    [Fact]
    public async Task Character_WithSkills_ReturnsWithSkillsLoaded()
    {
        // Arrange
        var character = new ShadowrunCharacter
        {
            DiscordUserId = TestUserId,
            Name = "Skilled Character",
            Metatype = "Human"
        };
        await _databaseService.CreateCharacterAsync(character);

        await _databaseService.AddOrUpdateSkillAsync(character.Id, "Pistols", 6, "Ares Predator");
        await _databaseService.AddOrUpdateSkillAsync(character.Id, "Stealth", 5);

        // Act
        var loaded = await _databaseService.GetCharacterAsync(character.Id);

        // Assert
        Assert.NotNull(loaded);
        Assert.NotNull(loaded.Skills);
        Assert.Equal(2, loaded.Skills.Count);
        Assert.Contains(loaded.Skills, s => s.SkillName == "Pistols" && s.Rating == 6);
        Assert.Contains(loaded.Skills, s => s.SkillName == "Stealth" && s.Rating == 5);
    }
}
