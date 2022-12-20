using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MinecraftClient.Protocol.PacketPipeline;

namespace MinecraftClient.Protocol.Handlers.packet.s2c
{
    internal static class DeclareCommands
    {
        private static int RootIdx;
        private static CommandNode[] Nodes = Array.Empty<CommandNode>();

        public static async Task Read(DataTypes dataTypes, PacketStream packetData)
        {
            int count = dataTypes.ReadNextVarInt(packetData);
            Nodes = new CommandNode[count];
            for (int i = 0; i < count; ++i)
            {
                byte flags = dataTypes.ReadNextByte(packetData);

                int childCount = dataTypes.ReadNextVarInt(packetData);
                int[] childs = new int[childCount];
                for (int j = 0; j < childCount; ++j)
                    childs[j] = dataTypes.ReadNextVarInt(packetData);

                int redirectNode = ((flags & 0x08) > 0) ? dataTypes.ReadNextVarInt(packetData) : -1;

                string? name = ((flags & 0x03) == 1 || (flags & 0x03) == 2) ? (await dataTypes.ReadNextStringAsync(packetData)) : null;

                int paserId = ((flags & 0x03) == 2) ? dataTypes.ReadNextVarInt(packetData) : -1;
                Paser? paser = null;
                if ((flags & 0x03) == 2)
                {
                    paser = paserId switch
                    {
                        1 => new PaserFloat(dataTypes, packetData),
                        2 => new PaserDouble(dataTypes, packetData),
                        3 => new PaserInteger(dataTypes, packetData),
                        4 => new PaserLong(dataTypes, packetData),
                        5 => new PaserString(dataTypes, packetData),
                        6 => new PaserEntity(dataTypes, packetData),
                        8 => new PaserBlockPos(dataTypes, packetData),
                        9 => new PaserColumnPos(dataTypes, packetData),
                        10 => new PaserVec3(dataTypes, packetData),
                        11 => new PaserVec2(dataTypes, packetData),
                        18 => new PaserMessage(dataTypes, packetData),
                        27 => new PaserRotation(dataTypes, packetData),
                        29 => new PaserScoreHolder(dataTypes, packetData),
                        43 => new PaserResourceOrTag(dataTypes, packetData),
                        44 => new PaserResource(dataTypes, packetData),
                        _ => new PaserEmpty(dataTypes, packetData),
                    };
                }

                string? suggestionsType = ((flags & 0x10) > 0) ? (await dataTypes.ReadNextStringAsync(packetData)) : null;

                Nodes[i] = new(flags, childs, redirectNode, name, paser, suggestionsType);
            }
            RootIdx = dataTypes.ReadNextVarInt(packetData);

            ConsoleIO.OnDeclareMinecraftCommand(ExtractRootCommand());
        }

        private static string[] ExtractRootCommand()
        {
            List<string> commands = new();
            CommandNode root = Nodes[RootIdx];
            foreach (var child in root.Clildren)
            {
                string? childName = Nodes[child].Name;
                if (childName != null)
                    commands.Add(childName);
            }
            return commands.ToArray();
        }

        public static List<Tuple<string, string>> CollectSignArguments(string command)
        {
            List<Tuple<string, string>> needSigned = new();
            CollectSignArguments(RootIdx, command, needSigned);
            return needSigned;
        }

        private static void CollectSignArguments(int NodeIdx, string command, List<Tuple<string, string>> arguments)
        {
            CommandNode node = Nodes[NodeIdx];
            string last_arg = command;
            switch (node.Flags & 0x03)
            {
                case 0: // root
                    break;
                case 1: // literal
                    {
                        string[] arg = command.Split(' ', 2, StringSplitOptions.None);
                        if (!(arg.Length == 2 && node.Name! == arg[0]))
                            return;
                        last_arg = arg[1];
                    }
                    break;
                case 2: // argument
                    {
                        int argCnt = (node.Paser == null) ? 1 : node.Paser.GetArgCnt();
                        string[] arg = command.Split(' ', argCnt + 1, StringSplitOptions.None);
                        if ((node.Flags & 0x04) > 0)
                        {
                            if (node.Paser != null && node.Paser.GetName() == "minecraft:message")
                                arguments.Add(new(node.Name!, command));
                        }
                        if (!(arg.Length == argCnt + 1))
                            return;
                        last_arg = arg[^1];
                    }
                    break;
                default:
                    break;
            }

            while (Nodes[NodeIdx].RedirectNode >= 0)
                NodeIdx = Nodes[NodeIdx].RedirectNode;

            foreach (int childIdx in Nodes[NodeIdx].Clildren)
                CollectSignArguments(childIdx, last_arg, arguments);
        }

        internal class CommandNode
        {
            public byte Flags;
            public int[] Clildren;
            public int RedirectNode;
            public string? Name;
            public Paser? Paser;
            public string? SuggestionsType;


            public CommandNode(byte Flags,
                        int[] Clildren,
                        int RedirectNode = -1,
                        string? Name = null,
                        Paser? Paser = null,
                        string? SuggestionsType = null)
            {
                this.Flags = Flags;
                this.Clildren = Clildren;
                this.RedirectNode = RedirectNode;
                this.Name = Name;
                this.Paser = Paser;
                this.SuggestionsType = SuggestionsType;
            }
        }

        internal abstract class Paser
        {
            public abstract string GetName();

            public abstract int GetArgCnt();

            public abstract bool Check(string text);
        }

        internal class PaserEmpty : Paser
        {

            public PaserEmpty(DataTypes dataTypes, PacketStream packetData) { }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 1;
            }

            public override string GetName()
            {
                return "";
            }
        }

        internal class PaserFloat : Paser
        {
            private byte Flags;
            private float Min = float.MinValue, Max = float.MaxValue;

