using System;
using System.Collections.Generic;
using System.Linq;

namespace JimmysUnityUtilities
{
    public static class CollectionExtensions
    {
        /// <summary>
        /// Great for foreach loops on collections that might be null
        /// </summary>
        public static IEnumerable<T> OrEmptyIfNull<T>(this IEnumerable<T> source)
        {
            return source ?? Enumerable.Empty<T>();
        }

        public static bool ContainsIndex(this Array array, int index, int dimension)
        {
            if (index < 0)
                return false;

            return index < array.GetLength(dimension);
        }

        public static int FirstIndex<T>(this IReadOnlyList<T> list)
        {
            if (list.Count == 0)
                throw new Exception("List has no elements, and therefore no first index");

            return 0;
        }

        public static int LastIndex<T>(this IReadOnlyList<T> list)
        {
            if (list.Count == 0)
                throw new Exception("List has no elements, and therefore no last index");

            return list.Count - 1;
        }
    }
}