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