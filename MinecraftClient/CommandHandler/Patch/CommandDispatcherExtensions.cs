using System.Text;
using Brigadier.NET;

namespace MinecraftClient.CommandHandler.Patch
{
    public static class CommandDispatcherExtensions
    {
        /**
        * This method unregisteres a previously declared command
        *
        * @param The name of the command to remove
        */
        public static void Unregister(this CommandDispatcher<CmdResult> commandDispatcher, string commandname)
        {
            commandDispatcher.GetRoot().RemoveChild(commandname);
        }

        public static string GetAllUsageString(this CommandDispatcher<CmdResult> commandDispatcher, string commandName, bool restricted)
        {
            char cmdChar = Settings.Config.Main.Advanced.InternalCmdChar.ToChar();
            try
            {
                string[] usages = commandDispatcher.GetAllUsage(commandDispatcher.GetRoot().GetChild(commandName), new(), restricted);
                StringBuilder sb = new();
                sb.AppendLine("All Usages:");
                foreach (var usage in usages)
                {
                    sb.Append(cmdChar).Append(commandName).Append(' ');
                    if (usage.Length > 0 && usage[0] == '_')
                        sb.AppendLine(usage.Replace("_help -> ", $"_help -> {cmdChar}help "));
                    else
                        sb.AppendLine(usage);
                }
                sb.Remove(sb.Length - 1, 1);
                return sb.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}