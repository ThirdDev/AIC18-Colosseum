﻿using Colosseum.GS;
using Colosseum.Services;
using Colosseum.Services.Client;
using Colosseum.Services.Server;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Colosseum.App
{
    public static class ArenaManager
    {
        static int _startPort => 8000;
        private static int geneProcessLimit => 8;
        static DateTime _arenaStartTime = DateTime.Now;
        static readonly TimeSpan maximumAllowedRunTime = TimeSpan.FromSeconds(120);


        static readonly GenerationGenerator _generationGenerator = new GenerationGenerator();

        public static async Task RunCompetitions(string mapPath, bool useContainer, bool useSwarm, CancellationToken cancellationToken = default)
        {
            if (useContainer)
            {
                await DockerService.BuildImageAsync(Directory.GetCurrentDirectory(), Program.DockerImageName);
                await DockerService.StopAndRemoveAllContainersAsync();
            }

            _arenaStartTime = DateTime.Now;

            Console.WriteLine($"welcome to Colosseum. enjoy the show :)");

            List<Gene> lastGeneration = null;
            int generationNumber = 1;

            var arenaDir = new DirectoryInfo("arena");
            arenaDir.Create();

            var currentRunDir = arenaDir.CreateSubdirectory(DateTime.Now.ToString("s").Replace(" ", "-").ToValidFileName());
            currentRunDir.Create();

            int port = _startPort;

            CancellationTokenSource cts = new CancellationTokenSource();
            cancellationToken.Register(() => cts.Cancel());

            while (true)
            {
                var generationProcessStartTime = DateTime.Now;

                var generationDir = currentRunDir.CreateSubdirectory(generationNumber.ToString());
                generationDir.Create();

                var newGeneration = _generationGenerator.Genetic(lastGeneration);
                lastGeneration = new List<Gene>();

                var competitionTasks = new List<Task>();

                if (useContainer)
                {
                    await DockerService.StopAndRemoveAllContainersAsync();
                }
                else
                {
                    await cleanSystem(cancellationToken);
                }

                Console.WriteLine($"running generation #{generationNumber}");
                Console.WriteLine($"this generation will have {newGeneration.Count} genes and we'll process up to {geneProcessLimit} genes simultaneously");
                Console.WriteLine("-----------");
                Console.WriteLine("no.\tsuccess\tscore\t\t\tid\t\telapsed\t\t\tsystem elapsed\t\t\tPPM\t\t\tcurrent time");

                foreach (var gene in newGeneration)
                {
                    competitionTasks.AddThreadSafe(processGene(gene, mapPath, lastGeneration, port, generationDir, useContainer: useContainer, useSwarm: useSwarm, cancellationToken));

                    port++;
                }

                await Task.WhenAll(competitionTasks).CancelOnFaulted(cts);

                var generationInfoFilePath = Path.Combine(generationDir.FullName, "generationInfo.json");
                await File.WriteAllTextAsync(generationInfoFilePath, JsonConvert.SerializeObject(newGeneration, Formatting.Indented), cancellationToken);

                var generationScoreAverage = newGeneration.Average(x => x.Score);
                Console.WriteLine($"generation score average: {generationScoreAverage}, generation elapsed time: {DateTime.Now - generationProcessStartTime}");

                generationNumber++;
            }
        }

        static SemaphoreSlim _geneProcessSemaphoreSlim = new SemaphoreSlim(geneProcessLimit);
        static SemaphoreSlim _geneProcessLogSemaphoreSlim = new SemaphoreSlim(1);
        static List<TimeSpan> _geneProcessTimes = new List<TimeSpan>();

        private static async Task processGene(Gene gene, string mapPath, List<Gene> lastGeneration, int port, DirectoryInfo generationDir, bool useContainer, bool useSwarm, CancellationToken cancellationToken)
        {
            await _geneProcessSemaphoreSlim.WaitAsync(cancellationToken);
            try
            {
                var processStopwatch = new Stopwatch();
                processStopwatch.Start();

                var geneDir = generationDir.CreateSubdirectory(gene.Id.ToString());
                geneDir.Create();
                var success = await runCompetition(geneDir, gene, port, mapPath, useContainer: useContainer, useSwarm: useSwarm, cancellationToken);
                if (success)
                {
                    var defenseDir = getGeneDefendClientDirectory(geneDir);
                    var defenseOutputPath = ClientManager.GetClientOutputPath(defenseDir, ClientMode.defend);
                    if (File.Exists(defenseOutputPath))
                    {
                        var scoreString = (await File.ReadAllLinesAsync(defenseOutputPath, cancellationToken)).First();
                        if (double.TryParse(scoreString, out double score))
                        {
                            gene.Score = score;
                        }
                        else
                        {
                            gene.Score = null;
                        }
                    }
                    else
                    {
                        success = false;
                        gene.Score = null;
                    }
                }
                else
                {
                    gene.Score = null;
                }
                lastGeneration.AddThreadSafe(gene);

                processStopwatch.Stop();
                await _geneProcessLogSemaphoreSlim.WaitAsync();
                try
                {
                    _geneProcessTimes.AddThreadSafe(processStopwatch.Elapsed);
                    int processCount = _geneProcessTimes.Count;
                    Console.Write($"{processCount.ToString().PadRight(8)}");
                    Console.Write($"{(success ? "x" : "").PadRight(8)}");
                    var score = gene.Score.HasValue ? gene.Score.ToString() : "null";
                    Console.Write($"{score.ToString().PadRight(24)}");
                    Console.Write($"{gene.Id.ToString().PadRight(16)}");
                    TimeSpan processElapsed = processStopwatch.Elapsed;
                    Console.Write($"{processElapsed.ToString().PadRight(24)}");
                    TimeSpan arenaElapse = DateTime.Now - _arenaStartTime;
                    Console.Write($"{arenaElapse.ToString().PadRight(24)}");
                    TimeSpan allProcessAverage = arenaElapse / processCount;
                    double taskPerMinute = TimeSpan.FromSeconds(60) / allProcessAverage;
                    Console.Write($"{taskPerMinute.ToString().PadRight(24)}");
                    Console.Write(DateTime.Now.ToString("s"));
                }
                finally
                {
                    _geneProcessLogSemaphoreSlim.Release();
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"an error occurred while running task for gene hash {gene.Id}{Environment.NewLine}{ex}");
            }
            finally
            {
                _geneProcessSemaphoreSlim.Release();
            }
        }

        private static TimeSpan calculateAverag(List<TimeSpan> list)
        {
            var sum = TimeSpan.FromSeconds(0);
            foreach (var item in list)
            {
                sum += item;
            }
            return sum / list.Count;
        }

        private static DirectoryInfo getGeneServerDirectory(DirectoryInfo rootDirectory)
        {
            return rootDirectory;
        }

        private static DirectoryInfo getGeneAttackClientDirectory(DirectoryInfo rootDirectory)
        {
            return rootDirectory;
        }

        private static DirectoryInfo getGeneDefendClientDirectory(DirectoryInfo rootDirectory)
        {
            return rootDirectory;
        }

        private static DirectoryInfo getTempDir()
        {
            var arenaDir = new DirectoryInfo("arena");
            arenaDir.Create();

            var tmpDir = arenaDir.CreateSubdirectory("tmp");
            tmpDir.Create();
            return tmpDir;
        }

        static HttpClient _httpClient = new HttpClient();


        private static async Task<bool> runCompetition(DirectoryInfo rootDirectory, Gene gene, int port, string mapPath, bool useContainer, bool useSwarm, CancellationToken cancellationToken = default)
        {
            if (useSwarm)
            {
                var tmpDir = getTempDir();
                var geneTempFile = new FileInfo(Path.Combine(tmpDir.FullName, $"{gene.Id}.zip"));

                if (geneTempFile.Exists)
                {
                    await geneTempFile.DeleteAsync();
                }

                var payload = new
                {
                    Gene = gene,
                    mapFileName = Path.GetFileName(mapPath)
                };

                var httpContent = new StringContent(JsonConvert.SerializeObject(payload));

                var response = await _httpClient.PostAsync("127.0.0.1:74275", httpContent);

                using (var fs = geneTempFile.Create())
                {
                    await response.Content.ReadAsStreamAsync().Result.CopyToAsync(fs);
                }

                rootDirectory.Create();
                ZipFile.ExtractToDirectory(geneTempFile.FullName, rootDirectory.FullName);

                return true;
            }
            else
            {
                var serverDir = getGeneServerDirectory(rootDirectory);
                var attackClientDir = getGeneAttackClientDirectory(rootDirectory);
                var defendClientDir = getGeneDefendClientDirectory(rootDirectory);

                await ServerManager.InitializeServerFiles(serverDir, mapPath, (!useContainer) ? port : 7099, cancellationToken);

                await ClientManager.InitializeClientFiles(attackClientDir, gene, ClientMode.attack, cancellationToken);
                await ClientManager.InitializeClientFiles(defendClientDir, gene, ClientMode.defend, cancellationToken);

                if (useContainer)
                {
                    return await runCompetitionInsideContainer(gene.Id, rootDirectory, cancellationToken);
                }
                else
                {
                    return await runCompetitionInsideHost(gene.Id, port, serverDir, attackClientDir, defendClientDir, cancellationToken);
                }
            }
        }

        private static async Task<bool> runCompetitionInsideContainer(int geneId, DirectoryInfo rootDirectory, CancellationToken cancellationToken = default)
        {
            var containerId = await DockerService.RunArenaAsync(Program.DockerImageName, rootDirectory.FullName);
            var startTime = DateTime.Now;
            bool hasFinished = true;

            while (await DockerService.IsContainerRunningAsync(containerId))
            {
                if ((DateTime.Now - startTime) > maximumAllowedRunTime)
                {
                    //Console.WriteLine($"gene {geneId} didn't finish");
                    hasFinished = false;
                    break;
                }
                await Task.Delay(1000);
            }

            var containerInfoPath = Path.Combine(rootDirectory.FullName, "container.info");
            await File.AppendAllLinesAsync(containerInfoPath, await DockerService.GetContainerInfo(containerId, showAll: true, cancellationToken: cancellationToken));

            var containerInspect = Path.Combine(rootDirectory.FullName, "container.inspect.json");
            await File.WriteAllTextAsync(containerInspect, await DockerService.GetContainerInspect(containerId, cancellationToken: cancellationToken));

            var containerLogPath = Path.Combine(rootDirectory.FullName, "container.log");
            await File.WriteAllTextAsync(containerLogPath, await DockerService.ContainerLogAsync(containerId));

            await DockerService.StopAndRemoveContainerAsync(containerId);

            return hasFinished;
        }

        private static async Task<bool> runCompetitionInsideHost(int geneId, int port, DirectoryInfo serverDir, DirectoryInfo attackClientDir, DirectoryInfo defendClientDir, CancellationToken cancellationToken)
        {
            bool hasFinished = true;

            var serverProcessPayload = await ServerManager.RunServer(serverDir, cancellationToken);
            if (!serverProcessPayload.IsRunning())
            {
                throw new Exception("server is not running");
            }

            var attackClientPayload = await ClientManager.RunClient(attackClientDir, port, ClientMode.attack, cancellationToken: cancellationToken);
            if (!attackClientPayload.IsRunning())
            {
                throw new Exception("attack client is not running");
            }

            var defendClientPayload = await ClientManager.RunClient(defendClientDir, port, ClientMode.defend, cancellationToken: cancellationToken);
            if (!defendClientPayload.IsRunning())
            {
                throw new Exception("defend client is not running");
            }

            DateTime startTime = DateTime.Now;
            while (defendClientPayload.IsRunning() && attackClientPayload.IsRunning())
            {
                if ((DateTime.Now - startTime) > maximumAllowedRunTime)
                {
                    //Console.WriteLine($"gene {geneId} didn't finish");
                    hasFinished = false;
                    break;
                }
                await Task.Delay(1000);

            }

            if (attackClientPayload.IsRunning())
            {
                attackClientPayload.Kill();
            }

            if (defendClientPayload.IsRunning())
            {
                defendClientPayload.Kill();
            }

            if (serverProcessPayload.IsRunning())
            {
                serverProcessPayload.Kill();
            }

            return hasFinished;
        }

        static CommandInfo _cleanSystemCommand =>
            new CommandInfo
            {
                FileName = @"C:\WINDOWS\system32\taskkill.EXE",
                Args = "/f /im java.exe",
                RequiresBash = false
            };

        private static async Task cleanSystem(CancellationToken cancellationToken = default)
        {
            var arenaDir = new DirectoryInfo("arena");
            arenaDir.Create();

            var logDir = arenaDir.CreateSubdirectory("tmp");
            logDir.Create();

            await OperationSystemService.RunCommandAsync(_cleanSystemCommand, new ProcessPayload(), logDir, null, log: false, cancellationToken: cancellationToken);
        }
    }
}
