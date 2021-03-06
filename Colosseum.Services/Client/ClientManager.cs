﻿using Colosseum.GS;
using Colosseum.Services.Server;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Colosseum.Services.Client
{
    public enum ClientMode
    {
        attack,
        defend
    }

    public static class ClientManager
    {
        private static FileInfo _clientName => new FileInfo("Client.jar");
        private static FileInfo _clientConfigName(ClientMode mode) => new FileInfo($"client.{mode}.cfg");
        private static FileInfo _clientOutput(ClientMode mode) => new FileInfo($"{_clientConfigName(mode)}.out");

        private static string getConfigPath(DirectoryInfo directory, ClientMode mode)
        {
            return Path.Combine(directory.FullName, _clientConfigName(mode).Name);
        }

        private static CommandInfo getCommandInfo(DirectoryInfo directory, int port, ClientMode mode, int clientTimeout = 1000)
        {
            var configPath = getConfigPath(directory, mode);

            var serverConfig = new ServerConfig();

            return new CommandInfo
            {
                FileName = @"C:\ProgramData\Oracle\Java\javapath\java.EXE",
                Args = $"-Xms100m -Xmx1g -jar \"{_clientName.FullName}\" 127.0.0.1 {port} {serverConfig.UIToken} {clientTimeout} {mode} \"{configPath}\"",
            };
        }

        private static string getConfig(Gene gene)
        {
            return gene.ToString();
        }

        public static async Task InitializeClientFiles(DirectoryInfo directory, Gene gene, ClientMode mode, CancellationToken cancellationToken = default)
        {
            Debug.WriteLine($"initializing client file for gene id {gene.Id} in directory {directory.FullName}");

            var clientJarFile = new FileInfo(_clientName.FullName);
            if (!clientJarFile.Exists)
            {
                throw new FileNotFoundException($"client file doesn't exist at {clientJarFile.FullName}");
            }

            var clientConfigFile = new FileInfo(Path.Combine(directory.FullName, _clientConfigName(mode).Name));
            if (clientConfigFile.Exists)
            {
                clientConfigFile.Delete();
            }
            await File.WriteAllTextAsync(clientConfigFile.FullName, getConfig(gene), cancellationToken);

            Debug.WriteLine($"end of initializing client file for gene id {gene.Id} in directory {directory.FullName}");
        }

        public static async Task<ProcessPayload> RunClient(DirectoryInfo directory, int port, ClientMode mode, int clientTimeout = 1000, CancellationToken cancellationToken = default)
        {
            Debug.WriteLine($"running client file in directory {directory.FullName} in mode {mode}");

            var payload = new ProcessPayload();

            var serverCommand = getCommandInfo(directory, port, mode, clientTimeout);
            var logDir = directory.CreateSubdirectory($"client-{mode}-process-info");
            var unused = Task.Run(async () => await OperationSystemService.RunCommandAsync(
                serverCommand,
                payload,
                logDir,
                directory.FullName,
                cancellationToken: cancellationToken), cancellationToken);
            while (!payload.IsRunning())
            {
                await Task.Delay(100, cancellationToken);
            }

            Debug.WriteLine($"returning payload of client file in directory {directory.FullName} in mode {mode}");
            return payload;
        }

        public static string GetClientOutputPath(DirectoryInfo directory, ClientMode mode)
        {
            return Path.Combine(directory.FullName, _clientOutput(mode).Name);
        }
    }
}
