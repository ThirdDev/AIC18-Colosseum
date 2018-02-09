namespace System
{
    public static class StringExtensions
    {
        public static bool IsNullOrWhiteSpace(this string str) => string.IsNullOrWhiteSpace(str);

        public static bool IsNullOrEmpty(this string str) => string.IsNullOrEmpty(str);

        public static string ToValidFileName(this string str)
        {
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                str = str.Replace(c, '_');
            }

            return str;
        }

        public static string ToValidBashPath(this string str)
        {
            return str.Replace("'", "\\'");
        }

    }
}
