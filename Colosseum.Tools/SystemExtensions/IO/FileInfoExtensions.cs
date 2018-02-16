using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Colosseum.Tools.SystemExtensions.IO
{
    public static class FileInfoExtensions
    {
        public static void ForceCopyTo(this FileInfo file, string destenation, bool overwrite)
        {
            while (true)
            {
                try
                {
                    file.CopyTo(destenation, overwrite);
                    break;
                }
                catch
                {
                    Task.Delay(1).Wait();
                }
            }
        }
    }
}
