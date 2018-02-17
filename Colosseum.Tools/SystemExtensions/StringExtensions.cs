using System.Linq;

namespace Colosseum.Tools.SystemExtensions
{
    public static class StringExtensions
    {
        public static bool IsNullOrWhiteSpace(this string str) => string.IsNullOrWhiteSpace(str);

        public static bool IsNullOrEmpty(this string str) => string.IsNullOrEmpty(str);

        public static string ToValidFileName(this string str)
        {
            return System.IO.Path.GetInvalidFileNameChars().Aggregate(str, (current, c) => current.Replace(c, '_'));
        }

        public static string ToValidBashPath(this string str)
        {
            return str.Replace("'", "\\'");
        }

    }
}
