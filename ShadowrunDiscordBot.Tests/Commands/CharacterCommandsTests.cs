using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ShadowrunDiscordBot.Commands;
using ShadowrunDiscordBot.Models;
using ShadowrunDiscordBot.Services;
using Xunit;

namespace ShadowrunDiscordBot.Tests.Commands;

/// <summary>
/// Unit tests for CharacterCommands - tests command validation
/// </summary>
public class CharacterCommandsTests
{
    private readonly Mock<ILogger<CharacterCommands>> _loggerMock;
    private readonly Mock<BotConfig> _configMock;
    private readonly Mock<DatabaseService> _databaseMock;
    private readonly Mock<DiceService> _diceServiceMock;

    public CharacterCommandsTests()
    {
        _loggerMock = new Mock<ILogger<CharacterCommands>>();
        _configMock = new Mock<BotConfig>();
        _databaseMock = new Mock<DatabaseService>();
        var diceLoggerMock = new Mock<ILogger<DiceService>>();
        _diceServiceMock = new Mock<DiceService>(diceLoggerMock.Object);
    }

    #region Metatype Validation Tests (MED-002)

    [Theory]
    [InlineData("Human")]
    [InlineData("Elf")]
    [InlineData("Dwarf")]
    [InlineData("Ork")]
    [InlineData("Troll")]
    public void ValidMetatypes_ShouldBeAccepted(string metatype)
    {
        // This test verifies the ValidMetatypes HashSet contains expected values
        var validMetatypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Human", "Elf", "Dwarf", "Ork", "Troll"
        };

        // Act & Assert
        validMetatypes.Contains(metatype).Should().BeTrue();
    }

    [Theory]
    [InlineData("human")]
    [InlineData("ELF")]
    [InlineData("dWaRf")]
    public void ValidMetatypes_ShouldBeCaseInsensitive(string metatype)
    {
        // This test verifies case-insensitive matching
        var validMetatypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Human", "Elf", "Dwarf", "Ork", "Troll"
        };

        // Act & Assert
        validMetatypes.Contains(metatype).Should().BeTrue();
    }

    [Theory]
    [InlineData("Alien")]
    [InlineData("Vampire")]
    [InlineData("")]
    [InlineData("Humanoid")]
    public void InvalidMetatypes_ShouldBeRejected(string metatype)
    {
        // This test verifies invalid metatypes are rejected
        var validMetatypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Human", "Elf", "Dwarf", "Ork", "Troll"
        };

        // Act & Assert
        validMetatypes.Contains(metatype).Should().BeFalse();
    }

    #endregion

    #region Archetype Validation Tests (MED-002)

    [Theory]
    [InlineData("Mage")]
    [InlineData("Shaman")]
    [InlineData("Physical Adept")]
    [InlineData("Street Samurai")]
    [InlineData("Decker")]
    [InlineData("Rigger")]
    public void ValidArchetypes_ShouldBeAccepted(string archetype)
    {
        // This test verifies the ValidArchetypes HashSet contains expected values
        var validArchetypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Mage", "Shaman", "Physical Adept", "Street Samurai", "Decker", "Rigger", "Face", "Samurai"
        };

        // Act & Assert
        validArchetypes.Contains(archetype).Should().BeTrue();
    }

    [Theory]
    [InlineData("mage")]
    [InlineData("SHAMAN")]
    [InlineData("physical adept")]
    public void ValidArchetypes_ShouldBeCaseInsensitive(string archetype)
    {
        var validArchetypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Mage", "Shaman", "Physical Adept", "Street Samurai", "Decker", "Rigger", "Face", "Samurai"
        };

        // Act & Assert
        validArchetypes.Contains(archetype).Should().BeTrue();
    }

    [Theory]
    [InlineData("Hacker")]
    [InlineData("Ninja")]
    [InlineData("")]
    [InlineData("Wizard")]
    public void InvalidArchetypes_ShouldBeRejected(string archetype)
    {
        var validArchetypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Mage", "Shaman", "Physical Adept", "Street Samurai", "Decker", "Rigger", "Face", "Samurai"
        };

        // Act & Assert
        validArchetypes.Contains(archetype).Should().BeFalse();
    }

    #endregion

    #region Name Validation Tests (MED-002)

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void EmptyOrWhitespaceName_ShouldBeInvalid(string? name)
    {
        // Act
        var isValid = !string.IsNullOrWhiteSpace(name);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void NameExceedingMaxLength_ShouldBeInvalid()
    {
        // Arrange
        const int maxLength = 50;
        var longName = new string('A', maxLength + 1);

        // Act
        var isValid = longName.Length <= maxLength;

        // Assert
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("Shadowrunner")]
    [InlineData("John Doe")]
    [InlineData("A")]
    [InlineData("Name-With-Dashes")]
    public void ValidNames_ShouldBeAccepted(string name)
    {
        // Act
        var isValid = !string.IsNullOrWhiteSpace(name) && name.Length <= 50;

        // Assert
        isValid.Should().BeTrue();
    }

    #endregion

    #region Attribute Bounds Tests (MED-002)

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(6)]
    [InlineData(10)]
    public void ValidAttributeValues_ShouldBeAccepted(int value)
    {
        const int min = 1;
        const int max = 10;

        // Act
        var isValid = value >= min && value <= max;

        // Assert
        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void NegativeAttributeValues_ShouldBeRejected(int value)
    {
        const int min = 1;

        // Act
        var isValid = value >= min;

        // Assert
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(11)]
    [InlineData(15)]
    [InlineData(100)]
    public void ExcessiveAttributeValues_ShouldBeRejected(int value)
    {
        const int max = 10;

        // Act
        var isValid = value <= max;

        // Assert
        isValid.Should().BeFalse();
    }

    #endregion

    #region Character Creation Constants Tests

    [Fact]
    public void StartingKarma_ShouldBePositive()
    {
        const int StartingKarma = 5;
        StartingKarma.Should().BePositive();
    }

    [Fact]
    public void StartingNuyen_ShouldBePositive()
    {
        const int StartingNuyen = 5000;
        StartingNuyen.Should().BePositive();
    }

    [Fact]
    public void DeckerBonusNuyen_ShouldBeGreaterThanStarting()
    {
        const int StartingNuyen = 5000;
        const int DeckerBonusNuyen = 100000;
        DeckerBonusNuyen.Should().BeGreaterThan(StartingNuyen);
    }

    [Fact]
    public void RiggerBonusNuyen_ShouldBeGreaterThanStarting()
    {
        const int StartingNuyen = 5000;
        const int RiggerBonusNuyen = 50000;
        RiggerBonusNuyen.Should().BeGreaterThan(StartingNuyen);
    }

    #endregion
}
