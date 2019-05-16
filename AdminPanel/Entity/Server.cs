using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace AdminPanel.Entity
{
    public class Server
    {
        IPAddress[] ipAddresses { get; set; }
        string strHostName { get; set; }
        Game gameType { get; set; }
        uint port { get; set; }
    }
}
