using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    class Tps : Command
    {
        public override string CMDName { get { return "tps"; } }
        public override string CMDDesc { get { return "Display server current tps (tick per second). May not be accurate"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
        {
            var tps = handler.GetServerTPS();
            string color = "§a"; // Green
            if (tps <= 15)
                color = "§e";    // Yellow
            if (tps <= 10)
                color = "§c";    // Red
            return "Current tps: " + color + Math.Round(tps, 2);
        }
    }
}
