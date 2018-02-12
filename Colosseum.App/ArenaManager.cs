using Colosseum.GS;
using Colosseum.Services;
using Colosseum.Services.Client;
using Colosseum.Services.Server;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static int geneProcessLimit => 10;
        static DateTime _arenaStartTime = DateTime.Now;
        static readonly TimeSpan maximumAllowedRunTime = TimeSpan.FromSeconds(40);


        static readonly GenerationGenerator _generationGenerator = new GenerationGenerator();

        public static async Task RunCompetitions(string mapPath, bool useContainer, CancellationToken cancellationToken = default)
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
                Console.WriteLine($"this generation will have {newGeneration.Count} genes and we'll process up to {geneProcessLimit} genes at each moment");
                Console.WriteLine("-----------");
                Console.WriteLine("score\telapsed\t\t\tmin avg max\t\t\t\t\t\tcount\ttotal elapsed\t\tprocesses per minute");

                foreach (var gene in newGeneration)
                {
                    competitionTasks.AddThreadSafe(processGene(gene, mapPath, lastGeneration, port, generationDir, useContainer, cancellationToken));

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

        private static async Task processGene(Gene gene, string mapPath, List<Gene> lastGeneration, int port, DirectoryInfo generationDir, bool useContainer, CancellationToken cancellationToken)
        {
            await _geneProcessSemaphoreSlim.WaitAsync(cancellationToken);
            try
            {
                var processStopwatch = new Stopwatch();
                processStopwatch.Start();

                var geneDir = generationDir.CreateSubdirectory(gene.Id.ToString());
                geneDir.Create();
                await runCompetition(geneDir, gene, port, mapPath, useContainer, cancellationToken);
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
                    gene.Score = null;
                }
                lastGeneration.AddThreadSafe(gene);

                processStopwatch.Stop();
                await _geneProcessLogSemaphoreSlim.WaitAsync();
                try
                {
                    _geneProcessTimes.AddThreadSafe(processStopwatch.Elapsed);
                    Console.WriteLine($"\t\t\t\t\t\t\t\t========{gene.Id}========");
                    var score = gene.Score;
                    Console.Write($"{score}\t");
                    TimeSpan processElapsed = processStopwatch.Elapsed;
                    Console.Write($"{processElapsed}\t");
                    TimeSpan min = _geneProcessTimes.Min();
                    Console.Write($"{min} ");
                    TimeSpan avg = calculateAverag(_geneProcessTimes);
                    Console.Write($"{avg} ");
                    TimeSpan max = _geneProcessTimes.Max();
                    Console.Write($"{max}\t");
                    int processCount = _geneProcessTimes.Count;
                    Console.Write($"{processCount}\t");
                    TimeSpan arenaElapse = DateTime.Now - _arenaStartTime;
                    Console.Write($"{arenaElapse}\t");
                    TimeSpan allProcessAverage = arenaElapse / processCount;
					double taskPerMinute = allProcessAverage / TimeSpan.FromSeconds(60);
                    Console.Write($"{taskPerMinute}");
                }
                finally
                {
                    _geneProcessLogSemaphoreSlim.Release();
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"an error occured while running task for gene hash {gene.Id}{Environment.NewLine}{ex}");
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

        private static async Task runCompetition(DirectoryInfo rootDirectory, Gene gene, int port, string mapPath, bool useContainer, CancellationToken cancellationToken = default)
        {
            var serverDir = getGeneServerDirectory(rootDirectory);
            var attackClientDir = getGeneAttackClientDirectory(rootDirectory);
            var defendClientDir = getGeneDefendClientDirectory(rootDirectory);

            await ServerManager.InitializeServerFiles(serverDir, mapPath, (!useContainer) ? port : 7099, cancellationToken);

            await ClientManager.InitializeClientFiles(attackClientDir, gene, ClientMode.attack, cancellationToken);
            await ClientManager.InitializeClientFiles(defendClientDir, gene, ClientMode.defend, cancellationToken);

            if (useContainer)
            {
                await runCompetitionInsideContainer(gene.Id, rootDirectory, cancellationToken);
            }
            else
            {
                await runCompetitionInsideHost(port, serverDir, attackClientDir, defendClientDir, cancellationToken);
            }
        }

        private static async Task runCompetitionInsideContainer(int geneId, DirectoryInfo rootDirectory, CancellationToken cancellationToken = default)
        {
            var containerId = await DockerService.RunArenaAsync(Program.DockerImageName, rootDirectory.FullName);

            async Task checkContainer()
            {
                while (await DockerService.IsContainerRunningAsync(containerId))
                {
                    await Task.Delay(1000);
                }
            }

            var timeoutCheck = Task.Delay(maximumAllowedRunTime.Milliseconds);
            var finalTask = Task.WhenAny(checkContainer(), timeoutCheck);

            if (finalTask == timeoutCheck)
            {
                Console.WriteLine($"gene {geneId} didn't finish");
            }

            var containerLogPath = Path.Combine(rootDirectory.FullName, "container.log");
            File.WriteAllText(containerLogPath, await DockerService.ContainerLogAsync(containerId));

            await DockerService.StopAndRemoveContainerAsync(containerId);
        }

        private static async Task runCompetitionInsideHost(int port, DirectoryInfo serverDir, DirectoryInfo attackClientDir, DirectoryInfo defendClientDir, CancellationToken cancellationToken)
        {
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
                await Task.Delay(1000);
                if ((DateTime.Now - startTime) > maximumAllowedRunTime)
                    break;
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
