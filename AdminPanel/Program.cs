using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace AdminPanel
{
    public class Program
    {
        public static List<Entity.ServerEntity> Servers = new List<Entity.ServerEntity>();

        public static bool ServerStatusIsRun(int id)
        {
            var status = (Servers.FirstOrDefault(x => x.Id == id)?.Task.Status
                ?? System.Threading.Tasks.TaskStatus.Faulted) == System.Threading.Tasks.TaskStatus.Running;

            return status;
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
