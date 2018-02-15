using System.IO;

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

            directoryInfo.Delete();
        }
    }
}