            public PaserFloat(DataTypes dataTypes, PacketStream packetData)
            {
                Flags = dataTypes.ReadNextByte(packetData);
                if ((Flags & 0x01) > 0)
                    Min = dataTypes.ReadNextFloat(packetData);
                if ((Flags & 0x02) > 0)
                    Max = dataTypes.ReadNextFloat(packetData);
            }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 1;
            }

            public override string GetName()
            {
                return "brigadier:float";
            }
        }

        internal class PaserDouble : Paser
        {
            private byte Flags;
            private double Min = double.MinValue, Max = double.MaxValue;

            public PaserDouble(DataTypes dataTypes, PacketStream packetData)
            {
                Flags = dataTypes.ReadNextByte(packetData);
                if ((Flags & 0x01) > 0)
                    Min = dataTypes.ReadNextDouble(packetData);
                if ((Flags & 0x02) > 0)
                    Max = dataTypes.ReadNextDouble(packetData);
            }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 1;
            }

            public override string GetName()
            {
                return "brigadier:double";
            }
        }

        internal class PaserInteger : Paser
        {
            private byte Flags;
            private int Min = int.MinValue, Max = int.MaxValue;

            public PaserInteger(DataTypes dataTypes, PacketStream packetData)
            {
                Flags = dataTypes.ReadNextByte(packetData);
                if ((Flags & 0x01) > 0)
                    Min = dataTypes.ReadNextInt(packetData);
                if ((Flags & 0x02) > 0)
                    Max = dataTypes.ReadNextInt(packetData);
            }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 1;
            }

            public override string GetName()
            {
                return "brigadier:integer";
            }
        }

        internal class PaserLong : Paser
        {
            private byte Flags;
            private long Min = long.MinValue, Max = long.MaxValue;

            public PaserLong(DataTypes dataTypes, PacketStream packetData)
            {
                Flags = dataTypes.ReadNextByte(packetData);
                if ((Flags & 0x01) > 0)
                    Min = dataTypes.ReadNextLong(packetData);
                if ((Flags & 0x02) > 0)
                    Max = dataTypes.ReadNextLong(packetData);
            }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 1;
            }

            public override string GetName()
            {
                return "brigadier:long";
            }
        }

        internal class PaserString : Paser
        {
            private StringType Type;

            private enum StringType { SINGLE_WORD, QUOTABLE_PHRASE, GREEDY_PHRASE };

            public PaserString(DataTypes dataTypes, PacketStream packetData)
            {
                Type = (StringType)dataTypes.ReadNextVarInt(packetData);
            }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 1;
            }

            public override string GetName()
            {
                return "brigadier:string";
            }
        }

        internal class PaserEntity : Paser
        {
            private byte Flags;

            public PaserEntity(DataTypes dataTypes, PacketStream packetData)
            {
                Flags = dataTypes.ReadNextByte(packetData);
            }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 1;
            }

            public override string GetName()
            {
                return "minecraft:entity";
            }
        }

        internal class PaserBlockPos : Paser
        {

            public PaserBlockPos(DataTypes dataTypes, PacketStream packetData) { }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 3;
            }

            public override string GetName()
            {
                return "minecraft:block_pos";
            }
        }

        internal class PaserColumnPos : Paser
        {

            public PaserColumnPos(DataTypes dataTypes, PacketStream packetData) { }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 3;
            }

            public override string GetName()
            {
                return "minecraft:column_pos";
            }
        }

        internal class PaserVec3 : Paser
        {

            public PaserVec3(DataTypes dataTypes, PacketStream packetData) { }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 3;
            }

            public override string GetName()
            {
                return "minecraft:vec3";
            }
        }

        internal class PaserVec2 : Paser
        {

            public PaserVec2(DataTypes dataTypes, PacketStream packetData) { }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 2;
            }

            public override string GetName()
            {
                return "minecraft:vec2";
            }
        }

        internal class PaserRotation : Paser
        {

            public PaserRotation(DataTypes dataTypes, PacketStream packetData) { }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 2;
            }

            public override string GetName()
            {
                return "minecraft:rotation";
            }
        }

        internal class PaserMessage : Paser
        {
            public PaserMessage(DataTypes dataTypes, PacketStream packetData) { }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 1;
            }

            public override string GetName()
            {
                return "minecraft:message";
            }
        }

        internal class PaserScoreHolder : Paser
        {
            private byte Flags;

            public PaserScoreHolder(DataTypes dataTypes, PacketStream packetData)
            {
                Flags = dataTypes.ReadNextByte(packetData);
            }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 1;
            }

            public override string GetName()
            {
                return "minecraft:score_holder";
            }
        }

        internal class PaserRange : Paser
        {
            private bool Decimals;

            public PaserRange(DataTypes dataTypes, PacketStream packetData)
            {
                Decimals = dataTypes.ReadNextBool(packetData);
            }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 1;
            }

            public override string GetName()
            {
                return "minecraft:range";
            }
        }

        internal class PaserResourceOrTag : Paser
        {
            private string Registry;

            public PaserResourceOrTag(DataTypes dataTypes, PacketStream packetData)
            {
                var task = dataTypes.ReadNextStringAsync(packetData);
                task.Wait();
                Registry = task.Result;
            }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 1;
            }

            public override string GetName()
            {
                return "minecraft:resource_or_tag";
            }
        }

        internal class PaserResource : Paser
        {
            private string Registry;

            public PaserResource(DataTypes dataTypes, PacketStream packetData)
            {
                var task = dataTypes.ReadNextStringAsync(packetData);
                task.Wait();
                Registry = task.Result;
            }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 1;
            }

            public override string GetName()
            {
                return "minecraft:resource";
            }
        }
    }
}
