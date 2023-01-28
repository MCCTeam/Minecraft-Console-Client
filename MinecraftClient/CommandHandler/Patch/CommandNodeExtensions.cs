using System.Collections.Generic;
using System.Reflection;
using Brigadier.NET.Tree;

namespace MinecraftClient.CommandHandler.Patch
{
    public static class CommandNodeExtensions
    {
        public static void RemoveChild(this CommandNode<CmdResult> commandNode, string name)
        {
            var children = (IDictionary<string, CommandNode<CmdResult>>)
                typeof(CommandNode<CmdResult>)
                .GetField("_children", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(commandNode)!;
            var literals = (IDictionary<string, LiteralCommandNode<CmdResult>>)
                typeof(CommandNode<CmdResult>)
                .GetField("_literals", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(commandNode)!;
            var arguments = (IDictionary<string, ArgumentCommandNode<CmdResult>>)
                typeof(CommandNode<CmdResult>)
                .GetField("_arguments", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(commandNode)!;

            children.Remove(name);
            literals.Remove(name);
        }
    }
}