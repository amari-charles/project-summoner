# Unit Testing Infrastructure

This directory contains the unit testing infrastructure for Fateforged using the [GUT (Godot Unit Test)](https://github.com/bitwes/Gut) framework.

## Directory Structure

```
tests/
├── unit/              # Unit tests for individual services
│   ├── test_economy_service.gd
│   ├── test_campaign_service.gd
│   └── test_battle_context.gd
├── integration/       # Integration tests (future)
├── mocks/             # Mock implementations for testing
│   ├── mock_economy_service.gd
│   └── mock_collection_service.gd
└── README.md
```

## Running Tests

### From Godot Editor

1. Open the project in Godot
2. Go to **Project → Project Settings → Plugins**
3. Enable the "Gut" plugin
4. Click the "GUT" button in the bottom panel
5. Click "Run All" to run all tests

### From Command Line (requires Godot CLI)

```bash
godot --headless -s addons/gut/gut_cmdln.gd
```

## Writing Tests

### Test File Naming

- All test files must be prefixed with `test_` (e.g., `test_economy_service.gd`)
- Place unit tests in `tests/unit/`
- Place integration tests in `tests/integration/`

### Basic Test Structure

```gdscript
extends GutTest

var service_under_test: SomeService
var mock_repo: MockProfileRepo

func before_each() -> void:
    # Set up fresh instances for each test
    mock_repo = MockProfileRepo.new()
    service_under_test = SomeService.new()
    service_under_test.init_for_testing(mock_repo)

func after_each() -> void:
    # Clean up
    if service_under_test:
        service_under_test.free()
    if mock_repo:
        mock_repo.free()

func test_something_works() -> void:
    # Arrange
    mock_repo.set_resources({"gold": 100})

    # Act
    service_under_test.do_something()

    # Assert
    assert_eq(service_under_test.get_result(), expected_value)
```

## Dependency Injection Pattern

Services support dependency injection for testability. The C# `ProfileRepo` autoload
is used directly (no GDScript wrapper or mock needed for integration tests).

## Mock Classes

### MockEconomyService

Lightweight mock for `EconomyService`. Tracks calls to `add_gold()`, `spend()`, etc.

### MockCollectionService

Lightweight mock for `CollectionService`. Tracks calls to `grant_card()`.

## Test Coverage

Current test coverage:

| Service | Coverage | Notes |
|---------|----------|-------|
| EconomyService | High | Resource operations, signals, affordability |
| CampaignService | High | Progress tracking, unlocks, rewards |
| BattleContext | Medium | State machine, card tracking, hero stats |

## Adding New Tests

1. Create a new test file in the appropriate directory
2. Extend `GutTest`
3. Use `before_each()` and `after_each()` for setup/teardown
4. Name test methods with `test_` prefix
5. Use GUT assertions: `assert_eq()`, `assert_true()`, `assert_false()`, etc.

## Future Improvements

- [ ] Add CI/CD script to run tests on PR
- [ ] Add integration tests for full system flows
- [ ] Add performance/benchmark tests
