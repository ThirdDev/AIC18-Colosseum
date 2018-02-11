﻿using Colosseum.Services;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Colosseum.Services.Server
{
    public static class ServerManager
    {
        static FileInfo _serverJarFileName => new FileInfo("AIC18-Server.jar");
        static FileInfo _serverConfigsFileName => new FileInfo("server.cfg");
        static FileInfo _gameLogFileName => new FileInfo("game.log");

        private static string getConfigPath(DirectoryInfo directory)
        {
            return Path.Combine(directory.FullName, _serverConfigsFileName.Name);
        }

        private static CommandInfo getCommandInfo(DirectoryInfo directory)
        {
            var configPath = getConfigPath(directory);
            return new CommandInfo
            {
                FileName = @"C:\ProgramData\Oracle\Java\javapath\java.EXE",
                Args = $"-Xms100m -Xmx1g -jar \"{_serverJarFileName.FullName}\" --config=\"{configPath}\"",
                RequiresBash = false,
                HasStandardInput = false
            };
        }

        private static string getServerConfig(string mapPath, int port)
        {
            return new ServerConfig
            {
                Map = mapPath,
                ClientsPort = port.ToString(),
                UIPort = (port - 1000).ToString()
            }.Serialize();
        }

        public static async Task InitializeServerFiles(DirectoryInfo directory, string mapPath, int port, CancellationToken cancellationToken = default)
        {
            var serverJarFile = new FileInfo(_serverJarFileName.FullName);
            if (!serverJarFile.Exists)
            {
                throw new FileNotFoundException($"server file doesn't exist at {serverJarFile.FullName}");
            }

            var serverConfigFile = new FileInfo(Path.Combine(directory.FullName, _serverConfigsFileName.Name));
            if (serverConfigFile.Exists)
            {
                serverConfigFile.Delete();
            }
            await File.WriteAllTextAsync(serverConfigFile.FullName, getServerConfig(mapPath, port), cancellationToken);
        }

        public static string GetGameLogPath(DirectoryInfo directory)
        {
            return Path.Combine(directory.FullName, _gameLogFileName.Name);
        }

        public static async Task<ProcessPayload> RunServer(DirectoryInfo directory, CancellationToken cancellationToken = default)
        {
            ProcessPayload payload = new ProcessPayload();
            var serverCommand = getCommandInfo(directory);
            var logDir = directory.CreateSubdirectory("process-info");
            var task = Task.Run(async () => await OperationSystemService.RunCommandAsync(serverCommand, payload, logDir, directory.FullName, cancellationToken: cancellationToken), cancellationToken);
            while (payload == null)
            {
                await Task.Delay(100);
            }
            while (!payload.IsRunning())
            {
                await Task.Delay(100);
            }
            return payload;
        }
    }
}