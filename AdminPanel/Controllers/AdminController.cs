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
    public class AdminController : ControllerBase
    {
        [HttpPost]
        public void CreateServer([FromForm] string nameGame, [FromForm] int maxBotsCount, [FromForm] int botUpdateMs, [FromForm] int coreUpdateMs, [FromForm] int spectatorUpdateMs, [FromForm] int port)
        {
            var gameType = new GameEntity()
            {
                Name = nameGame,
                MaxBotsCount = maxBotsCount,
                BotUpdateMs = botUpdateMs,
                CoreUpdateMs = coreUpdateMs,
                SpectatorUpdateMs = spectatorUpdateMs
                
            };

            var newPort = Convert.ToUInt32(port);
            while (true)
            {
                if (Program.servers.Any(x => x.Port == newPort))
                {
                    newPort += 10;
                }
                else
                {
                    break;
                }
            }

            TankCommon.Objects.Map map = TankCommon.MapManager.LoadMap(20, TankCommon.Enum.CellMapType.Wall, 50, 50);
            var server = new Server(map, newPort, Convert.ToUInt32(maxBotsCount), Convert.ToUInt32(coreUpdateMs), Convert.ToUInt32(spectatorUpdateMs), Convert.ToUInt32(botUpdateMs));
            var cancellationToken = new CancellationTokenSource();

            Program.servers.Add(new ServerEntity()
            {
                Id = Program.servers.Count == 0 ? 1 : Program.servers[Program.servers.Count - 1].Id + 1,
                CancellationToken = cancellationToken,
                GameType = gameType,
                Port = newPort,
                Server = server,
                Task = server.Run(cancellationToken.Token)
            });                        
        }
        
        [HttpPost]
        public void StartServer()
        {
            // TODO TBD технической возможности запуска
        }

        [HttpPost]
        public void ChangeServerSettings([FromBody] int id)
        {

        }

        [HttpPost]
        public void StopServer([FromBody] int id)
        {
            if (id - 1 < Program.servers.Count)
            {
                var server = Program.servers[id - 1];
                if (server.Task.IsCompleted)
                {
                    server.CancellationToken.Cancel();
                }
                Program.servers.Remove(server);
            }
        }
    }
}
