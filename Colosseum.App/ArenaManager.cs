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
        private static int geneProcessLimit => 4;
        static DateTime _arenaStartTime = DateTime.Now;
        static readonly TimeSpan maximumAllowedRunTime = TimeSpan.FromSeconds(40);


        static readonly GenerationGenerator _generationGenerator = new GenerationGenerator();

        public static async Task RunCompetitions(string mapPath, CancellationToken cancellationToken = default)
        {
            _arenaStartTime = DateTime.Now;

            Console.WriteLine($"welcome to Colosseum. we have {GenerationGenerator.generationPopulation} cells per generation and we process {geneProcessLimit} competitions each moment. enjoy the show :)");

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

                await cleanSystem(cancellationToken);

                Console.WriteLine($"running generation #{generationNumber}");
                Console.WriteLine("-----------");
                Console.WriteLine("score\telapsed\t\t\tmin avg max\t\t\t\t\t\tcount\ttotal elapsed\t\ttime per process");

                foreach (var gene in newGeneration)
                {
                    competitionTasks.AddThreadSafe(processGene(gene, mapPath, lastGeneration, port, generationDir, cancellationToken));

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

        private static async Task processGene(Gene gene, string mapPath, List<Gene> lastGeneration, int port, DirectoryInfo generationDir, CancellationToken cancellationToken)
        {
            await _geneProcessSemaphoreSlim.WaitAsync(cancellationToken);
            try
            {
                var processStopwatch = new Stopwatch();
                processStopwatch.Start();

                var geneDir = generationDir.CreateSubdirectory(gene.Id.ToString());
                geneDir.Create();
                await runCompetition(geneDir, gene, port, mapPath, cancellationToken);
                var defenseDir = getGeneDefendClientDirectory(geneDir);
                var defenseOutputPath = ClientManager.GetClientOutputPath(defenseDir);
                if (File.Exists(defenseOutputPath))
                {
                    gene.Score = double.Parse((await File.ReadAllLinesAsync(defenseOutputPath, cancellationToken)).First());
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
                    Console.WriteLine($"id: {gene.Id}");
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
                    Console.Write($"{allProcessAverage}");
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

        private static async Task runCompetition(DirectoryInfo rootDirectory, Gene gene, int port, string mapPath, CancellationToken cancellationToken = default)
        {
            var serverDir = getGeneServerDirectory(rootDirectory);
            var attackClientDir = getGeneAttackClientDirectory(rootDirectory);
            var defendClientDir = getGeneDefendClientDirectory(rootDirectory);

            await ServerManager.InitializeServerFiles(serverDir, mapPath, port, cancellationToken);

            await ClientManager.InitializeClientFiles(attackClientDir, gene, cancellationToken);
            await ClientManager.InitializeClientFiles(defendClientDir, gene, cancellationToken);

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

            await OperationSystemService.RunCommandAsync(_cleanSystemCommand, new ProcessPayload(), logDir, null, cancellationToken: cancellationToken);
        }
    }
}
