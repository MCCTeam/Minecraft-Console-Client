﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class Sneak : Command
    {
        private bool sneaking = false;
        public override string CMDName { get { return "TSneak"; } }
        public override string CMDDesc { get { return "Sneak: Toggles sneaking"; } }

        public override string Run(McTcpClient handler, string command, Dictionary<string, object> localVars)
        {
            if (sneaking)
            {
                var result = handler.sendEntityAction(Protocol.ActionType.StopSneaking);
                sneaking = false;
                return  result ? "Success" : "Fail";
            }
            else
            {
                var result = handler.sendEntityAction(Protocol.ActionType.StartSneaking);
                sneaking = true;
                return  result ? "Success" : "Fail";
            }
            
        }
    }
}