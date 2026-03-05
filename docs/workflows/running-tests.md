# Running Tests

This project uses two testing frameworks:
- **GdUnit4Net** for C# code (recommended for new tests)
- **GUT (Godot Unit Test)** for GDScript code (legacy)

## Quick Start (One Command)

Run the default local test flow (typecheck, C#, then GUT):

```bash
./tools/run_tests.sh
```

Common variants:

```bash
./tools/run_tests.sh --fast         # typecheck + dotnet
./tools/run_tests.sh --gut-only     # gut only
./tools/run_tests.sh --dotnet-only  # typecheck + dotnet
./tools/run_tests.sh --typecheck-only
./tools/run_tests.sh --skip-typecheck
```

## GDScript Type/Parse Check

Use the dedicated checker to catch strict-typing parse/reload issues early:

```bash
./tools/check_gdscript_types.sh
```

This script opens the project in headless editor mode, scans the output for GDScript parse/compile/type diagnostics, and fails fast when it finds any.
If needed, set `GODOT_BIN` or `GODOT_PATH` to point to your local Godot executable.

## C# Tests with GdUnit4Net

### Running All C# Tests

```bash
dotnet test --settings test.runsettings
```

The `test.runsettings` file configures the GODOT_BIN path for the test adapter.

### Runtime-Dependent C# Suites

A few C# suites intentionally require full Godot runtime types (for example `Godot.Collections.Dictionary` payload paths).  
Mark these tests/suites with `[RequireGodotRuntime]` so `dotnet test` can execute them correctly via the gdUnit runtime bridge.

### Running Specific Tests

```bash
# Run tests by class name
dotnet test --settings test.runsettings --filter "FullyQualifiedName~CardCatalogTest"

# Run a single test
dotnet test --settings test.runsettings --filter "FullyQualifiedName~GetCard_ReturnsCardDefinition"

# Run tests with verbose output
dotnet test --settings test.runsettings --logger "console;verbosity=detailed"
```

### Test Location

C# tests live in `tests/csharp/` mirroring the source structure:
```
tests/csharp/
  Cards/
    CardCatalogTest.cs
    FormationPresetsTest.cs
```

### Writing New Tests

```csharp
namespace ProjectSummoner.Tests.Cards;

using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class MyTest
{
    [TestCase]
    public void MyMethod_DoesExpectedThing()
    {
        var result = MyClass.DoThing();
        AssertThat(result).IsEqual(expected);
    }
}
```

### Key Features

- **Most tests run without full Godot runtime** - fast CLI feedback for pure C# logic
- Uses `test.runsettings` to configure the test adapter's GODOT_BIN path
- Works with `dotnet test` command (CI/CD friendly)
- IDE integration with VS Code, Rider, Visual Studio

### Assertions

```csharp
AssertThat(value).IsEqual(expected);
AssertThat(value).IsNotNull();
AssertThat(value).IsTrue();
AssertThat(array.Length).IsGreater(0);
AssertThat(floatValue).IsEqualApprox(expected, tolerance);
```

## GDScript Tests with GUT

GUT tests can run in two modes depending on whether you need C#-dependent tests.

### Running GUT Tests (Headless Mode - Fast)

For quick test runs during development (C#-dependent tests will be skipped):

```bash
godot --headless -s addons/gut/gut_cmdln.gd -gdir=res://tests/unit -gexit
```

**Results:** ~284 passing, ~43 pending (C#-dependent tests gracefully skip)

### Running GUT Tests (Full - With C# Support)

To run ALL tests including those that depend on C# classes (SpatialGrid, TargetingConfigRegistry, pool containers, etc.), use the Godot .NET version:

```bash
# Using Godot .NET (Mono) version
"/path/to/Godot_mono.app/Contents/MacOS/Godot" -s addons/gut/gut_cmdln.gd -gdir=res://tests/unit -gexit

# Example on macOS:
"/Applications/Godot_mono.app/Contents/MacOS/Godot" -s addons/gut/gut_cmdln.gd -gdir=res://tests/unit -gexit
```

**Results:** ~326 passing, ~1 pending (full coverage)

### Why Two Modes?

The standard Godot binary doesn't include .NET support. Tests that interact with C# classes (like `SpatialGrid`, `TargetingConfigRegistry`, or managers with C# dependencies) require the Godot .NET version to run.

| Test Category | Headless (Standard) | With .NET |
|--------------|---------------------|-----------|
| Pure GDScript tests | Pass | Pass |
| C#-dependent tests | Skip (pending) | Pass |

### Setting Up Godot .NET Alias (Recommended)

To make running full tests easier, add an alias to your shell config:

```bash
# In ~/.zshrc or ~/.bashrc
alias godot-mono="/Applications/Godot_mono.app/Contents/MacOS/Godot"
```

Then run tests with:
```bash
godot-mono -s addons/gut/gut_cmdln.gd -gdir=res://tests/unit -gexit
```

### Running from Godot Editor

For interactive test runs with full C# support:

1. Open the project in Godot Editor (.NET version)
2. Go to **Project > Tools > GUT** (or press the GUT panel)
3. Click **Run All**

### Test Location

GDScript tests live in `tests/unit/` with `test_` prefix:
```
tests/unit/
  test_battle_context.gd
  test_spatial_grid.gd
  test_targeting_config_registry.gd
  ...
```

## CI/CD

For CI pipelines:

```yaml
# Example GitHub Actions
- name: Run C# Tests
  run: dotnet test --settings test.runsettings --configuration Release

- name: Run GDScript Tests (Headless)
  run: godot --headless -s addons/gut/gut_cmdln.gd -gdir=res://tests/unit -gexit

# For full test coverage, use Godot .NET in CI
- name: Run GDScript Tests (Full)
  run: |
    # Download and extract Godot .NET
    # Run: godot-mono -s addons/gut/gut_cmdln.gd -gdir=res://tests/unit -gexit
```

Note: The `test.runsettings` file contains the GODOT_BIN path. For CI, you may need to update this path or set the `GODOT_BIN` environment variable to match your CI environment's Godot installation.
