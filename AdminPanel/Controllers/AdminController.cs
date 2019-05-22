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
        /// <param name="maxClientsCount">Колличество одновременно играющих на сервере</param>
        /// <param name="nameSession"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
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
                GameSpeed = (int)gameSpeed,
                TankSpeed = (int)tankSpeed,
                BulletSpeed = (int)bulletSpeed,
                TankDamage = (int)tankDamage
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
        /// <param name="GameSpeed">Скорость игры</param>
        /// <param name="TankSpeed">Скорость танка</param>
        /// <param name="BulletSpeed">Скорость Пули</param>
        /// <param name="TankDamage">Урон танков</param>
        [HttpPost]
        public void ChangeServerSettings([FromForm] int id, [FromForm] decimal? GameSpeed, [FromForm] decimal? TankSpeed, [FromForm] decimal? BulletSpeed, [FromForm] decimal? TankDamage)
        {
            bool update = false;
            if (Program.ServerStatusIsRun(id))
            {
                var server = Program.Servers[id - 1].Server;
                if (GameSpeed != null)
                {
                    server.serverSettings.TankSettings.GameSpeed = (int)GameSpeed;
                    update = true;
                }
                if (TankSpeed != null)
                {
                    server.serverSettings.TankSettings.TankSpeed = (decimal)TankSpeed;
                    update = true;
                }
                if (BulletSpeed != null)
                {
                    server.serverSettings.TankSettings.BulletSpeed = (decimal)BulletSpeed;
                    update = true;
                }
                if (TankDamage != null)
                {
                    server.serverSettings.TankSettings.TankDamage = (decimal)TankDamage;
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