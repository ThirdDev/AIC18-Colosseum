using System;
using System.Collections.Generic;
using System.Text;

namespace System.IO
{
    public static class StreamReaderExtensions
    {
        public static string[] ReadAllLines(this StreamReader streamReader)
        {
            List<string> lines = new List<string>();

            while (!streamReader.EndOfStream)
            {
                lines.Add(streamReader.ReadLine());
            }

            return lines.ToArray();
        }
    }
}
