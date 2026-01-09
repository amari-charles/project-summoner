# Running Tests

This project uses two testing frameworks:
- **GdUnit4Net** for C# code (recommended for new tests)
- **GUT (Godot Unit Test)** for GDScript code (legacy)

## C# Tests with GdUnit4Net

### Running All C# Tests

```bash
dotnet test
```

### Running Specific Tests

```bash
# Run tests by class name
dotnet test --filter "FullyQualifiedName~CardCatalogTest"

# Run a single test
dotnet test --filter "FullyQualifiedName~GetCard_ReturnsCardDefinition"

# Run tests with verbose output
dotnet test --logger "console;verbosity=detailed"
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

- **Runs without Godot runtime** by default (fast, ~60ms for 19 tests)
- Use `[RequireGodotRuntime]` attribute for tests that need Godot features
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

GUT tests require running through the Godot editor due to .NET compatibility issues with headless mode.

### Running GUT Tests

1. Open the project in Godot Editor
2. Go to **Project > Tools > GUT** (or press the GUT panel)
3. Click **Run All**

### Test Location

GDScript tests live in `tests/` with `test_` prefix:
```
tests/
  test_example.gd
```

## CI/CD

For CI pipelines, use `dotnet test` for C# tests. GUT tests in headless mode are currently unstable with Godot .NET.

```yaml
# Example GitHub Actions
- name: Run C# Tests
  run: dotnet test --configuration Release
```
