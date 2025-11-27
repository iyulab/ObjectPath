# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2024-11-27

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
