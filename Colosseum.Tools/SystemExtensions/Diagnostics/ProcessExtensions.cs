using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Colosseum.Tools.SystemExtensions.Diagnostics
{
    public static class ProcessExtensions
    {
        public static Process WaitForExitThen(this Process process)
        {
            process.WaitForExit();
            return process;
        }

        public static Task WaitForExitAsync(this Process process, CancellationToken cancellationToken)
        {
            return Task.Run(() => process.WaitForExit(), cancellationToken);
        }

        public static Exception GetProcessErrorException(this Process process, string processDescription)
        {
            return new Exception($"failed to {(processDescription.IsNullOrWhiteSpace().Not() ? processDescription : "run process")}." +
                $"{Environment.NewLine}  output: {process.StandardOutput.ReadToEnd()}" +
                $"{Environment.NewLine}  error output: {process.StandardError.ReadToEnd()}");
        }
    }
}
