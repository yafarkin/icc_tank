using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AdminPanel.Entity
{
    public class ServerEntity
    {
        public int Id { get; set; }
        public Game GameType { get; set; }
        public uint Port { get; set; }
        public CancellationTokenSource CancellationToken { get; set; }
        public Task Task { get; set; }
        public TankServer.Server Server { get; set; }        
    }
}
