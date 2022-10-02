using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    public class Animation : Command
    {
        public override string CmdName { get { return "animation"; } }
        public override string CmdUsage { get { return "animation <mainhand|offhand>"; } }
        public override string CmdDesc { get { return "cmd.animation.desc"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            if (HasArg(command))
            {
                string[] args = GetArgs(command);
                if (args.Length > 0)
                {
                    if (args[0] == "mainhand" || args[0] == "0")
                    {
                        handler.DoAnimation(0);
                        return Translations.Get("general.done");
                    }
                    else if (args[0] == "offhand" || args[0] == "1")
                    {
                        handler.DoAnimation(1);
                        return Translations.Get("general.done");
                    }
                    else
                    {
                        return GetCmdDescTranslated();
                    }
                }
                else
                {
                    return GetCmdDescTranslated();
                }
            }
            else return GetCmdDescTranslated();
        }
    }
}
