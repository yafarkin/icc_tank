using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TankClient
{
    class Program
    {
        public static Queue<ConsoleKey> Keys = new Queue<ConsoleKey>();

        static void Main(string[] args)
        {
            var server = ConfigurationManager.AppSettings["server"];
            if (server.Contains("0.0.0.0") || server.Contains("localhost"))
            {
                server = $"ws://{Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(z => z.AddressFamily == AddressFamily.InterNetwork)}:{server.Split(':').Last()}";
            }
            var nickname = ConfigurationManager.AppSettings["nickname"];

            Console.WriteLine("Запуск клиента");

            var isSpectator = false;

            Console.WriteLine("Нажмите 'Esc', что бы выйти");

            var tokenSource = new CancellationTokenSource();
            var clientCore = new ClientCore(server, isSpectator ? string.Empty : nickname);
            var botClass = isSpectator ? new Spectator(tokenSource.Token) as IClientBot : new ManualClient();

            var clientThread = new Thread(() => { clientCore.Run(!isSpectator, botClass.Client, tokenSource.Token); });
            clientThread.Start();

            Console.WriteLine("Нажмите клавишу 'Esc' для завершения работы клиента.");
            while (clientThread.IsAlive)
            {
                Thread.Sleep(100);

                if (Console.KeyAvailable)
                {
                    var c = Console.ReadKey(true);
                    Keys.Enqueue(c.Key);

                    if (c.Key == ConsoleKey.Escape)
                    {
                        Keys.Dequeue();
                        tokenSource.Cancel();
                    }
                }
            }

            Console.WriteLine("Завершение клиента. Нажмите Enter для выхода");
            Console.ReadLine();
        }
    }
}