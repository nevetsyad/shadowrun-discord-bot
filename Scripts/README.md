# Development Scripts

This directory contains helper scripts for local development and CI checks.

## Available Scripts

### Primary Scripts

| Script | Purpose | Usage |
|--------|---------|-------|
| `run-all-checks.sh` | Run all CI checks locally before pushing | `./scripts/run-all-checks.sh` |
| `fix-code-style.sh` | Auto-fix code style violations | `./scripts/fix-code-style.sh` |
| `check-code-style.sh` | Check code style without fixing | `./scripts/check-code-style.sh` |
| `check-build.sh` | Verify code compiles without errors | `./scripts/check-build.sh` |
| `check-security.sh` | Check for vulnerable packages | `./scripts/check-security.sh` |

### Other Scripts

| Script | Purpose |
|--------|---------|
| `LoadTest.sh` | Load testing scenarios for the bot |

## Quick Start

### Before Every Push

```bash
# Run all CI checks locally
./scripts/run-all-checks.sh
```

This will run:
1. Code style check
2. Build check
3. Security check (advisory)
4. Tests

### Fixing Code Style Issues

```bash
# Auto-fix style violations
./scripts/fix-code-style.sh

# Review changes
git diff

# Commit fixes
git add .
git commit -m "style: apply code style fixes"
```

### Checking Specific Areas

```bash
# Just check if code compiles
./scripts/check-build.sh

# Just check code style
./scripts/check-code-style.sh

# Just check security
./scripts/check-security.sh
```

## How These Scripts Relate to CI

These scripts mirror the checks in the CI pipeline:

| Local Script | CI Job |
|--------------|--------|
| `check-code-style.sh` | EditorConfig Validation |
| `check-build.sh` | Build & Test (build portion) |
| `check-security.sh` | Security Scan (advisory) |
| `run-all-checks.sh` | All CI jobs combined |

## Exit Codes

All scripts follow standard exit code conventions:
- `0` = Success
- `1` = Failure (except `check-security.sh`, which always exits 0 since security is advisory)

## Making Scripts Executable

If you get "Permission denied" errors:

```bash
chmod +x scripts/*.sh
```

## Requirements

- **.NET 8 SDK** must be installed and in your PATH
- Project must be run from the repository root directory

## Troubleshooting

### "command not found: dotnet"

Ensure the .NET SDK 8.0 is installed and available in your PATH. Check with:

```bash
dotnet --version
```

Should output: `8.0.x`

### Script runs from wrong directory

These scripts automatically navigate to the project root. You can run them from any directory:

```bash
# From anywhere
/path/to/project/scripts/run-all-checks.sh

# Or from project root
./scripts/run-all-checks.sh
```

### Git diff shows no changes after fix-code-style.sh

This means your code already follows the style rules. Good job!

## See Also

- [CI Documentation](../docs/CI-DOCUMENTATION.md) - Full CI pipeline documentation
- [.editorconfig](../.editorconfig) - Code style rules
- [CI Workflow](../.github/workflows/ci.yml) - GitHub Actions workflow
