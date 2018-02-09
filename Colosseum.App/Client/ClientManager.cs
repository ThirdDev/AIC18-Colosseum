﻿using Colosseum.App.Server;
using Colosseum.GS;
using Colosseum.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        static FileInfo _clientName => new FileInfo("Client.jar");
        static FileInfo _clientConfigName => new FileInfo("clientConfig.cfg");
        static FileInfo _clientOutput => new FileInfo($"{_clientConfigName}.out");

        private static string getConfigPath(DirectoryInfo directory)
        {
            return Path.Combine(directory.FullName, _clientConfigName.Name);
        }

        private static CommandInfo getCommandInfo(DirectoryInfo directory, int port, ClientMode mode, int clientTimeout = 1000)
        {
            var configPath = getConfigPath(directory);

            var serverConfig = new ServerConfig();

            return new CommandInfo
            {
                FileName = @"C:\ProgramData\Oracle\Java\javapath\java.EXE",
                Args = $"-Xms100m -Xmx1g -jar \"{_clientName.FullName}\" 127.0.0.1 {port} {serverConfig.UIToken} {clientTimeout} {mode} \"{configPath}\"",
                RequiresBash = false,
                HasStandardInput = false
            };
        }

        private static string getConfig(Gene gene)
        {
            return gene.ToString();
        }

        public static async Task InitializeClientFiles(DirectoryInfo directory, Gene gene, CancellationToken cancellationToken = default(CancellationToken))
        {
            Debug.WriteLine($"initalizing client file for gene id {gene.GetHashCode()} in directory {directory.FullName}");

            var clientJarFile = new FileInfo(_clientName.FullName);
            if (!clientJarFile.Exists)
            {
                throw new FileNotFoundException($"client file doesn't exist at {clientJarFile.FullName}");
            }

            var clientConfigFile = new FileInfo(Path.Combine(directory.FullName, _clientConfigName.Name));
            if (clientConfigFile.Exists)
            {
                clientConfigFile.Delete();
            }
            await File.WriteAllTextAsync(clientConfigFile.FullName, getConfig(gene), cancellationToken);

            Debug.WriteLine($"end of initalizing client file for gene id {gene.GetHashCode()} in directory {directory.FullName}");
        }

        public static async Task<ProcessPayload> RunClient(DirectoryInfo directory, int port, ClientMode mode, int clientTimeout = 1000, CancellationToken cancellationToken = default(CancellationToken))
        {
            Debug.WriteLine($"running client file in directory {directory.FullName} in mode {mode}");

            ProcessPayload payload = new ProcessPayload();

            void errorReceived(string line)
            {
                payload.Kill();
            }

            var serverCommand = getCommandInfo(directory, port, mode, clientTimeout);
            var logDir = directory.CreateSubdirectory("process-info");
            var task = Task.Run(async () => await OperationSystemService.RunCommandAsync(
                serverCommand,
                payload,
                logDir,
                directory.FullName,
                errorReveived: errorReceived,
                cancellationToken: cancellationToken), cancellationToken);
            while (payload == null)
            {
                await Task.Delay(100);
            }
            while (!payload.IsRunning())
            {
                await Task.Delay(100);
            }

            Debug.WriteLine($"returning payload of client file in directory {directory.FullName} in mode {mode}");
            return payload;
        }

        public static string GetClientOutputPath(DirectoryInfo directory)
        {
            return Path.Combine(directory.FullName, _clientOutput.Name);
        }
    }
}
