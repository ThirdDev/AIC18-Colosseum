using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

            //var retryCount = 0;
            while (true)
            {
                try
                {
                    directoryInfo.Delete();
                    break;
                }
                catch
                {
                    //retryCount++;
                    //if (retryCount >= 10)
                    //{
                    //    throw;
                    //}
                    Task.Delay(1).Wait();
                }
            }
        }

        public static void CopyContentsTo(this DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory)
        {
            foreach (var file in sourceDirectory.GetFiles())
            {
                file.ForceCopyTo(Path.Combine(destinationDirectory.FullName, file.Name), true);
            }

            foreach (var dir in sourceDirectory.GetDirectories())
            {
                while (true)
                {
                    try
                    {
                        var desDirectoryInfo = destinationDirectory.GetDirectories().FirstOrDefault(x => x.Name == dir.Name) ??
                                               destinationDirectory.CreateSubdirectory(dir.Name);
                        dir.CopyContentsTo(desDirectoryInfo);
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
}