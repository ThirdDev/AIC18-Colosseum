using Colosseum.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Colosseum.App.Server
{
    public static class ServerManager
    {
        static string _serverJarFileName => "AIC18-Server.jar";
        static string _serverConfigsFileName => "server.cfg";
        static string _gameLogFileName => "game.log";

        private static string getJarPath(DirectoryInfo directory)
        {
            return Path.Combine(directory.FullName, _serverJarFileName);
        }

        private static string getConfigPath(DirectoryInfo directory)
        {
            return Path.Combine(directory.FullName, _serverConfigsFileName);
        }

        private static CommandInfo getCommandInfo(DirectoryInfo directory)
        {
            var jarPath = getJarPath(directory);
            var configPath = getConfigPath(directory);
            return new CommandInfo
            {
                FileName = "java",
                Args = $"-jar {jarPath} --config={configPath}",
                RequiresBash = true,
                HasStandardInput = false
            };
        }

        private static string getServerConfig(string mapPath, int port)
        {
            return new ServerConfig
            {
                Map = mapPath,
                ClientsPort = port.ToString()
            }.Serialize();
        }

        public static async Task InitializeServerFiles(DirectoryInfo directory, string mapPath, int port, CancellationToken cancellationToken)
        {
            var serverJarFile = new FileInfo(_serverJarFileName);
            if (!serverJarFile.Exists)
            {
                throw new FileNotFoundException($"server file doesn't exist at {serverJarFile.FullName}");
            }
            serverJarFile.CopyTo(getJarPath(directory));

            var serverConfigFile = new FileInfo(Path.Combine(directory.FullName, _serverConfigsFileName));
            if (!serverConfigFile.Exists)
            {
                serverConfigFile.Create();
            }
            await File.WriteAllTextAsync(serverConfigFile.FullName, getServerConfig(mapPath, port), cancellationToken);
        }

        public static string GetGameLogPath(DirectoryInfo directory)
        {
            return Path.Combine(directory.FullName, _gameLogFileName);
        }

        public static ProcessPayload RunServer(DirectoryInfo directory, CancellationToken cancellationToken)
        {
            ProcessPayload payload = null;
            var serverCommand = getCommandInfo(directory);
            var task = Task.Run(async () => await OperationSystemService.RunCommandAsync(serverCommand, cancellationToken, payload));
            return payload;
        }
    }
}
