# Contributing to Shadowrun Discord Bot

Thank you for your interest in contributing to the Shadowrun Discord Bot! This document provides guidelines and instructions for contributing.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Code Style Guidelines](#code-style-guidelines)
- [Commit Guidelines](#commit-guidelines)
- [Pull Request Process](#pull-request-process)

## Code of Conduct

By participating in this project, you agree to maintain a respectful and inclusive environment. Be kind, constructive, and helpful.

## Getting Started

1. Fork the repository
2. Clone your fork locally
3. Create a feature branch from `main`
4. Make your changes
5. Submit a pull request

## Development Setup

### Prerequisites

- .NET 8.0 SDK or later
- A Discord Bot Token (from [Discord Developer Portal](https://discord.com/developers/applications))
- Git

### Initial Setup

```bash
# Clone the repository
git clone https://github.com/your-username/shadowrun-discord-bot.git
cd shadowrun-discord-bot

# Restore dependencies
dotnet restore

# Copy environment example
cp .env.example .env

# Edit .env with your Discord token
# DISCORD_TOKEN=your_token_here

# Build the project
dotnet build

# Run the bot
dotnet run
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Code Style Guidelines

This project uses an `.editorconfig` file to enforce consistent code style. Most modern IDEs (Visual Studio, VS Code, JetBrains Rider) automatically apply these settings.

### C# Naming Conventions

| Type | Convention | Example |
|------|------------|---------|
| Classes, Methods, Properties | PascalCase | `CharacterService`, `GetCharacterAsync()` |
| Private Fields | _camelCase | `_discordClient`, `_logger` |
| Local Variables | camelCase | `character`, `diceResult` |
| Constants | PascalCase | `MaxDiceRolls`, `DefaultTimeout` |
| Interfaces | IPascalCase | `ICharacterRepository` |

### General Guidelines

1. **Use nullable reference types** - This project has nullable enabled. Use `?` for nullable types.
2. **Prefer async/await** - Use async methods for I/O operations.
3. **Use dependency injection** - Register services in `Program.cs`.
4. **Document public APIs** - Use XML documentation comments for public methods.
5. **Keep methods small** - Aim for single responsibility.

### Example Code Style

```csharp
/// <summary>
/// Retrieves a character by ID from the database.
/// </summary>
/// <param name="characterId">The unique identifier of the character.</param>
/// <returns>The character if found, null otherwise.</returns>
public async Task<Character?> GetCharacterAsync(int characterId)
{
    if (characterId <= 0)
    {
        _logger.LogWarning("Invalid character ID: {CharacterId}", characterId);
        return null;
    }

    var character = await _context.Characters
        .FirstOrDefaultAsync(c => c.Id == characterId);

    return character;
}
```

## Commit Guidelines

### Commit Message Format

```
type(scope): subject

body (optional)

footer (optional)
```

### Types

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `test`: Adding/updating tests
- `chore`: Maintenance tasks

### Examples

```
feat(combat): add initiative reroll command
fix(dice): correct edge case handling for glitch detection
docs(readme): update installation instructions
```

## Pull Request Process

1. **Create a feature branch** from `main`:
   ```bash
   git checkout -b feat/your-feature-name
   ```

2. **Make your changes** following the code style guidelines.

3. **Add/update tests** for your changes.

4. **Ensure all tests pass**:
   ```bash
   dotnet test
   dotnet build
   ```

5. **Commit your changes** using the commit message format.

6. **Push to your fork** and create a pull request.

7. **Fill out the PR template** completely.

8. **Address review feedback** promptly.

### PR Requirements

- [ ] Code compiles without warnings
- [ ] All tests pass
- [ ] New code has appropriate test coverage
- [ ] Documentation is updated if needed
- [ ] Commit messages follow the format

## Questions?

Feel free to open an issue for questions or discussions about the project.

---

Thank you for contributing! 🎲
