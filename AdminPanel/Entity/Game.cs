using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdminPanel.Entity
{
    public class Game
    {
        string name { get; set; }        
        uint maxBotsCount { get; set; }
        uint coreUpdateMs { get; set; }
        uint spectatorUpdateMs { get; set; }
        uint botUpdateMs { get; set; }
    }
}
