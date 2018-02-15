using Colosseum.GS;
using Colosseum.Services;
using Colosseum.Services.Client;
using Colosseum.Services.Server;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Colosseum.Tools.SystemExtensions;
using Colosseum.Tools.SystemExtensions.Collection.Generic;
using Colosseum.Tools.SystemExtensions.Threading.Tasks;

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
        private const int _startPort = 8000;
        private const int _geneProcessLimit = 9;
        private static DateTime _arenaStartTime = DateTime.Now;
        private static readonly TimeSpan _maximumAllowedRunTime = TimeSpan.FromSeconds(45);
        private const int _maximumTryCount = 3;


        private static readonly GenerationGenerator _generationGenerator = new GenerationGenerator();

        [SuppressMessage("ReSharper", "FunctionNeverReturns")]
        public static async Task RunCompetitions(string mapPath, CompetetionMode competetionMode, CancellationToken cancellationToken = default)
        {

            switch (competetionMode)
            {
                case CompetetionMode.NoContainer:
                    break;
                case CompetetionMode.ContainerPerCompetetion:
                    await DockerService.BuildImageAsync(Directory.GetCurrentDirectory(), Program.DockerImageName, cancellationToken);
                    await DockerService.StopAndRemoveAllContainersAsync(cancellationToken);
                    break;
                case CompetetionMode.ReusableContainer:
                    await DockerService.BuildImageAsync(Directory.GetCurrentDirectory(), Program.DockerImageName, cancellationToken);
                    await DockerService.StopAndRemoveAllContainersAsync(cancellationToken);
                    await ContainerRepository.InitalizeContainers(_geneProcessLimit, Program.DockerImageName, cancellationToken);
                    break;
            }

            _arenaStartTime = DateTime.Now;

            Console.WriteLine("welcome to Colosseum. enjoy the show :)");

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

            var generationNumber = 1;

            var arenaDir = new DirectoryInfo("arena");
            arenaDir.Create();

            var currentRunDir = arenaDir.CreateSubdirectory(DateTime.Now.ToString("s").Replace(" ", "-").ToValidFileName());
            currentRunDir.Create();

            var port = _startPort;

            var cts = new CancellationTokenSource();
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
                        await DockerService.StopAndRemoveAllContainersAsync(cancellationToken);
                        break;
                    case CompetetionMode.ReusableContainer:
                        break;
                }

                Console.WriteLine($"running generation #{generationNumber}");
                Console.WriteLine($"this generation will have {newGeneration.Count} genes and we'll process up to {_geneProcessLimit} genes simultaneously");
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

        private static readonly SemaphoreSlim _geneProcessSemaphoreSlim = new SemaphoreSlim(_geneProcessLimit);
        private static readonly SemaphoreSlim _geneProcessLogSemaphoreSlim = new SemaphoreSlim(1);
        private static readonly List<TimeSpan> _geneProcessTimes = new List<TimeSpan>();

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
                    var defenseOutputPath = ClientManager.GetClientOutputPath(geneDir, ClientMode.defend);
                    var scoreString = (await File.ReadAllLinesAsync(defenseOutputPath, cancellationToken)).First();
                    if (double.TryParse(scoreString, out var score))
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
                await _geneProcessLogSemaphoreSlim.WaitAsync(cancellationToken);
                try
                {
                    _geneProcessTimes.AddThreadSafe(processStopwatch.Elapsed);
                    var processCount = _geneProcessTimes.Count;
                    Console.Write($"{processCount.ToString().PadRight(8)}");

                    var successState = "";
                    if (competitionResult.Status == CompetitionResultStatus.Successful)
                        successState = "x" + (competitionResult.TryCount == 1 ? "" : competitionResult.TryCount.ToString());
                    Console.Write($"{successState.PadRight(8)}");

                    var score = gene.Score.HasValue ? gene.Score?.ToString("F2") : "null";
                    Console.Write($"{score.PadRight(16)}");
                    Console.Write($"{gene.Id.ToString().PadRight(16)}");
                    var processElapsed = processStopwatch.Elapsed;
                    Console.Write($"{processElapsed.ToString(@"mm\:ss\.fff").PadRight(16)}");
                    var arenaElapse = DateTime.Now - _arenaStartTime;
                    Console.Write($"{arenaElapse.ToString(@"hh\:mm\:ss\.fff").PadRight(24)}");
                    var allProcessAverage = arenaElapse / processCount;
                    var taskPerMinute = TimeSpan.FromSeconds(60) / allProcessAverage;
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

        private static async Task<CompetitionResult> runCompetition(DirectoryInfo rootDirectory, Gene gene, int port, string mapPath, CompetetionMode competetionMode, CancellationToken cancellationToken = default)
        {
            var tryCount = 1;

            while (tryCount <= _maximumTryCount)
            {
                bool result;

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
                        throw new ArgumentOutOfRangeException(nameof(competetionMode), competetionMode, null);
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

        private static async Task initializeCompeteionDirectory(Gene gene, int port, string mapPath, DirectoryInfo rootDirectory, bool overWriteFiles = false, CancellationToken cancellationToken = default)
        {
            await ServerManager.InitializeServerFiles(rootDirectory, mapPath, port, overWriteFiles, cancellationToken);

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

            //await initializeCompeteionDirectory(gene, 7099, mapPath, rootDirectory, overWriteFiles: true, cancellationToken: cancellationToken);

            var containerInfo = await ContainerRepository.GetAFreeContainer(cancellationToken);
            try
            {
                //foreach (var file in rootDirectory.GetFiles())
                //{
                //    file.CopyTo(Path.Combine(containerInfo.FilesDirectory.FullName, file.Name), true);
                //}

                await initializeCompeteionDirectory(gene, 7099, mapPath, containerInfo.FilesDirectory, overWriteFiles: true, cancellationToken: cancellationToken);

                await DockerService.StartContainer(containerInfo.Id, cancellationToken);
                var startTime = DateTime.Now;
                while (await DockerService.IsContainerRunningAsync(containerInfo.Id, cancellationToken))
                {
                    if ((DateTime.Now - startTime) > _maximumAllowedRunTime)
                    {
                        //Console.WriteLine($"gene {geneId} didn't finish");
                        hasFinished = false;
                        break;
                    }
                    await Task.Delay(1000, cancellationToken);
                }

                foreach (var file in containerInfo.FilesDirectory.GetFiles())
                {
                    file.CopyTo(Path.Combine(rootDirectory.FullName, file.Name), true);
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
                containerInfo.Release();
            }

        }

        private static async Task<bool> runCompetitionInsideContainer(Gene gene, string mapPath, DirectoryInfo rootDirectory, CancellationToken cancellationToken = default)
        {
            await initializeCompeteionDirectory(gene, 7099, mapPath, rootDirectory, cancellationToken: cancellationToken);

            var containerId = await DockerService.RunArenaAsync(Program.DockerImageName, rootDirectory.FullName, cancellationToken);
            var startTime = DateTime.Now;
            var hasFinished = true;

            while (await DockerService.IsContainerRunningAsync(containerId, cancellationToken))
            {
                if ((DateTime.Now - startTime) > _maximumAllowedRunTime)
                {
                    //Console.WriteLine($"gene {geneId} didn't finish");
                    hasFinished = false;
                    break;
                }
                await Task.Delay(1000, cancellationToken);
            }

            if (hasFinished)
            {
                var defenderClientOutputPath = ClientManager.GetClientOutputPath(rootDirectory, ClientMode.defend);
                var attackerClientOutputPath = ClientManager.GetClientOutputPath(rootDirectory, ClientMode.attack);

                hasFinished = File.Exists(defenderClientOutputPath) && File.Exists(attackerClientOutputPath);
            }

            var containerInfoPath = Path.Combine(rootDirectory.FullName, "container.info");
            await File.AppendAllLinesAsync(containerInfoPath,
                await DockerService.GetContainerInfo(containerId, showAll: true, cancellationToken: cancellationToken),
                cancellationToken);

            var containerInspect = Path.Combine(rootDirectory.FullName, "container.inspect.json");
            await File.WriteAllTextAsync(containerInspect,
                await DockerService.GetContainerInspect(containerId, cancellationToken: cancellationToken),
                cancellationToken);

            var containerLogPath = Path.Combine(rootDirectory.FullName, "container.log");
            await File.WriteAllTextAsync(containerLogPath,
                await DockerService.ContainerLogAsync(containerId, cancellationToken),
                cancellationToken);

            await DockerService.StopAndRemoveContainerAsync(containerId, cancellationToken);

            return hasFinished;
        }

        private static async Task<bool> runCompetitionInsideHost(Gene gene, int port, string mapPath, DirectoryInfo rootDirectory, CancellationToken cancellationToken)
        {
            await initializeCompeteionDirectory(gene, port, mapPath, rootDirectory, cancellationToken: cancellationToken);

            var hasFinished = true;

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

            var startTime = DateTime.Now;
            while (defendClientPayload.IsRunning() && attackClientPayload.IsRunning())
            {
                if ((DateTime.Now - startTime) > _maximumAllowedRunTime)
                {
                    //Console.WriteLine($"gene {geneId} didn't finish");
                    hasFinished = false;
                    break;
                }
                await Task.Delay(1000, cancellationToken);

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

        private static CommandInfo _cleanSystemCommand =>
            new CommandInfo
            {
                FileName = @"C:\WINDOWS\system32\taskkill.EXE",
                Args = "/f /im java.exe",
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
