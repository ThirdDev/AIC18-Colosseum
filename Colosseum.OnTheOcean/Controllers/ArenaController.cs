using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Colosseum.GS;
using Colosseum.Services.Client;
using Colosseum.Services.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Colosseum.OnTheOcean.Controllers
{
    [Produces("application/json")]
    [Route("api/arena")]
    public class ArenaController : Controller
    {
        static TimeSpan runTimeLimit = TimeSpan.FromSeconds(60);

        [HttpPost]
        public async Task<ActionResult> Run([FromBody] Gene gene, [FromBody] string mapFileName, CancellationToken cancellationToken)
        {

            var finalFilePath = await run(gene, $"maps/{mapFileName}", cancellationToken);

            return File(finalFilePath, "application/octet-stream");
        }

        static SemaphoreSlim _runSemaphore = new SemaphoreSlim(1);

        private static async Task<string> run(Gene gene, string mapPath, CancellationToken cancellationToken)
        {
            await _runSemaphore.WaitAsync();
            try
            {
                var arenaDir = new DirectoryInfo("arena");
                arenaDir.Create();

                var geneDir = arenaDir.CreateSubdirectory(gene.Id.ToString());

                var port = 7099;

                await ServerManager.InitializeServerFiles(geneDir, mapPath, port, cancellationToken);

                await ClientManager.InitializeClientFiles(geneDir, gene, ClientMode.attack, cancellationToken);
                await ClientManager.InitializeClientFiles(geneDir, gene, ClientMode.defend, cancellationToken);

                var serverProcessPayload = await ServerManager.RunServer(geneDir, cancellationToken);
                if (!serverProcessPayload.IsRunning())
                {
                    throw new Exception("server is not running");
                }

                var attackClientPayload = await ClientManager.RunClient(geneDir, port, ClientMode.attack, cancellationToken: cancellationToken);
                if (!attackClientPayload.IsRunning())
                {
                    throw new Exception("attack client is not running");
                }

                var defendClientPayload = await ClientManager.RunClient(geneDir, port, ClientMode.defend, cancellationToken: cancellationToken);
                if (!defendClientPayload.IsRunning())
                {
                    throw new Exception("defend client is not running");
                }

                DateTime startTime = DateTime.Now;
                while (defendClientPayload.IsRunning() && attackClientPayload.IsRunning())
                {
                    if (DateTime.Now - startTime > runTimeLimit)
                    {
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

                var zipPath = Path.Combine(arenaDir.FullName, $"{gene.Id}.zip");
                ZipFile.CreateFromDirectory(geneDir.FullName, zipPath);

                return zipPath;
            }
            finally
            {
                _runSemaphore.Release();
            }
        }
    }
}