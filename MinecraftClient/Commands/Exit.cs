using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class Exit : Command
    {
        public override string CMDName { get { return "exit"; } }
        public override string CMDDesc { get { return "exit: disconnect from the server."; } }
        
        public override string Run(McTcpClient handler, string command)
        {
            Program.Exit();
            return "";
        }

        public override IEnumerable<string> getCMDAliases()
        {
            return new string[] { "quit" };
        }
    }
}
