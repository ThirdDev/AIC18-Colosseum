namespace System.Collections.Generic
{
    public static class DictionaryExtensions
    {
        public static TValue AddAndGet<Tkey, TValue>(this Dictionary<Tkey, TValue> dictionary, Tkey key, TValue value)
        {
            lock (dictionary)
            {
                if (!dictionary.ContainsKey(key))
                {
                    dictionary.Add(key, value);
                }
                return dictionary[key];
            }
        }

        public static void AddThreadSafe<Tkey, TValue>(this Dictionary<Tkey, TValue> dictionary, Tkey key, TValue value)
        {
            lock (dictionary)
            {
                dictionary.Add(key, value);
            }
        }

        public static bool RemoveThreadSafe<Tkey, TValue>(this Dictionary<Tkey, TValue> dictionary, Tkey key)
        {
            lock (dictionary)
            {
                return dictionary.Remove(key);
            }
        }
    }
}