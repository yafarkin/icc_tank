using System;
using System.Linq;
using System.Threading;
using AdminPanel.Entity;
using Microsoft.AspNetCore.Mvc;
using TankServer;

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
        public void CreateServer([FromForm] int? maxClientsCount, [FromForm] string nameSession, [FromForm] int? width, [FromForm] int? height,
            [FromForm] int? gameSpeed, [FromForm] int? tankSpeed, [FromForm] int? bulletSpeed, [FromForm] int? tankDamage, [FromForm] int? sessionTime)
        {
            if (nameSession == String.Empty) return;
            if (maxClientsCount == null) maxClientsCount = 2;
            if (width == null) width = 20;
            if (height == null) height = 20;
            if (gameSpeed == null) gameSpeed = 1;
            if (tankSpeed == null) tankSpeed = 2;
            if (bulletSpeed == null) bulletSpeed = 7;
            if (tankDamage == null) tankDamage = 40;
            if (sessionTime == null) sessionTime = 5;

            var port = 1000;
            while (true)
            {
                if (Program.servers.Any(x => x.Port == port))
                {
                    port += 10;
                }
                else
                {
                    break;
                }
            }

            var serverSettings = new ServerSettings()
            {
                SessionName = nameSession,
                MapType = (TankCommon.Enum.MapType)1,
                Width = (int)width,
                Height = (int)height,
                MaxClientCount = (int)maxClientsCount,
                Port = port,
                SessionTime = DateTime.Now.AddMinutes((int)sessionTime) - DateTime.Now,
                ServerType = TankCommon.Enum.ServerType.BattleCity
            };

            var tankSettings = new TankCommon.TankSettings()
            {
                Version = 1,
                GameSpeed = (int)gameSpeed,
                TankSpeed = (int)tankSpeed,
                BulletSpeed = (int)bulletSpeed,
                TankDamage = (int)tankDamage
            };

//            TankCommon.Objects.Map map = TankCommon.MapManager.LoadMap(20, TankCommon.Enum.CellMapType.Wall, 50, 50);
            var server = new Server(serverSettings, tankSettings);
            var cancellationToken = new CancellationTokenSource();

            Program.servers.Add(new ServerEntity()
            {
                Id = Program.servers.Count == 0 ? 1 : Program.servers.Count,
                CancellationToken = cancellationToken,
//                Port = port,
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
        /// Изменение настроек сервера
        /// </summary>
        /// <param name="id">Номер сервера</param>
        /// <param name="GameSpeed">Скорость игры</param>
        /// <param name="TankSpeed">Скорость танка</param>
        /// <param name="BulletSpeed">Скорость Пули</param>
        /// <param name="TankDamage">Урон танков</param>
        /// <param name="ServerName">Имя сервера</param>
        /// <param name="ServerType">Игровой тип сервера</param>
        /// <param name="SessionTime">Время игрового матча</param>
        [HttpPost]
        public void ChangeServerSettings([FromForm] int id, [FromForm] decimal? GameSpeed, [FromForm] decimal? TankSpeed, [FromForm] decimal? BulletSpeed,
            [FromForm] decimal? TankDamage, [FromForm] string ServerName, [FromForm] string ServerType, [FromForm] string SessionTime)
        {
            if (Program.ServerStatusIsRun(id))
            {
                var server = Program.servers[id - 1].Server;
                if (GameSpeed != null) server._tankSettings.GameSpeed = (decimal)GameSpeed;
                if (TankSpeed != null) server._tankSettings.TankSpeed = (decimal)TankSpeed;
                if (BulletSpeed != null) server._tankSettings.BulletSpeed = (decimal)BulletSpeed;
                if (TankDamage != null) server._tankSettings.TankDamage = (decimal)TankDamage;
                if (ServerName != null) server._tankSettings.ServerName = ServerName;
                if (ServerType != null) server._tankSettings.ServerType = (TankCommon.Enum.ServerType)Enum.Parse(typeof(TankCommon.Enum.ServerType), ServerType);
                if (SessionTime != null) server._tankSettings.SessionTime = DateTime.Now.AddMinutes(int.Parse(SessionTime)) - DateTime.Now;
                server._tankSettings.Version++;
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
                var server = Program.servers[id - 1];
                server.CancellationToken.Cancel();
                Program.servers.Remove(server);
            }
                
        }
    }
}