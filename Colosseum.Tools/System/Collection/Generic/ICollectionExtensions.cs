using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    public static class ICollectionExtensions
    {
        public static void AddThreadSafe<TSource>(this ICollection<TSource> collection, TSource value)
        {
            lock (collection)
            {
                collection.Add(value);
            }
        }

        public static bool RemoveThreadSafe<TSource>(this ICollection<TSource> collection, TSource value)
        {
            lock (collection)
            {
                return collection.Remove(value);
            }
        }
    }
}