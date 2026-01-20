using System.Dynamic;

namespace ObjectPathLibrary
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Converts a dictionary to an ExpandoObject, enabling dynamic property access.
        /// Nested dictionaries are recursively converted to ExpandoObjects.
        /// </summary>
        /// <param name="dictionary">The source dictionary to convert.</param>
        /// <returns>An ExpandoObject with properties matching the dictionary keys.</returns>
        /// <example>
        /// <code>
        /// var dict = new Dictionary&lt;string, object?&gt; { ["Name"] = "John", ["Age"] = 30 };
        /// dynamic expando = dict.ToExpando();
        /// Console.WriteLine(expando.Name); // "John"
        /// </code>
        /// </example>
        public static dynamic ToExpando(this IDictionary<string, object?> dictionary)
        {
            var expando = new ExpandoObject();
            var expandoDict = (IDictionary<string, object?>)expando!;

            foreach (var kvp in dictionary)
            {
                if (kvp.Value is IDictionary<string, object?> dict)
                {
                    expandoDict.Add(kvp.Key, dict.ToExpando());
                }
                else
                {
                    expandoDict.Add(kvp.Key, kvp.Value);
                }
            }

            return expando;
        }
    }
}