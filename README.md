# ObjectPath

[![NuGet](https://img.shields.io/nuget/v/ObjectPath.svg)](https://www.nuget.org/packages/ObjectPath/)
[![License](https://img.shields.io/github/license/user/ObjectPath.svg)](LICENSE)

A simple and intuitive library for accessing object values using string path expressions in .NET.

## Features

- Access nested properties and fields using dot notation (`Address.City`)
- Array and list index access using bracket notation (`Items[0]`)
- JSON element support (`System.Text.Json`)
- Dictionary support (`IDictionary<string, T>`, `IDictionary`, `ExpandoObject`)
- Generic type conversion (`GetValue<T>`)
- Non-throwing TryGetValue pattern
- Case-insensitive matching by default
- Property and field caching for performance
- .NET 8.0, 9.0, and 10.0 support

## Installation

```bash
dotnet add package ObjectPath
```

## Quick Start

```csharp
using ObjectPathLibrary;

var person = new
{
    Name = "John",
    Age = 30,
    Address = new { City = "New York", Street = "123 Main St" }
};

// Extension method
var city = person.GetValueByPath("Address.City"); // "New York"

// Static method
var name = ObjectPath.GetValue(person, "Name"); // "John"
```

## API Reference

### Static Methods

| Method | Description |
|--------|-------------|
| `GetValue(obj, path, ignoreCase)` | Gets value at path, throws on invalid path |
| `GetValue<T>(obj, path, ignoreCase)` | Gets value with type conversion |
| `TryGetValue(obj, path, out value, ignoreCase)` | Non-throwing, returns success bool |
| `TryGetValue<T>(obj, path, out value, ignoreCase)` | Non-throwing with type conversion |

### Extension Methods

| Method | Description |
|--------|-------------|
| `obj.GetValueByPath(path, ignoreCase)` | Gets value at path |
| `obj.GetValueByPath<T>(path, ignoreCase)` | Gets value with type conversion |
| `obj.GetValueByPathOrNull(path, ignoreCase)` | Returns null on invalid path |
| `obj.TryGetValueByPath(path, out value, ignoreCase)` | Non-throwing pattern |
| `obj.TryGetValueByPath<T>(path, out value, ignoreCase)` | Non-throwing with type conversion |

## Usage Examples

### Nested Objects

```csharp
var obj = new
{
    User = new
    {
        Profile = new { Name = "John", Email = "john@example.com" }
    }
};

var email = obj.GetValueByPath("User.Profile.Email"); // "john@example.com"
```

### Arrays and Lists

```csharp
var obj = new
{
    Numbers = new[] { 10, 20, 30 },
    Users = new[]
    {
        new { Name = "John" },
        new { Name = "Jane" }
    }
};

var first = obj.GetValueByPath("Numbers[0]");      // 10
var jane = obj.GetValueByPath("Users[1].Name");   // "Jane"
```

### JSON Elements

```csharp
using System.Text.Json;

var json = """
{
    "name": "John",
    "address": { "city": "New York" },
    "tags": ["developer", "designer"]
}
""";

var doc = JsonDocument.Parse(json);
var root = doc.RootElement;

var city = ObjectPath.GetValue(root, "address.city");  // "New York"
var tag = ObjectPath.GetValue(root, "tags[0]");        // "developer"
```

### Dictionaries

```csharp
// Dictionary<string, object>
var dict = new Dictionary<string, object>
{
    ["Name"] = "John",
    ["Address"] = new Dictionary<string, object>
    {
        ["City"] = "New York"
    }
};

var city = dict.GetValueByPath("Address.City"); // "New York"

// ExpandoObject
dynamic expando = new ExpandoObject();
expando.Name = "John";
expando.Age = 30;

var name = ObjectPath.GetValue((object)expando, "Name"); // "John"
```

### Generic Type Conversion

```csharp
var obj = new { Count = 42, Price = "19.99", Active = true };

int count = obj.GetValueByPath<int>("Count");        // 42
decimal price = obj.GetValueByPath<decimal>("Price"); // 19.99m
bool active = obj.GetValueByPath<bool>("Active");    // true
```

### TryGetValue Pattern

```csharp
var obj = new { Name = "John", Age = 30 };

// Non-throwing access
if (obj.TryGetValueByPath("Name", out var name))
{
    Console.WriteLine($"Name: {name}");
}

// With type conversion
if (obj.TryGetValueByPath<int>("Age", out var age))
{
    Console.WriteLine($"Age: {age}");
}

// Safe access for missing properties
if (!obj.TryGetValueByPath("Email", out _))
{
    Console.WriteLine("Email not found");
}
```

### Case Sensitivity

```csharp
var obj = new { Name = "John", age = 30 };

// Case-insensitive (default)
var name1 = ObjectPath.GetValue(obj, "name");           // "John"
var name2 = ObjectPath.GetValue(obj, "NAME");           // "John"

// Case-sensitive
var name3 = ObjectPath.GetValue(obj, "Name", ignoreCase: false);  // "John"
var age = ObjectPath.GetValue(obj, "age", ignoreCase: false);     // 30

// Throws InvalidObjectPathException
ObjectPath.GetValue(obj, "name", ignoreCase: false);  // Error!
```

### Error Handling

```csharp
var obj = new { Name = "John" };

// Option 1: GetValueByPathOrNull
var value = obj.GetValueByPathOrNull("NonExistent"); // null

// Option 2: TryGetValue pattern
if (!obj.TryGetValueByPath("NonExistent", out var result))
{
    // Handle missing property
}

// Option 3: Catch exception
try
{
    var invalid = obj.GetValueByPath("NonExistent");
}
catch (InvalidObjectPathException ex)
{
    // ex.Message contains the full path for debugging
    Console.WriteLine(ex.Message);
    // "Property or field 'NonExistent' not found in path 'NonExistent'."
}
```

## Supported Types

| Type | Support |
|------|---------|
| Anonymous types | ✅ |
| Classes/Records | ✅ |
| `JsonElement` | ✅ |
| `Dictionary<string, T>` | ✅ |
| `IDictionary` (Hashtable, etc.) | ✅ |
| `ExpandoObject` | ✅ |
| Arrays (`T[]`) | ✅ |
| `List<T>` | ✅ |
| `IList` | ✅ |

## Path Syntax

| Syntax | Description | Example |
|--------|-------------|---------|
| `.` | Property access | `Address.City` |
| `[n]` | Array/list index | `Items[0]` |
| Combined | Nested access | `Users[0].Address.City` |

## Performance

- Property and field lookups are cached using `ConcurrentDictionary`
- Reflection is used only on first access per type/property combination
- Subsequent accesses use cached `PropertyInfo`/`FieldInfo`

## Requirements

- .NET 8.0 or later

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
