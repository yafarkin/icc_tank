using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AdminPanel.Entity;
using Microsoft.AspNetCore.Mvc;
using TankCommon.Objects;
using TankServer;
using TankCommon;
using TankCommon.Enum;

namespace AdminPanel.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        /// <summary>
        /// Создание сервера
        /// </summary>
        /// <param name="serverSettings">Класс настроек сервера и темпа игры</param>
        [HttpPost]
        public void CreateServer([FromForm] ServerSettings serverSettings)
        {
            if (string.IsNullOrWhiteSpace(serverSettings.SessionName)) return;

            var port = 2000;
            while (true)
            {
                if (Program.Servers.Any(x => x.Port == port))
                {
                    port += 10;
                }
                else
                {
                    break;
                }
            }

            serverSettings.Port = port;

            var server = new Server(serverSettings);
            var cancellationToken = new CancellationTokenSource();

            Program.Servers.Add(new ServerEntity()
            {
                Id = Program.Servers.Count == 0 ? 1 : Program.Servers.Count,
                CancellationToken = cancellationToken,
                Server = server,
                Task = server.Run(cancellationToken.Token)
            });                        
        }

        /// <summary>
        /// Запуск сервера(снятие режима пауза)
        /// </summary>
        [HttpPost]
        public void StartServer()
        {
            // TODO TBD технической возможности запуска
        }

        [HttpGet]
        public IEnumerable<object> GetMapTypes()
        {
            var result = new List<object>();
            foreach (var item in Enum.GetValues(typeof(MapType)))
            {
                result.Add(new
                {
                    Name = Enum.GetName(typeof(MapType), item),
                    Id = item
                });
            }

            return result;
        }

        [HttpGet]
        public IEnumerable<object> GetServerTypes()
        {
            var result = new List<object>();
            foreach (var item in Enum.GetValues(typeof(ServerType)))
            {
                result.Add(new
                {
                    Name = Enum.GetName(typeof(ServerType), item),
                    Id = item
                });
            }

            return result;
        }

        [HttpGet]
        public IEnumerable<object> GetServerList()
        {
            Task.Delay(250);
            return Program.Servers.Select(x => new
            {
                Id = x.Id,
                Name = x.Server.serverSettings.SessionName,
                Ip = Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(z => z.AddressFamily == AddressFamily.InterNetwork)?.ToString(),
                Port = x.Server.serverSettings.Port,
                Type = x.Server.serverSettings.ServerType.GetDescription(),
                People = x.Server.Clients.Count(z => !z.Value.IsSpecator) + " / " + x.Server.serverSettings.MaxClientCount
            });
        }

        /// <summary>
        /// Изменение настроек сервера
        /// </summary>
        /// <param name="id">Номер сервера</param>
        /// <param name="gameSpeed">Скорость игры</param>
        /// <param name="tankSpeed">Скорость танка</param>
        /// <param name="bulletSpeed">Скорость Пули</param>
        /// <param name="tankDamage">Урон танков</param>
        [HttpPost]
        public void ChangeServerSettings([FromForm] int id, [FromForm] decimal? gameSpeed, [FromForm] decimal? tankSpeed, [FromForm] decimal? bulletSpeed, [FromForm] decimal? tankDamage)
        {
            if (id == 0 || (gameSpeed ?? 0) != 0 && (tankSpeed ?? 0) != 0 && (bulletSpeed ?? 0) != 0 && (tankDamage ?? 0) != 0)
            {
                return;
            }

            if (Program.ServerStatusIsRun(id))
            {
                var newSettings = new TankSettings();
                var server = Program.Servers.FirstOrDefault(x => x.Id == id);
                if (server == null)
                {
                    return;
                }

                if ((gameSpeed ?? 0) != 0)
                {
                    newSettings.GameSpeed = (int)gameSpeed;
                }
                if ((tankSpeed ?? 0) != 0)
                {
                    newSettings.TankSpeed = (decimal)tankSpeed;
                }
                if ((bulletSpeed ?? 0) != 0)
                {
                    newSettings.BulletSpeed = (decimal)bulletSpeed;
                }
                if ((tankDamage ?? 0) != 0)
                {
                    newSettings.TankDamage = (decimal)tankDamage;
                }

                server.Server.serverSettings.TankSettings = newSettings;
            }
        }
        
        /// <summary>
        /// Останавливает сервер
        /// </summary>
        /// <param name="id">Номер сервера</param>
        [HttpPost]
        public void StopServer([FromForm] int id)
        {
            var status = Program.ServerStatusIsRun(id);
                
            if (status)
            {
                var server = Program.Servers[id - 1];
                server.CancellationToken.Cancel();
                Program.Servers.Remove(server);
            }
        }
    }
}