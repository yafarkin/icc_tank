using System.Collections.Generic;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace AdminPanel
{
    public class Program
    {
        public static List<Entity.ServerEntity> servers = new List<Entity.ServerEntity>();

        public static bool ServerStatusIsRun(int id)
        {
            bool stutus = false;
            if (id - 1 < servers.Count)
            {
                if (servers[id - 1].Task.IsCompleted)
                {
                    stutus = true;
                }
            }

            return stutus;
        }

        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
