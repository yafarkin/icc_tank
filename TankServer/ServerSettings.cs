using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TankServer
{
    class ServerSettings
    {
        public string SessionName { get; set; }
        public TankCommon.Enum.MapType MapType { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int MaxClientCount { get; set; }
    }
}
