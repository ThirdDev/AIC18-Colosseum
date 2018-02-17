using System;
using System.Collections.Generic;
using System.Linq;

namespace Colosseum.Tools.SystemExtensions.Collection.Generic
{
    public static class IEnumerableExtensions
    {
        private static Random random = new Random();

        public static T RandomElement<T>(this IEnumerable<T> enumerable)
        {
            var index = random.Next(0, enumerable.Count());
            return enumerable.ElementAt(index);
        }

        public static List<T> ToListThreadSafe<T>(this IEnumerable<T> enumerable)
        {
            lock (enumerable)
            {
                return enumerable.ToList();
            }
        }
    }
}