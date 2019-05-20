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
        /// <param name="maxBotsCount">Колличество одновременно играющих на сервере</param>
        /// <param name="botUpdateMs">Частота обновления клиентов</param>
        /// <param name="coreUpdateMs">Время простоя сервера до его обновления</param>
        /// <param name="spectatorUpdateMs">Частота обновления наблюдателей</param>
        /// <param name="port">Порт по которому будет работать сервер</param>
        [HttpPost]
        public void CreateServer( int maxBotsCount, int botUpdateMs, int coreUpdateMs, int spectatorUpdateMs, int port)
        {
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
                Port = newPort,
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