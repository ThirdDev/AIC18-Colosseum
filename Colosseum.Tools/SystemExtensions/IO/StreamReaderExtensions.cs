using System.Collections.Generic;
using System.IO;

namespace Colosseum.Tools.SystemExtensions.IO
{
    public static class StreamReaderExtensions
    {
        public static string[] ReadAllLines(this StreamReader streamReader)
        {
            var lines = new List<string>();

            while (!streamReader.EndOfStream)
            {
                lines.Add(streamReader.ReadLine());
            }

            return lines.ToArray();
        }
    }
}
