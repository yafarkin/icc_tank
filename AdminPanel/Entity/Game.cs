using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdminPanel.Entity
{
    public class Game
    {
        public string Name { get; set; }
        public int MaxBotsCount { get; set; }
        public int CoreUpdateMs { get; set; }
        public int SpectatorUpdateMs { get; set; }
        public int BotUpdateMs { get; set; }
    }
}
