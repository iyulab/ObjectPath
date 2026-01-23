using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace ObjectPathLibrary
{
    public static class ObjectPath
    {
        /// <summary>
        /// Maximum number of entries in each cache. When exceeded, oldest entries are removed.
        /// </summary>
        private const int MaxCacheSize = 1000;

        private static readonly ConcurrentDictionary<(Type, string, bool), PropertyInfo?> PropertyCache = new();
        private static readonly ConcurrentDictionary<(Type, string, bool), FieldInfo?> FieldCache = new();
        private static readonly ConcurrentDictionary<Type, DictionaryTypeInfo?> DictionaryTypeCache = new();

        public static object? GetValue(object? obj, string path, bool ignoreCase = true)
        {
            if (obj == null) return null;
            if (string.IsNullOrEmpty(path)) return obj;

            var segments = ParsePath(path);
            int index = 0;

            while (obj != null && index < segments.Length)
            {
                var currentSegment = segments[index];

                try
                {
                    if (obj is JsonElement jsonElement)
                    {
                        obj = HandleJsonElement(jsonElement, currentSegment, segments, ref index, ignoreCase, path);
                    }
                    else if (int.TryParse(currentSegment, out var arrayIndex))
                    {
                        // Try dictionary key access first for numeric string keys
                        if (TryGetDictionaryValue(obj, currentSegment, ignoreCase, out var dictValue))
                        {
                            obj = dictValue;
                        }
                        else
                        {
                            obj = HandleArrayIndex(obj, arrayIndex, path);
                        }
                    }
                    else
                    {
                        obj = HandleObjectProperty(obj, currentSegment, ignoreCase, path);
                    }
                }
                catch (InvalidObjectPathException)
                {
                    throw; // Re-throw with existing message
                }

                index++;
            }

            return obj;
        }

        /// <summary>
        /// Attempts to get a value from an object using a path expression.
        /// </summary>
        /// <param name="obj">The source object.</param>
        /// <param name="path">The path expression (e.g., "Address.City" or "Items[0].Name").</param>
        /// <param name="value">When this method returns, contains the value if found; otherwise, null.</param>
        /// <param name="ignoreCase">If true, property names are matched case-insensitively. Default is true.</param>
        /// <returns>true if the value was found; otherwise, false.</returns>
        public static bool TryGetValue(object? obj, string path, out object? value, bool ignoreCase = true)
        {
            if (obj == null)
            {
                value = null;
                return false;
            }

            try
            {
                value = GetValue(obj, path, ignoreCase);
                return true;
            }
            catch (InvalidObjectPathException)
            {
                value = null;
                return false;
            }
        }

        /// <summary>
        /// Gets a value from an object using a path expression and casts it to the specified type.
        /// </summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="obj">The source object.</param>
        /// <param name="path">The path expression.</param>
        /// <param name="ignoreCase">If true, property names are matched case-insensitively. Default is true.</param>
        /// <returns>The value cast to type T, or default(T) if the source object is null.</returns>
        /// <exception cref="InvalidObjectPathException">Thrown when the path is invalid or type conversion fails.</exception>
        public static T? GetValue<T>(object? obj, string path, bool ignoreCase = true)
        {
            var value = GetValue(obj, path, ignoreCase);

            if (value == null)
            {
                return default;
            }

            try
            {
                // Direct cast if same type
                if (value is T typedValue)
                {
                    return typedValue;
                }

                var targetType = typeof(T);

                // Handle Nullable<T> types
                var underlyingType = Nullable.GetUnderlyingType(targetType);
                if (underlyingType != null)
                {
                    targetType = underlyingType;
                }

                // Handle Enum conversion from string or number
                if (targetType.IsEnum)
                {
                    if (value is string strValue)
                    {
                        return (T)Enum.Parse(targetType, strValue, ignoreCase);
                    }
                    return (T)Enum.ToObject(targetType, value);
                }

                // Handle Guid conversion
                if (targetType == typeof(Guid) && value is string guidStr)
                {
                    return (T)(object)Guid.Parse(guidStr);
                }

                // Try Convert for compatible types
                return (T)Convert.ChangeType(value, targetType);
            }
            catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException or ArgumentException)
            {
                throw new InvalidObjectPathException(
                    $"Cannot convert value of type '{value.GetType().Name}' to '{typeof(T).Name}' at path '{path}'.", ex);
            }
        }

        /// <summary>
        /// Attempts to get a value from an object using a path expression and cast it to the specified type.
        /// </summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="obj">The source object.</param>
        /// <param name="path">The path expression.</param>
        /// <param name="value">When this method returns, contains the typed value if found and conversion succeeded; otherwise, default(T).</param>
        /// <param name="ignoreCase">If true, property names are matched case-insensitively. Default is true.</param>
        /// <returns>true if the value was found and successfully converted; otherwise, false.</returns>
        public static bool TryGetValue<T>(object? obj, string path, out T? value, bool ignoreCase = true)
        {
            if (obj == null)
            {
                value = default;
                return false;
            }

            try
            {
                value = GetValue<T>(obj, path, ignoreCase);
                return true;
            }
            catch (InvalidObjectPathException)
            {
                value = default;
                return false;
            }
        }

        private static object? HandleJsonElement(JsonElement jsonElement, string currentSegment, string[] segments, ref int index, bool ignoreCase, string fullPath)
        {
            if (jsonElement.ValueKind == JsonValueKind.Object)
            {
                if (jsonElement.TryGetProperty(currentSegment, out var jsonProperty) ||
                    (ignoreCase && TryGetPropertyIgnoreCase(jsonElement, currentSegment, out jsonProperty)))
                {
                    return GetJsonElementValue(jsonProperty);
                }
                else
                {
                    throw new InvalidObjectPathException($"Property '{currentSegment}' not found in path '{fullPath}'.");
                }
            }
            else if (jsonElement.ValueKind == JsonValueKind.Array)
            {
                if (int.TryParse(currentSegment, out var jsonArrayIndex) &&
                    jsonArrayIndex >= 0 && jsonArrayIndex < jsonElement.GetArrayLength())
                {
                    return GetJsonElementValue(jsonElement[jsonArrayIndex]);
                }
                else
                {
                    throw new InvalidObjectPathException($"Invalid array index '{currentSegment}' in path '{fullPath}'.");
                }
            }
            else if (index == segments.Length - 1)
            {
                return GetJsonElementValue(jsonElement);
            }
            else
            {
                throw new InvalidObjectPathException($"Cannot access '{currentSegment}' on non-object/non-array JSON element in path '{fullPath}'.");
            }
        }

        /// <summary>
        /// Attempts to get a value from a dictionary using the specified key.
        /// Returns true if the object is a dictionary and contains the key.
        /// </summary>
        private static bool TryGetDictionaryValue(object obj, string key, bool ignoreCase, out object? value)
        {
            // Handle IDictionary<string, object> (most common case, including ExpandoObject)
            if (obj is IDictionary<string, object> dictStringObject)
            {
                if (dictStringObject.TryGetValue(key, out var dictValue) ||
                    (ignoreCase && TryGetValueIgnoreCase(dictStringObject, key, out dictValue)))
                {
                    value = dictValue;
                    return true;
                }
                value = null;
                return false;
            }

            // Handle non-generic IDictionary (Hashtable, etc.)
            if (obj is IDictionary nonGenericDict)
            {
                if (nonGenericDict.Contains(key))
                {
                    value = nonGenericDict[key];
                    return true;
                }
                if (ignoreCase)
                {
                    foreach (var k in nonGenericDict.Keys)
                    {
                        if (k is string keyStr && string.Equals(keyStr, key, StringComparison.OrdinalIgnoreCase))
                        {
                            value = nonGenericDict[k];
                            return true;
                        }
                    }
                }
                value = null;
                return false;
            }

            // Handle generic IDictionary<string, T> via cached reflection
            var objType = obj.GetType();
            var dictTypeInfo = GetCachedDictionaryTypeInfo(objType);

            if (dictTypeInfo != null)
            {
                // Try exact key match
                var parameters = new object?[] { key, null };
                var found = (bool)dictTypeInfo.TryGetValueMethod.Invoke(obj, parameters)!;
                if (found)
                {
                    value = parameters[1];
                    return true;
                }

                // Try case-insensitive match if enabled
                if (ignoreCase && dictTypeInfo.KeysProperty != null)
                {
                    var keys = (IEnumerable)dictTypeInfo.KeysProperty.GetValue(obj)!;
                    foreach (string k in keys)
                    {
                        if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
                        {
                            value = dictTypeInfo.IndexerProperty.GetValue(obj, new object[] { k });
                            return true;
                        }
                    }
                }
            }

            value = null;
            return false;
        }

        private static object? HandleArrayIndex(object obj, int arrayIndex, string fullPath)
        {
            if (obj is IList list && arrayIndex >= 0 && arrayIndex < list.Count)
            {
                return list[arrayIndex];
            }
            else if (obj is Array array && arrayIndex >= 0 && arrayIndex < array.Length)
            {
                return array.GetValue(arrayIndex);
            }
            else
            {
                throw new InvalidObjectPathException($"Invalid array index '{arrayIndex}' in path '{fullPath}'.");
            }
        }

        private static object? HandleObjectProperty(object obj, string currentSegment, bool ignoreCase, string fullPath)
        {
            // Handle IDictionary<string, object> (most common case, including ExpandoObject)
            if (obj is IDictionary<string, object> dictStringObject)
            {
                if (dictStringObject.TryGetValue(currentSegment, out var dictValue) ||
                    (ignoreCase && TryGetValueIgnoreCase(dictStringObject, currentSegment, out dictValue)))
                {
                    return dictValue;
                }
                else
                {
                    throw new InvalidObjectPathException($"Property '{currentSegment}' not found in path '{fullPath}'.");
                }
            }
            
            // Handle non-generic IDictionary (Hashtable, etc.)
            if (obj is System.Collections.IDictionary nonGenericDict)
            {
                // Try exact key match first
                if (nonGenericDict.Contains(currentSegment))
                {
                    return nonGenericDict[currentSegment];
                }
                
                // Try case-insensitive match if enabled
                if (ignoreCase)
                {
                    foreach (var key in nonGenericDict.Keys)
                    {
                        if (key is string keyStr && string.Equals(keyStr, currentSegment, StringComparison.OrdinalIgnoreCase))
                        {
                            return nonGenericDict[key];
                        }
                    }
                }
                
                throw new InvalidObjectPathException($"Property '{currentSegment}' not found in path '{fullPath}'.");
            }
            
            // Handle generic IDictionary<string, T> via cached reflection
            var objType = obj.GetType();
            var dictTypeInfo = GetCachedDictionaryTypeInfo(objType);

            if (dictTypeInfo != null)
            {
                // Try exact key match
                var parameters = new object?[] { currentSegment, null };
                var found = (bool)dictTypeInfo.TryGetValueMethod.Invoke(obj, parameters)!;
                if (found)
                {
                    return parameters[1];
                }

                // Try case-insensitive match if enabled
                if (ignoreCase && dictTypeInfo.KeysProperty != null)
                {
                    var keys = (System.Collections.IEnumerable)dictTypeInfo.KeysProperty.GetValue(obj)!;
                    foreach (string key in keys)
                    {
                        if (string.Equals(key, currentSegment, StringComparison.OrdinalIgnoreCase))
                        {
                            return dictTypeInfo.IndexerProperty.GetValue(obj, new object[] { key });
                        }
                    }
                }

                throw new InvalidObjectPathException($"Property '{currentSegment}' not found in path '{fullPath}'.");
            }
            
            // Handle regular objects (properties and fields)
            var propertyInfo = GetCachedPropertyInfo(objType, currentSegment, ignoreCase);
            if (propertyInfo != null)
            {
                return propertyInfo.GetValue(obj);
            }

            var fieldInfo = GetCachedFieldInfo(objType, currentSegment, ignoreCase);
            if (fieldInfo != null)
            {
                return fieldInfo.GetValue(obj);
            }

            throw new InvalidObjectPathException($"Property or field '{currentSegment}' not found in path '{fullPath}'.");
        }

        private static PropertyInfo? GetCachedPropertyInfo(Type type, string propertyName, bool ignoreCase)
        {
            var key = (type, propertyName, ignoreCase);
            if (!PropertyCache.TryGetValue(key, out var propertyInfo))
            {
                TrimCacheIfNeeded(PropertyCache);
                var flags = ignoreCase
                    ? BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase
                    : BindingFlags.Public | BindingFlags.Instance;
                propertyInfo = type.GetProperty(propertyName, flags);
                PropertyCache[key] = propertyInfo;
            }
            return propertyInfo;
        }

        private static FieldInfo? GetCachedFieldInfo(Type type, string fieldName, bool ignoreCase)
        {
            var key = (type, fieldName, ignoreCase);
            if (!FieldCache.TryGetValue(key, out var fieldInfo))
            {
                TrimCacheIfNeeded(FieldCache);
                var flags = ignoreCase
                    ? BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase
                    : BindingFlags.Public | BindingFlags.Instance;
                fieldInfo = type.GetField(fieldName, flags);
                FieldCache[key] = fieldInfo;
            }
            return fieldInfo;
        }

        /// <summary>
        /// Parses a path expression into segments, supporting:
        /// - Dot notation: "User.Name" → ["User", "Name"]
        /// - Bracket index: "Items[0]" → ["Items", "0"]
        /// - Bracket string literals: "Data[\"my.key\"]" or "Data['my.key']" → ["Data", "my.key"]
        /// </summary>
        private static string[] ParsePath(string path)
        {
            var segments = new List<string>();
            var current = new System.Text.StringBuilder();
            int i = 0;

            while (i < path.Length)
            {
                char c = path[i];

                if (c == '.')
                {
                    // Dot separator - finish current segment
                    if (current.Length > 0)
                    {
                        segments.Add(current.ToString());
                        current.Clear();
                    }
                    i++;
                }
                else if (c == '[')
                {
                    // Start of bracket expression
                    if (current.Length > 0)
                    {
                        segments.Add(current.ToString());
                        current.Clear();
                    }
                    i++;

                    if (i < path.Length && (path[i] == '"' || path[i] == '\''))
                    {
                        // String literal: ["key"] or ['key']
                        char quote = path[i];
                        i++;
                        while (i < path.Length && path[i] != quote)
                        {
                            if (path[i] == '\\' && i + 1 < path.Length)
                            {
                                // Handle escape sequences
                                i++;
                                current.Append(path[i]);
                            }
                            else
                            {
                                current.Append(path[i]);
                            }
                            i++;
                        }
                        if (i < path.Length) i++; // Skip closing quote
                        if (i < path.Length && path[i] == ']') i++; // Skip closing bracket

                        if (current.Length > 0)
                        {
                            segments.Add(current.ToString());
                            current.Clear();
                        }
                    }
                    else
                    {
                        // Numeric index: [0]
                        while (i < path.Length && path[i] != ']')
                        {
                            current.Append(path[i]);
                            i++;
                        }
                        if (i < path.Length) i++; // Skip closing bracket

                        if (current.Length > 0)
                        {
                            segments.Add(current.ToString());
                            current.Clear();
                        }
                    }
                }
                else if (c == ']')
                {
                    // Unexpected closing bracket - skip
                    i++;
                }
                else
                {
                    // Regular character
                    current.Append(c);
                    i++;
                }
            }

            // Add final segment
            if (current.Length > 0)
            {
                segments.Add(current.ToString());
            }

            return segments.ToArray();
        }

        private static DictionaryTypeInfo? GetCachedDictionaryTypeInfo(Type type)
        {
            if (!DictionaryTypeCache.TryGetValue(type, out var typeInfo))
            {
                TrimCacheIfNeeded(DictionaryTypeCache);

                var dictionaryInterface = type.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType &&
                                        i.GetGenericTypeDefinition() == typeof(IDictionary<,>) &&
                                        i.GetGenericArguments()[0] == typeof(string));

                if (dictionaryInterface != null)
                {
                    var tryGetValueMethod = dictionaryInterface.GetMethod("TryGetValue");
                    var keysProperty = dictionaryInterface.GetProperty("Keys");
                    var indexer = dictionaryInterface.GetProperty("Item");

                    if (tryGetValueMethod != null && indexer != null)
                    {
                        typeInfo = new DictionaryTypeInfo(tryGetValueMethod, keysProperty, indexer);
                    }
                }

                DictionaryTypeCache[type] = typeInfo;
            }
            return typeInfo;
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement jsonElement, string propertyName, out JsonElement jsonProperty)
        {
            foreach (var property in jsonElement.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    jsonProperty = property.Value;
                    return true;
                }
            }

            jsonProperty = default;
            return false;
        }

        private static bool TryGetValueIgnoreCase<TValue>(IDictionary<string, TValue> dict, string key, out TValue? value)
        {
            foreach (var kvp in dict)
            {
                if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    value = kvp.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private static object? GetJsonElementValue(JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.String => jsonElement.GetString(),
                JsonValueKind.Number => GetJsonNumber(jsonElement),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => jsonElement
            };
        }

        private static object GetJsonNumber(JsonElement jsonElement)
        {
            // Try integer types first (more common and precise)
            if (jsonElement.TryGetInt64(out var longValue))
            {
                // Return int if it fits, otherwise long
                if (longValue >= int.MinValue && longValue <= int.MaxValue)
                {
                    return (int)longValue;
                }
                return longValue;
            }

            // Fall back to double for floating-point numbers
            if (jsonElement.TryGetDouble(out var doubleValue))
            {
                return doubleValue;
            }

            // Last resort: decimal for very large/precise numbers
            return jsonElement.GetDecimal();
        }

        /// <summary>
        /// Trims a cache if it exceeds the maximum size by removing approximately half of the entries.
        /// </summary>
        private static void TrimCacheIfNeeded<TKey, TValue>(ConcurrentDictionary<TKey, TValue> cache) where TKey : notnull
        {
            if (cache.Count > MaxCacheSize)
            {
                // Remove approximately half of the entries to avoid frequent trimming
                var keysToRemove = cache.Keys.Take(cache.Count / 2).ToList();
                foreach (var key in keysToRemove)
                {
                    cache.TryRemove(key, out _);
                }
            }
        }

        /// <summary>
        /// Clears all internal caches. Useful for testing or when memory pressure is detected.
        /// </summary>
        public static void ClearCaches()
        {
            PropertyCache.Clear();
            FieldCache.Clear();
            DictionaryTypeCache.Clear();
        }
    }

    /// <summary>
    /// Cached reflection information for dictionary types.
    /// </summary>
    internal sealed class DictionaryTypeInfo
    {
        public MethodInfo TryGetValueMethod { get; }
        public PropertyInfo? KeysProperty { get; }
        public PropertyInfo IndexerProperty { get; }

        public DictionaryTypeInfo(MethodInfo tryGetValueMethod, PropertyInfo? keysProperty, PropertyInfo indexerProperty)
        {
            TryGetValueMethod = tryGetValueMethod;
            KeysProperty = keysProperty;
            IndexerProperty = indexerProperty;
        }
    }
}