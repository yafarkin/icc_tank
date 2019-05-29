using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AdminPanel.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
        public string CreateServer([FromForm] string request)
        {
            var serverSettings = request.FromJson<ServerSettings>();
            if (string.IsNullOrWhiteSpace(serverSettings.SessionName))
            {
                var message = new {error = "Имя не должно быть пустым"};
                return message.ToJson();
            }

            var port = 2000;
            while (true)
            {
                lock (Program.Servers)
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
            }

            try
            {
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile("serversetting.json")
                    .Build();

                serverSettings.ServerTickRate = Convert.ToInt32(configuration["ServerTickRate"]);
                serverSettings.PlayerTickRate = Convert.ToInt32(configuration["PlayerTickRate"]);
                serverSettings.SpectatorTickRate = Convert.ToInt32(configuration["SpectatorTickRate"]);

                serverSettings.Port = port;

                var server = new Server(serverSettings, Program.Logger);

                lock (Program.Servers)
                {
                    Program.Servers.Add(new ServerEntity()
                    {
                        Id = Program.Servers.Count == 0 ? 1 : Program.Servers.Count + 1,
                        Server = server,
                        Port = (uint) port,
                        Task = server.Run(new CancellationTokenSource().Token)
                    });
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Error($"Ошибка во время работы: {ex}");
                return (new {error = ex.Message}).ToJson();
            }

            return null;
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
                    Name = ((MapType) item).GetDescription(),
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
                    Name = ((ServerType) item).GetDescription(),
                    Id = item
                });
            }

            return result;
        }

        [HttpGet]
        public string GetServerList()
        {
            var ips = Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(z => z.AddressFamily == AddressFamily.InterNetwork);
            Task.Delay(150);
            var result = Program.Servers.Select(x => new
            {
                Id = x.Id,
                Name = x.Server.serverSettings.SessionName,
                Ip = string.Join(", ", ips),
                Port = x.Server.serverSettings.Port,
                Type = x.Server.serverSettings.ServerType.GetDescription(),
                People = x.Server.Clients.Count(z => !string.IsNullOrWhiteSpace(z.Value.Nickname)) + " / " + x.Server.serverSettings.MaxClientCount
            });

            return result.ToJson();
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
        public void ChangeServerSettings(int id, [FromForm] string request)
        {
            var tankSettings = request.FromJson<TankSettings>();
            if (tankSettings == null) return;
            var server = Program.Servers.FirstOrDefault(x => x.Id == id && !x.Task.IsCanceled);

            if (server == null)
            {
                return;
            }

            server.Server.serverSettings.TankSettings = tankSettings;
        }

        /// <summary>
        /// Останавливает сервер
        /// </summary>
        /// <param name="id">Номер сервера</param>
        [HttpPost]
        public string StopServer(int id)
        {
            var server = Program.Servers.FirstOrDefault(x => x.Id == id && !x.Task.IsCanceled);
                
            if (server != null)
            {
                server.Server.Dispose();
                Task.Delay(150);
                Program.Servers.Remove(server);
            }

            return null;
        }
    }
}