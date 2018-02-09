using System.Linq;

namespace System.Collections.Generic
{
    public static class IEnumerableExtensions
    {
        private static Random random = new Random();

        public static T RandomElement<T>(this IEnumerable<T> enumerable)
        {
            int index = random.Next(0, enumerable.Count());
            return enumerable.ElementAt(index);
        }
    }
}