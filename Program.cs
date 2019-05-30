using System;
using System.Linq;
using System.Net;
using System.Threading;
using TankCommon;
using TankServer;
using TankCommon.Enum;
using TankCommon.Objects;

namespace ICC_Tank
{
    class Program
    {
        static uint ParseOrDefault(string v, uint defaultValue)
        {
            uint u;

            if (string.IsNullOrWhiteSpace(v))
            {
                return defaultValue;
            }

            if (uint.TryParse(v, out u))
            {
                return u;
            }

            return defaultValue;
        }

        static void Main(string[] args)
        {
            // 50 на 50 ... Идеальный баланс... эталон гармонии

            var map = MapManager.LoadMap(20, 20, CellMapType.Wall, 30, 30);
            Console.WriteLine($"Сгенерирована карта");

            var serverSetting = new ServerSettings()
            {
                Height = (int)ParseOrDefault(System.Configuration.ConfigurationManager.AppSettings["height"], 20),
                Width = (int)ParseOrDefault(System.Configuration.ConfigurationManager.AppSettings["width"], 20),
                CountOfLifes = (int)ParseOrDefault(System.Configuration.ConfigurationManager.AppSettings["countOfLifes"], 5),
                MapType = MapType.Base,
                MaxClientCount = ParseOrDefault(System.Configuration.ConfigurationManager.AppSettings["maxClientsCount"], 1000),
                MaxCountOfUpgrade = (int)ParseOrDefault(System.Configuration.ConfigurationManager.AppSettings["maxCountOfUpgrade"], 3),
                PlayerTickRate = (int)ParseOrDefault(System.Configuration.ConfigurationManager.AppSettings["playerTickRate"], 1000),
                Port = (int)ParseOrDefault(System.Configuration.ConfigurationManager.AppSettings["port"], 2000),
                ServerTickRate = (int)ParseOrDefault(System.Configuration.ConfigurationManager.AppSettings["serverTickRate"], 50),
                ServerType = ServerType.BattleCity,
                SessionName = "ICC_TANK",
                TimeOfInvulnerabilityAfterRespawn = 5000,
                SpectatorTickRate = (int)ParseOrDefault(System.Configuration.ConfigurationManager.AppSettings["spectatorTickRate"], 150),
                TankSettings = new TankSettings()
            };
            
            var strHostName = Dns.GetHostName();
            var ipEntry = Dns.GetHostEntry(strHostName);
            var ipAddresses = /*ipEntry.AddressList*/"ws://10.22.2.120:2000";

            Console.WriteLine($"Соединение по имени: ws://{strHostName}:{serverSetting.Port}");
            Console.WriteLine("Или по IP адресу(ам):");
            foreach (var ipAddress in ipAddresses)
            {
                Console.WriteLine($"\tws://{ipAddress}:{serverSetting.Port}");
            }
            
            Console.WriteLine("Нажмите Escape для выхода");

            var tokenSource = new CancellationTokenSource();

            var server = new Server(serverSetting, NLog.LogManager.GetCurrentClassLogger());
            var serverTask = server.Run(tokenSource.Token);
            
            try
            {
                while (!serverTask.IsCompleted)
                {
                    Thread.Sleep(100);

                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                    {
                        tokenSource.Cancel();
                    }
                }
            }
            finally
            {
                server.Dispose();

                Console.WriteLine("Завершение работы сервера. Нажмите Enter для выхода");
                Console.ReadLine();
            }
        }
    }
}
