# ObjectPath API Documentation

Complete API reference for the ObjectPath library.

## Namespace

```csharp
using ObjectPathLibrary;
```

## Static Class: ObjectPath

### GetValue

Gets the value at the specified path.

```csharp
public static object? GetValue(object? obj, string path, bool ignoreCase = true)
```

**Parameters:**
- `obj`: The source object to traverse
- `path`: The path expression (e.g., `"Address.City"` or `"Items[0].Name"`)
- `ignoreCase`: If `true`, property names are matched case-insensitively (default: `true`)

**Returns:** The value at the specified path, or `null` if the source object is null

**Exceptions:**
- `InvalidObjectPathException`: Thrown when the path is invalid or property not found

**Example:**
```csharp
var obj = new { Name = "John", Address = new { City = "NYC" } };
var city = ObjectPath.GetValue(obj, "Address.City"); // "NYC"
```

---

### GetValue&lt;T&gt;

Gets the value at the specified path and converts it to the specified type.

```csharp
public static T? GetValue<T>(object? obj, string path, bool ignoreCase = true)
```

**Parameters:**
- `T`: The expected return type
- `obj`: The source object
- `path`: The path expression
- `ignoreCase`: Case sensitivity flag (default: `true`)

**Returns:** The value cast to type `T`, or `default(T)` if source is null

**Exceptions:**
- `InvalidObjectPathException`: Thrown when path is invalid or type conversion fails

**Example:**
```csharp
var obj = new { Age = 30, Price = "19.99" };
int age = ObjectPath.GetValue<int>(obj, "Age");         // 30
decimal price = ObjectPath.GetValue<decimal>(obj, "Price"); // 19.99m
```

---

### TryGetValue

Attempts to get a value without throwing an exception.

```csharp
public static bool TryGetValue(object? obj, string path, out object? value, bool ignoreCase = true)
```

**Parameters:**
- `obj`: The source object
- `path`: The path expression
- `value`: When successful, contains the retrieved value
- `ignoreCase`: Case sensitivity flag (default: `true`)

**Returns:** `true` if the value was found; otherwise, `false`

**Example:**
```csharp
var obj = new { Name = "John" };
if (ObjectPath.TryGetValue(obj, "Name", out var name))
{
    Console.WriteLine(name); // "John"
}
```

---

### TryGetValue&lt;T&gt;

Attempts to get a typed value without throwing an exception.

```csharp
public static bool TryGetValue<T>(object? obj, string path, out T? value, bool ignoreCase = true)
```

**Parameters:**
- `T`: The expected return type
- `obj`: The source object
- `path`: The path expression
- `value`: When successful, contains the typed value
- `ignoreCase`: Case sensitivity flag (default: `true`)

**Returns:** `true` if value was found and converted successfully; otherwise, `false`

**Example:**
```csharp
var obj = new { Count = 42 };
if (ObjectPath.TryGetValue<int>(obj, "Count", out var count))
{
    Console.WriteLine(count); // 42
}
```

---

## Extension Methods: ObjectPathExtensions

### GetValueByPath

Extension method to get value at path.

```csharp
public static object? GetValueByPath(this object obj, string path, bool ignoreCase = true)
```

**Example:**
```csharp
var obj = new { Name = "John" };
var name = obj.GetValueByPath("Name"); // "John"
```

---

### GetValueByPath&lt;T&gt;

Extension method to get typed value at path.

```csharp
public static T? GetValueByPath<T>(this object obj, string path, bool ignoreCase = true)
```

**Example:**
```csharp
var obj = new { Age = 30 };
int age = obj.GetValueByPath<int>("Age"); // 30
```

---

### GetValueByPathOrNull

Gets value at path, returning null instead of throwing on invalid path.

```csharp
public static object? GetValueByPathOrNull(this object obj, string path, bool ignoreCase = true)
```

**Example:**
```csharp
var obj = new { Name = "John" };
var missing = obj.GetValueByPathOrNull("NonExistent"); // null (no exception)
```

---

### TryGetValueByPath

Extension method for non-throwing value retrieval.

```csharp
public static bool TryGetValueByPath(this object obj, string path, out object? value, bool ignoreCase = true)
```

**Example:**
```csharp
var obj = new { Name = "John" };
if (obj.TryGetValueByPath("Name", out var name))
{
    Console.WriteLine(name);
}
```

---

### TryGetValueByPath&lt;T&gt;

Extension method for non-throwing typed value retrieval.

```csharp
public static bool TryGetValueByPath<T>(this object obj, string path, out T? value, bool ignoreCase = true)
```

**Example:**
```csharp
var obj = new { Count = 42 };
if (obj.TryGetValueByPath<int>("Count", out var count))
{
    Console.WriteLine(count);
}
```

---

## Extension Methods: DictionaryExtensions

### ToExpando

Converts a dictionary to an ExpandoObject for dynamic access.

```csharp
public static dynamic? ToExpando(this IDictionary<string, object?>? dictionary)
```

**Example:**
```csharp
var dict = new Dictionary<string, object> { ["Name"] = "John", ["Age"] = 30 };
dynamic expando = dict.ToExpando();
Console.WriteLine(expando.Name); // "John"
```

---

## Exception: InvalidObjectPathException

Thrown when a path expression cannot be resolved.

```csharp
public class InvalidObjectPathException : Exception
```

**Properties:**
- `Message`: Contains detailed error information including the full path

**Common Causes:**
- Property or field not found
- Array index out of bounds
- Type conversion failure
- Accessing property on null value

**Example Messages:**
```
"Property or field 'NonExistent' not found in path 'User.NonExistent'."
"Invalid array index '10' in path 'Items[10]'."
"Cannot convert value of type 'String' to 'Int32' at path 'Age'."
```

---

## Path Syntax Reference

| Pattern | Description | Example Path | Object Structure |
|---------|-------------|--------------|------------------|
| `Property` | Direct property access | `"Name"` | `{ Name: "John" }` |
| `A.B` | Nested property | `"Address.City"` | `{ Address: { City: "NYC" } }` |
| `[n]` | Array/list index | `"[0]"` | `["first", "second"]` |
| `A[n]` | Property then index | `"Items[0]"` | `{ Items: [1, 2, 3] }` |
| `A[n].B` | Index then property | `"Users[0].Name"` | `{ Users: [{ Name: "John" }] }` |
| `A.B[n].C` | Mixed access | `"Data.Items[0].Value"` | Complex nested structure |

---

## Supported Object Types

| Type | Read Support | Notes |
|------|--------------|-------|
| Anonymous types | ✅ | Full property access |
| Classes | ✅ | Public properties and fields |
| Records | ✅ | Full property access |
| Structs | ✅ | Public properties and fields |
| `JsonElement` | ✅ | System.Text.Json support |
| `Dictionary<string, T>` | ✅ | Key-based access |
| `IDictionary` | ✅ | Hashtable, etc. |
| `ExpandoObject` | ✅ | Dynamic property access |
| `T[]` | ✅ | Index-based access |
| `List<T>` | ✅ | Index-based access |
| `IList` | ✅ | Index-based access |
| `ReadOnlyCollection<T>` | ✅ | Index-based access |
