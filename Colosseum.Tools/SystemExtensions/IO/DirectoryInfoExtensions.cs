using System;
using System.IO;
using System.Threading;

namespace Colosseum.Tools.SystemExtensions.IO
{
    public static class DirectoryInfoExtensions
    {
        public static void DeleteForce(this DirectoryInfo directoryInfo)
        {
            foreach (var file in directoryInfo.GetFiles())
            {
                file.Delete();
            }

            foreach (var dir in directoryInfo.GetDirectories())
            {
                dir.DeleteForce();
            }

            var retryCount = 0;
            while (true)
            {
                try
                {
                    directoryInfo.Delete();
                    break;
                }
                catch
                {
                    retryCount++;
                    if (retryCount >= 10)
                    {
                        throw;
                    }
                    Thread.Sleep(1);
                }
            }
        }
    }
}