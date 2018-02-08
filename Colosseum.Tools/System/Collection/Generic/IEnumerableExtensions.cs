using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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