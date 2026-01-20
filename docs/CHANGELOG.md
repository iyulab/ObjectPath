# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.3.0] - 2025-01-20

### Added

- **Bracket string literal syntax**: Access keys containing special characters using `["key.name"]` or `['key.name']`
  - Supports keys with dots: `["my.config"]`
  - Supports keys with brackets: `["items[0]"]`
  - Supports escaped quotes: `["key\"quote"]`
  - Works with JSON, dictionaries, and mixed paths
- **Enhanced type conversion**: Support for `Enum`, `Guid`, and `Nullable<T>` types in `GetValue<T>()`
- **Smarter JSON number handling**: JSON integers return `int`/`long`, floats return `double` (previously all returned `decimal`)
- **XML documentation**: Added comprehensive XML docs to `ToExpando()` method
- **New tests**: 24 additional test cases for new features (119 total)
- **Cache management**: `ClearCaches()` public method for testing and memory management
- **Cache size limits**: Automatic cache trimming when exceeding 1000 entries to prevent memory leaks
- **Dictionary reflection caching**: Cached `IDictionary<string, T>` interface info for improved performance

### Changed

- **Path parser**: Replaced simple `Split()` with stateful tokenizer for bracket literal support

### Fixed

- **TryGetValue return logic**: Now correctly returns `true` for valid paths even when the value is `null`
- **Exception message consistency**: All exceptions now include the full path for easier debugging
- **JSON array bounds checking**: Proper exception thrown for out-of-bounds array access in JSON

## [1.2.0] - 2024-11-27

### Added

- **Generic type conversion**: `GetValue<T>()` and `TryGetValue<T>()` methods for automatic type conversion
- **TryGetValue pattern**: Non-throwing alternatives that return `bool` success indicator
- **Extended dictionary support**:
  - `IDictionary` (non-generic) - Hashtable, etc.
  - `IDictionary<string, T>` via reflection
  - `ExpandoObject` support
- **Extension method enhancements**:
  - Added `ignoreCase` parameter to all extension methods
  - New `GetValueByPath<T>()` generic extension
  - New `TryGetValueByPath()` and `TryGetValueByPath<T>()` extensions
- **Comprehensive test coverage**: 95 tests covering edge cases

### Changed

- **Exception messages**: Now include full path context for easier debugging
- **Empty/null path handling**: Returns original object instead of throwing
- **Target frameworks**: Added .NET 9.0 and .NET 10.0 support

### Fixed

- Improved null handling in property chains

## [1.0.0] - 2024-06-05

### Added

- Initial release
- Basic path expression parsing with dot notation
- Array and list index access with bracket notation
- `JsonElement` support for System.Text.Json
- `Dictionary<string, object>` support
- Case-insensitive matching (default)
- Property and field caching for performance
- `GetValueByPath()` extension method
- `GetValueByPathOrNull()` for safe access
- `ToExpando()` dictionary extension
