using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdminPanel.Entity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TankServer;

namespace AdminPanel.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        [HttpPost]
        public void StartServer([FromForm]int port, [FromForm] string nameGame, [FromForm] int maxBotsCount,
            [FromForm]int coreUpdateMs, [FromForm] int spectatorUpdateMs, [FromForm] int botUpdateMs)
        {
            var gameType = new Game()
            {
                Name = nameGame,
                MaxBotsCount = maxBotsCount,
                BotUpdateMs = botUpdateMs,
                CoreUpdateMs = coreUpdateMs,
                SpectatorUpdateMs = spectatorUpdateMs
            };

            var newPort = Convert.ToUInt32(port);
            TankCommon.Objects.Map map = TankCommon.MapManager.LoadMap(20, TankCommon.Enum.CellMapType.Wall, 50, 50);
            var server = new Server(map, newPort, Convert.ToUInt32(maxBotsCount), Convert.ToUInt32(coreUpdateMs), Convert.ToUInt32(spectatorUpdateMs), Convert.ToUInt32(botUpdateMs));
            var cancellationToken = new CancellationTokenSource();

            Test.servers.Add(new ServerEntity()
            {
                Id = Test.servers.Count == 0 ? 1 : Test.servers[Test.servers.Count - 1].Id + 1,
                CancellationToken = cancellationToken,
                GameType = gameType,
                Port = newPort,
                Server = server,
                Task = server.Run(cancellationToken.Token)
            });                        
        }

        [HttpPost]
        public void StopServer([FromBody] int id)
        {
            if (id - 1 < Test.servers.Count)
            {
                var server = Test.servers[id - 1];
                if (server.Task.IsCompleted)
                {
                    server.CancellationToken.Cancel();
                }
                Test.servers.Remove(server);
            }
        }
    }
}
