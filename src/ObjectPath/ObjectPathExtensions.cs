namespace ObjectPathLibrary
{
    public static class ObjectPathExtensions
    {
        /// <summary>
        /// Gets a value from an object using a path expression.
        /// </summary>
        /// <param name="obj">The source object.</param>
        /// <param name="path">The path expression (e.g., "Address.City" or "Items[0].Name").</param>
        /// <param name="ignoreCase">If true, property names are matched case-insensitively. Default is true.</param>
        /// <returns>The value at the specified path.</returns>
        /// <exception cref="InvalidObjectPathException">Thrown when the path is invalid.</exception>
        public static object? GetValueByPath(this object obj, string path, bool ignoreCase = true)
        {
            return ObjectPath.GetValue(obj, path, ignoreCase);
        }

        /// <summary>
        /// Gets a value from an object using a path expression and casts it to the specified type.
        /// </summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="obj">The source object.</param>
        /// <param name="path">The path expression.</param>
        /// <param name="ignoreCase">If true, property names are matched case-insensitively. Default is true.</param>
        /// <returns>The value cast to type T.</returns>
        /// <exception cref="InvalidObjectPathException">Thrown when the path is invalid or type conversion fails.</exception>
        public static T? GetValueByPath<T>(this object obj, string path, bool ignoreCase = true)
        {
            return ObjectPath.GetValue<T>(obj, path, ignoreCase);
        }

        /// <summary>
        /// Gets a value from an object using a path expression, returning null if the path is invalid.
        /// </summary>
        /// <param name="obj">The source object.</param>
        /// <param name="path">The path expression.</param>
        /// <param name="ignoreCase">If true, property names are matched case-insensitively. Default is true.</param>
        /// <returns>The value at the specified path, or null if the path is invalid.</returns>
        public static object? GetValueByPathOrNull(this object obj, string path, bool ignoreCase = true)
        {
            return ObjectPath.TryGetValue(obj, path, out var value, ignoreCase) ? value : null;
        }

        /// <summary>
        /// Attempts to get a value from an object using a path expression.
        /// </summary>
        /// <param name="obj">The source object.</param>
        /// <param name="path">The path expression.</param>
        /// <param name="value">When this method returns, contains the value if found; otherwise, null.</param>
        /// <param name="ignoreCase">If true, property names are matched case-insensitively. Default is true.</param>
        /// <returns>true if the value was found; otherwise, false.</returns>
        public static bool TryGetValueByPath(this object obj, string path, out object? value, bool ignoreCase = true)
        {
            return ObjectPath.TryGetValue(obj, path, out value, ignoreCase);
        }

        /// <summary>
        /// Attempts to get a value from an object using a path expression and cast it to the specified type.
        /// </summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="obj">The source object.</param>
        /// <param name="path">The path expression.</param>
        /// <param name="value">When this method returns, contains the typed value if found; otherwise, default(T).</param>
        /// <param name="ignoreCase">If true, property names are matched case-insensitively. Default is true.</param>
        /// <returns>true if the value was found and successfully converted; otherwise, false.</returns>
        public static bool TryGetValueByPath<T>(this object obj, string path, out T? value, bool ignoreCase = true)
        {
            return ObjectPath.TryGetValue<T>(obj, path, out value, ignoreCase);
        }
    }
}
