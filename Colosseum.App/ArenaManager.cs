﻿using Colosseum.App.Client;
using Colosseum.App.Server;
using Colosseum.GS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Colosseum.App
{
    public static class ArenaManager
    {
        static int _startPort => 8000;

        static readonly GenerationGenerator _generationGenerator = new GenerationGenerator();

        public static async Task RunCompetitions(string mapPath, CancellationToken cancellationToken = default(CancellationToken))
        {
            var lastGeneration = new List<Gene>();
            int generationNumber = 1;

            var arenaDir = new DirectoryInfo("arena");
            arenaDir.Create();

            var currentRunDir = arenaDir.CreateSubdirectory(DateTime.Now.ToString().Replace(" ", "-"));
            currentRunDir.Create();

            int port = _startPort;

            CancellationTokenSource cts = new CancellationTokenSource();
            cancellationToken.Register(() => cts.Cancel());

            while (true)
            {
                var generationDir = currentRunDir.CreateSubdirectory(generationNumber.ToString());
                generationDir.Create();

                var newGeneration = _generationGenerator.Genetic(lastGeneration);
                lastGeneration = new List<Gene>();

                var competitionTasks = new List<Task>();

                Console.WriteLine($"running generation {generationNumber}");

                foreach (var gene in newGeneration)
                {
                    competitionTasks.Add(processGene(gene, mapPath, lastGeneration, port, generationDir, cancellationToken));
                    port++;
                }

                await Task.WhenAll(competitionTasks).CancelOnFaulted(cts);

                generationNumber++;
            }
        }

        private static async Task processGene(Gene gene, string mapPath, List<Gene> lastGeneration, int port, DirectoryInfo generationDir, CancellationToken cancellationToken)
        {
            var geneDir = generationDir.CreateSubdirectory(gene.GetHashCode().ToString());
            geneDir.Create();
            await runCompetition(geneDir, gene, port, mapPath, cancellationToken);
            var defenseDir = getGeneDefendClientDirectory(geneDir);
            var defenseOutputPath = ClientManager.GetClientOutputPath(defenseDir);
            gene.Score = double.Parse((await File.ReadAllLinesAsync(defenseOutputPath, cancellationToken)).Last());
            lastGeneration.AddThreadSafe(gene);
            Console.WriteLine($"hash: {gene.GetHashCode()}, score: {gene.Score}");
        }

        private static DirectoryInfo getGeneServerDirectory(DirectoryInfo rootDirectory)
        {
            var dir = rootDirectory.CreateSubdirectory("server");
            dir.Create();
            return dir;
        }

        private static DirectoryInfo getGeneAttackClientDirectory(DirectoryInfo rootDirectory)
        {
            var dir = rootDirectory.CreateSubdirectory("attack-client");
            dir.Create();
            return dir;
        }

        private static DirectoryInfo getGeneDefendClientDirectory(DirectoryInfo rootDirectory)
        {
            var dir = rootDirectory.CreateSubdirectory("defend-client");
            dir.Create();
            return dir;
        }

        private static async Task runCompetition(DirectoryInfo rootDirectory, Gene gene, int port, string mapPath, CancellationToken cancellationToken = default(CancellationToken))
        {
            var serverDir = getGeneServerDirectory(rootDirectory);
            var attackClientDir = getGeneAttackClientDirectory(rootDirectory);
            var defendClientDir = getGeneDefendClientDirectory(rootDirectory);

            await ServerManager.InitializeServerFiles(serverDir, mapPath, port, cancellationToken);

            await ClientManager.InitializeClientFiles(attackClientDir, gene, cancellationToken);
            await ClientManager.InitializeClientFiles(defendClientDir, gene, cancellationToken);

            var serverProcessPayload = await ServerManager.RunServer(rootDirectory, cancellationToken);
            if (!serverProcessPayload.IsRunning())
            {
                throw new Exception("server is not running");
            }

            var attackClientPayload = await ClientManager.RunClient(attackClientDir, port, ClientMode.attack, cancellationToken: cancellationToken);
            if (!attackClientPayload.IsRunning())
            {
                throw new Exception("attack client is not running");
            }

            var defendClientPayload = await ClientManager.RunClient(attackClientDir, port, ClientMode.defend, cancellationToken: cancellationToken);
            if (!defendClientPayload.IsRunning())
            {
                throw new Exception("defend client is not running");
            }

            while (serverProcessPayload.IsRunning())
            {
                await Task.Delay(1000);
            }

            attackClientPayload.Kill();
            defendClientPayload.Kill();
        }
    }
}
