using Colosseum.App.Server;
using Colosseum.GS;
using Colosseum.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Colosseum.App.Client
{
    public enum ClientMode
    {
        attack,
        defend
    }

    public static class ClientManager
    {
        static string _clientName => "Client.jar";
        static string _clientConfigName => "clientConfig.cfg";
        static string _clientOutput => $"{_clientConfigName}.out";

        private static string getJarPath(DirectoryInfo directory)
        {
            return Path.Combine(directory.FullName, _clientName);
        }

        private static string getConfigPath(DirectoryInfo directory)
        {
            return Path.Combine(directory.FullName, _clientConfigName);
        }

        private static CommandInfo getCommandInfo(DirectoryInfo directory, int port, ClientMode mode, int clientTimeout = 1000)
        {
            var jarPath = getJarPath(directory);
            var configPath = getConfigPath(directory);

            var serverConfig = new ServerConfig();

            return new CommandInfo
            {
                FileName = "java",
                Args = $"-jar \"{jarPath}\" localhost {port} {serverConfig.UIToken} {clientTimeout} {mode} \"{configPath}\"",
                RequiresBash = true,
                HasStandardInput = false
            };
        }

        private static string getConfig(Gene gene)
        {
            return gene.ToString();
        }

        public static async Task InitializeClientFiles(DirectoryInfo directory, Gene gene, CancellationToken cancellationToken = default(CancellationToken))
        {
            var clientJarFile = new FileInfo(_clientName);
            if (!clientJarFile.Exists)
            {
                throw new FileNotFoundException($"client file doesn't exist at {clientJarFile.FullName}");
            }
            clientJarFile.CopyTo(getJarPath(directory));

            var clientConfigFile = new FileInfo(Path.Combine(directory.FullName, _clientConfigName));
            if (!clientConfigFile.Exists)
            {
                clientConfigFile.Create();
            }
            await File.WriteAllTextAsync(clientConfigFile.FullName, getConfig(gene), cancellationToken);
        }

        public static async Task<ProcessPayload> RunClient(DirectoryInfo directory, int port, ClientMode mode, int clientTimeout = 1000, CancellationToken cancellationToken = default(CancellationToken))
        {
            ProcessPayload payload = null;
            var serverCommand = getCommandInfo(directory, port, mode, clientTimeout);
            var task = Task.Run(async () => await OperationSystemService.RunCommandAsync(serverCommand, cancellationToken, payload), cancellationToken);
            while (!payload.IsRunning())
            {
                await Task.Delay(100);
            }
            return payload;
        }
    }
}
