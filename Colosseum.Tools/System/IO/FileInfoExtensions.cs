using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace System.IO
{
    public static class FileInfoExtensions
    {
        public static Task DeleteAsync(this FileInfo fi)
        {
            return Task.Factory.StartNew(() => fi.Delete());
        }
    }
}
