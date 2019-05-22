using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using AdminPanel.Entity;
using Microsoft.AspNetCore.Mvc;
using TankCommon.Objects;
using TankServer;
using TankCommon;

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
            if (serverSettings.SessionName == string.Empty) return;

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
/*
            var tankSettings = new TankSettings()
            {
                Version = 1,
                gameSpeed = (int)gameSpeed,
                tankSpeed = (int)tankSpeed,
                bulletSpeed = (int)bulletSpeed,
                tankDamage = (int)tankDamage
            };

            var serverSettings = new ServerSettings()
            {
                SessionName = nameSession,
                MapType = (TankCommon.Enum.MapType)1,
                Width = (int)width,
                Height = (int)height,
                MaxClientCount = (uint)maxClientsCount,
                Port = port,
                ServerType = TankCommon.Enum.ServerType.BattleCity,
                TankSettings = tankSettings
            };    */        

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

        /// <summary>
        /// Отправка класса с настройками сервера UI
        /// </summary>
        /// <returns></returns>
        public IEnumerable<object> GetServerTypeInfo()
        {
            var server = new ServerSettings();
            var res = server.GetType().GetProperties();
            var result = res.Select(z => new
            {
                Name = z.Name,
                Text = z.GetDescription(),
                Value = z.GetValue(server)
            });

            return result;
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
            bool update = false;
            if (Program.ServerStatusIsRun(id))
            {
                var server = Program.Servers[id - 1].Server;
                if (gameSpeed != null)
                {
                    server.serverSettings.TankSettings.GameSpeed = (int)gameSpeed;
                    update = true;
                }
                if (tankSpeed != null)
                {
                    server.serverSettings.TankSettings.TankSpeed = (decimal)tankSpeed;
                    update = true;
                }
                if (bulletSpeed != null)
                {
                    server.serverSettings.TankSettings.BulletSpeed = (decimal)bulletSpeed;
                    update = true;
                }
                if (tankDamage != null)
                {
                    server.serverSettings.TankSettings.TankDamage = (decimal)tankDamage;
                    update = true;
                }
                if (update) server.serverSettings.TankSettings.Version++;
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