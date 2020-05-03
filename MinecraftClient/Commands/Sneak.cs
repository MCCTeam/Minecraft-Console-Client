using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class Sneak : Command
    {
        private bool sneaking = false;
        public override string CMDName { get { return "Sneak"; } }
        public override string CMDDesc { get { return "Sneak: Toggles sneaking"; } }

        public override string Run(McTcpClient handler, string command, Dictionary<string, object> localVars)
        {
            Console.WriteLine(command);
            if (sneaking)
            {
                var result = handler.sendEntityAction(Protocol.EntityActionType.StopSneaking);
                sneaking = false;
                return  result ? "Success" : "Fail";
            }
            else
            {
                var result = handler.sendEntityAction(Protocol.EntityActionType.StartSneaking);
                sneaking = true;
                return  result ? "Success" : "Fail";
            }
            
        }
    }
}