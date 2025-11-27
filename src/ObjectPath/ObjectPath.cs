using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace ObjectPathLibrary
{
    public static class ObjectPath
    {
        private static readonly char[] Separator = new[] { '.', '[', ']' };

        private static readonly ConcurrentDictionary<(Type, string, bool), PropertyInfo?> PropertyCache = new();
        private static readonly ConcurrentDictionary<(Type, string, bool), FieldInfo?> FieldCache = new();

        public static object? GetValue(object? obj, string path, bool ignoreCase = true)
        {
            if (obj == null) return null;
            if (string.IsNullOrEmpty(path)) return obj;

            var segments = path.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
            int index = 0;

            while (obj != null && index < segments.Length)
            {
                var currentSegment = segments[index];

                try
                {
                    if (obj is JsonElement jsonElement)
                    {
                        obj = HandleJsonElement(jsonElement, currentSegment, segments, ref index, ignoreCase);
                    }
                    else if (int.TryParse(currentSegment, out var arrayIndex))
                    {
                        obj = HandleArrayIndex(obj, arrayIndex, path);
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
            try
            {
                value = GetValue(obj, path, ignoreCase);
                return obj != null;
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

                // Try Convert for compatible types
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
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
            try
            {
                value = GetValue<T>(obj, path, ignoreCase);
                return obj != null;
            }
            catch (InvalidObjectPathException)
            {
                value = default;
                return false;
            }
        }

        private static object? HandleJsonElement(JsonElement jsonElement, string currentSegment, string[] segments, ref int index, bool ignoreCase)
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
                    throw new InvalidObjectPathException($"Property '{currentSegment}' not found.");
                }
            }
            else if (jsonElement.ValueKind == JsonValueKind.Array && int.TryParse(currentSegment, out var jsonArrayIndex) &&
                     jsonArrayIndex >= 0 && jsonArrayIndex < jsonElement.GetArrayLength())
            {
                return GetJsonElementValue(jsonElement[jsonArrayIndex]);
            }
            else if (index == segments.Length - 1)
            {
                return GetJsonElementValue(jsonElement);
            }
            else
            {
                throw new InvalidObjectPathException($"Invalid array index '{currentSegment}'.");
            }
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
            
            // Handle generic IDictionary<string, T> via reflection
            var objType = obj.GetType();
            var dictionaryInterface = objType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && 
                                    i.GetGenericTypeDefinition() == typeof(IDictionary<,>) &&
                                    i.GetGenericArguments()[0] == typeof(string));
            
            if (dictionaryInterface != null)
            {
                // Use reflection to access the dictionary
                var tryGetValueMethod = dictionaryInterface.GetMethod("TryGetValue");
                var keysProperty = dictionaryInterface.GetProperty("Keys");
                var indexer = dictionaryInterface.GetProperty("Item");
                
                if (tryGetValueMethod != null && indexer != null)
                {
                    // Try exact key match
                    var parameters = new object?[] { currentSegment, null };
                    var found = (bool)tryGetValueMethod.Invoke(obj, parameters)!;
                    if (found)
                    {
                        return parameters[1];
                    }
                    
                    // Try case-insensitive match if enabled
                    if (ignoreCase && keysProperty != null)
                    {
                        var keys = (System.Collections.IEnumerable)keysProperty.GetValue(obj)!;
                        foreach (string key in keys)
                        {
                            if (string.Equals(key, currentSegment, StringComparison.OrdinalIgnoreCase))
                            {
                                return indexer.GetValue(obj, new object[] { key });
                            }
                        }
                    }
                    
                    throw new InvalidObjectPathException($"Property '{currentSegment}' not found in path '{fullPath}'.");
                }
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
                var flags = ignoreCase
                    ? BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase
                    : BindingFlags.Public | BindingFlags.Instance;
                fieldInfo = type.GetField(fieldName, flags);
                FieldCache[key] = fieldInfo;
            }
            return fieldInfo;
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
                JsonValueKind.Number => jsonElement.GetDecimal(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => jsonElement
            };
        }
    }
}