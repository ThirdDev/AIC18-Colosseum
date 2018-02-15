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
    public enum CompetetionMode
    {
        NoContainer,
        ContainerPerCompetetion,
        ReusableContainer
    }

    public static class ArenaManager
    {
        static int _startPort => 8000;
        private static int geneProcessLimit => 9;
        static DateTime _arenaStartTime = DateTime.Now;
        static readonly TimeSpan maximumAllowedRunTime = TimeSpan.FromSeconds(45);
        static readonly int maximumTryCount = 3;


        static readonly GenerationGenerator _generationGenerator = new GenerationGenerator();

        public static async Task RunCompetitions(string mapPath, CompetetionMode competetionMode, CancellationToken cancellationToken = default)
        {

            switch (competetionMode)
            {
                case CompetetionMode.NoContainer:
                    break;
                case CompetetionMode.ContainerPerCompetetion:
                    await DockerService.BuildImageAsync(Directory.GetCurrentDirectory(), Program.DockerImageName);
                    await DockerService.StopAndRemoveAllContainersAsync();
                    break;
                case CompetetionMode.ReusableContainer:
                    await ContainerRepository.InitalizeContainers(geneProcessLimit, Program.DockerImageName, cancellationToken);
                    break;
                default:
                    break;
            }

            _arenaStartTime = DateTime.Now;

            Console.WriteLine($"welcome to Colosseum. enjoy the show :)");

            List<Gene> lastGeneration = null;
            var lastGenerationFile = new FileInfo("generationInfo.json");
            if (lastGenerationFile.Exists)
            {
                Console.WriteLine($"loading last generation from {lastGenerationFile.FullName}");
                try
                {
                    var content = await File.ReadAllTextAsync(lastGenerationFile.FullName, cancellationToken);
                    lastGeneration = JsonConvert.DeserializeObject<List<Gene>>(content);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"loading last generation failed. error:{Environment.NewLine}{ex}");
                }
            }

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

                switch (competetionMode)
                {
                    case CompetetionMode.NoContainer:
                        await cleanSystem(cancellationToken);
                        break;
                    case CompetetionMode.ContainerPerCompetetion:
                        await DockerService.StopAndRemoveAllContainersAsync();
                        break;
                    case CompetetionMode.ReusableContainer:
                        break;
                    default:
                        break;
                }

                Console.WriteLine($"running generation #{generationNumber}");
                Console.WriteLine($"this generation will have {newGeneration.Count} genes and we'll process up to {geneProcessLimit} genes simultaneously");
                Console.WriteLine("-----------");
                Console.WriteLine("no.\tsuccess\tscore\t\tid\t\telapsed\t\tsystem elapsed\t\tPPM\t\tcurrent time");

                foreach (var gene in newGeneration)
                {
                    competitionTasks.AddThreadSafe(processGene(gene, mapPath, lastGeneration, port, generationDir, competetionMode, cancellationToken));

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

        private static async Task processGene(Gene gene, string mapPath, List<Gene> lastGeneration, int port, DirectoryInfo generationDir, CompetetionMode competetionMode, CancellationToken cancellationToken)
        {
            await _geneProcessSemaphoreSlim.WaitAsync(cancellationToken);
            try
            {
                var processStopwatch = new Stopwatch();
                processStopwatch.Start();

                var geneDir = generationDir.CreateSubdirectory(gene.Id.ToString());
                geneDir.Create();
                var competitionResult = await runCompetition(geneDir, gene, port, mapPath, competetionMode, cancellationToken);
                if (competitionResult.Status == CompetitionResultStatus.Successful)
                {
                    var defenseDir = getGeneDefendClientDirectory(geneDir);
                    var defenseOutputPath = ClientManager.GetClientOutputPath(defenseDir, ClientMode.defend);
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
                    int processCount = _geneProcessTimes.Count;
                    Console.Write($"{processCount.ToString().PadRight(8)}");

                    string successState = "";
                    if (competitionResult.Status == CompetitionResultStatus.Successful)
                        successState = "x" + (competitionResult.TryCount == 1 ? "" : competitionResult.TryCount.ToString());
                    Console.Write($"{successState.PadRight(8)}");

                    var score = gene.Score.HasValue ? gene.Score?.ToString("F2") : "null";
                    Console.Write($"{score.ToString().PadRight(16)}");
                    Console.Write($"{gene.Id.ToString().PadRight(16)}");
                    TimeSpan processElapsed = processStopwatch.Elapsed;
                    Console.Write($"{processElapsed.ToString(@"mm\:ss\.fff").PadRight(16)}");
                    TimeSpan arenaElapse = DateTime.Now - _arenaStartTime;
                    Console.Write($"{arenaElapse.ToString(@"hh\:mm\:ss\.fff").PadRight(24)}");
                    TimeSpan allProcessAverage = arenaElapse / processCount;
                    double taskPerMinute = TimeSpan.FromSeconds(60) / allProcessAverage;
                    Console.Write($"{taskPerMinute.ToString("F2").PadRight(12)}");
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

        private static async Task<CompetitionResult> runCompetition(DirectoryInfo rootDirectory, Gene gene, int port, string mapPath, CompetetionMode competetionMode, CancellationToken cancellationToken = default)
        {
            int tryCount = 1;

            while (tryCount <= maximumTryCount)
            {
                var result = false;

                switch (competetionMode)
                {
                    case CompetetionMode.NoContainer:
                        result = await runCompetitionInsideHost(gene, port, mapPath, rootDirectory, cancellationToken);
                        break;
                    case CompetetionMode.ContainerPerCompetetion:
                        result = await runCompetitionInsideContainer(gene, mapPath, rootDirectory, cancellationToken);
                        if (!result)
                        {
                            backupAttempt(rootDirectory, tryCount);
                        }
                        break;
                    case CompetetionMode.ReusableContainer:
                        result = await runCompetetionInsideReusableContainer(gene, mapPath, rootDirectory, cancellationToken);
                        break;
                    default:
                        throw new NotImplementedException();
                        //break;
                }

                if (result)
                {
                    return new CompetitionResult
                    {
                        Status = CompetitionResultStatus.Successful,
                        TryCount = tryCount,
                    };
                }

                tryCount++;
            }

            return new CompetitionResult
            {
                Status = CompetitionResultStatus.Failed,
                TryCount = tryCount - 1,
            };
        }

        private static async Task initializeCompeteionDirectory(Gene gene, int port, string mapPath, DirectoryInfo rootDirectory, CancellationToken cancellationToken)
        {
            await ServerManager.InitializeServerFiles(rootDirectory, mapPath, port, cancellationToken);

            await ClientManager.InitializeClientFiles(rootDirectory, gene, ClientMode.attack, cancellationToken);
            await ClientManager.InitializeClientFiles(rootDirectory, gene, ClientMode.defend, cancellationToken);
        }

        private static void backupAttempt(DirectoryInfo attackClientDir, int tryCount)
        {
            var backupPath = attackClientDir.CreateSubdirectory($"failed-attempt-{tryCount}").FullName;

            foreach (var file in attackClientDir.GetFiles())
                file.CopyTo(Path.Combine(backupPath, file.Name));
        }

        private static async Task<bool> runCompetetionInsideReusableContainer(Gene gene, string mapPath, DirectoryInfo rootDirectory, CancellationToken cancellationToken = default)
        {
            var hasFinished = true;

            await initializeCompeteionDirectory(gene, 7099, mapPath, rootDirectory, cancellationToken);

            var containerInfo = ContainerRepository.GetAFreeContainer();
            await containerInfo.Semaphore.WaitAsync(cancellationToken);
            try
            {
                foreach (var file in rootDirectory.GetFiles())
                {
                    file.CopyTo(Path.Combine(containerInfo.FilesDirectory.FullName, file.Name));
                }

                await DockerService.StartContainer(containerInfo.Id, cancellationToken);
                var startTime = DateTime.Now; ;
                while (await DockerService.IsContainerRunningAsync(containerInfo.Id, cancellationToken))
                {
                    if ((DateTime.Now - startTime) > maximumAllowedRunTime)
                    {
                        //Console.WriteLine($"gene {geneId} didn't finish");
                        hasFinished = false;
                        break;
                    }
                    await Task.Delay(1000);
                }

                if (hasFinished)
                {
                    var defenderClientOutputPath = ClientManager.GetClientOutputPath(rootDirectory, ClientMode.defend);
                    var attackerClientOutputPath = ClientManager.GetClientOutputPath(rootDirectory, ClientMode.attack);

                    hasFinished = File.Exists(defenderClientOutputPath) && File.Exists(attackerClientOutputPath);
                }

                var containerInfoPath = Path.Combine(rootDirectory.FullName, "container.info");
                await File.AppendAllLinesAsync(containerInfoPath, await DockerService.GetContainerInfo(containerInfo.Id, showAll: true, cancellationToken: cancellationToken), cancellationToken);

                var containerInspect = Path.Combine(rootDirectory.FullName, "container.inspect.json");
                await File.WriteAllTextAsync(containerInspect, await DockerService.GetContainerInspect(containerInfo.Id, cancellationToken: cancellationToken), cancellationToken);

                var containerLogPath = Path.Combine(rootDirectory.FullName, "container.log");
                await File.WriteAllTextAsync(containerLogPath, await DockerService.ContainerLogAsync(containerInfo.Id, cancellationToken), cancellationToken);

                return hasFinished;
            }
            finally
            {
                containerInfo.Semaphore.Release();
            }

        }

        private static async Task<bool> runCompetitionInsideContainer(Gene gene, string mapPath, DirectoryInfo rootDirectory, CancellationToken cancellationToken = default)
        {
            await initializeCompeteionDirectory(gene, 7099, mapPath, rootDirectory, cancellationToken);

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

            if (hasFinished)
            {
                var defenderClientOutputPath = ClientManager.GetClientOutputPath(rootDirectory, ClientMode.defend);
                var attackerClientOutputPath = ClientManager.GetClientOutputPath(rootDirectory, ClientMode.attack);

                hasFinished = File.Exists(defenderClientOutputPath) && File.Exists(attackerClientOutputPath);
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

        private static async Task<bool> runCompetitionInsideHost(Gene gene, int port, string mapPath, DirectoryInfo rootDirectory, CancellationToken cancellationToken)
        {
            await initializeCompeteionDirectory(gene, port, mapPath, rootDirectory, cancellationToken);

            bool hasFinished = true;

            var serverProcessPayload = await ServerManager.RunServer(rootDirectory, cancellationToken);
            if (!serverProcessPayload.IsRunning())
            {
                throw new Exception("server is not running");
            }

            var attackClientPayload = await ClientManager.RunClient(rootDirectory, port, ClientMode.attack, cancellationToken: cancellationToken);
            if (!attackClientPayload.IsRunning())
            {
                throw new Exception("attack client is not running");
            }

            var defendClientPayload = await ClientManager.RunClient(rootDirectory, port, ClientMode.defend, cancellationToken: cancellationToken);
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


            if (hasFinished)
            {
                var defenderClientOutputPath = ClientManager.GetClientOutputPath(rootDirectory, ClientMode.defend);
                var attackerClientOutputPath = ClientManager.GetClientOutputPath(rootDirectory, ClientMode.attack);

                hasFinished = File.Exists(defenderClientOutputPath) && File.Exists(attackerClientOutputPath);
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
