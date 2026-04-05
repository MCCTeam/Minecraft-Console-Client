using System;
using System.Collections.Generic;
using MinecraftClient.Protocol.PacketPipeline;

namespace MinecraftClient.Protocol.Handlers.packet.s2c
{
    internal static class DeclareCommands
    {
        private const byte NodeTypeMask = 0x03;
        private const byte NodeExecutableFlag = 0x04;
        private const byte NodeRedirectFlag = 0x08;
        private const byte NodeCustomSuggestionsFlag = 0x10;
        private const byte NodeRestrictedFlag = 0x20;

        private static readonly Dictionary<string, ArgumentTypeLayout> s_argumentTypeCatalog = CreateArgumentTypeCatalog();
        private static readonly ArgumentTypeLayout s_unknownLegacyArgumentType = new("minecraft:unknown");

        // Generated from tools/gen_command_argument_registry.py with IDE-only registrations excluded.
        private static readonly string[] s_modernArgumentTypes1206 =
        [
            "brigadier:bool", "brigadier:float", "brigadier:double", "brigadier:integer", "brigadier:long", "brigadier:string",
            "entity", "game_profile", "block_pos", "column_pos", "vec3", "vec2", "block_state", "block_predicate",
            "item_stack", "item_predicate", "color", "component", "style", "message", "nbt_compound_tag", "nbt_tag",
            "nbt_path", "objective", "objective_criteria", "operation", "particle", "angle", "rotation",
            "scoreboard_slot", "score_holder", "swizzle", "team", "item_slot", "item_slots", "resource_location",
            "function", "entity_anchor", "int_range", "float_range", "dimension", "gamemode", "time",
            "resource_or_tag", "resource_or_tag_key", "resource", "resource_key", "template_mirror",
            "template_rotation", "heightmap", "loot_table", "loot_predicate", "loot_modifier", "uuid"
        ];

        private static readonly string[] s_modernArgumentTypes1215 =
        [
            "brigadier:bool", "brigadier:float", "brigadier:double", "brigadier:integer", "brigadier:long", "brigadier:string",
            "entity", "game_profile", "block_pos", "column_pos", "vec3", "vec2", "block_state", "block_predicate",
            "item_stack", "item_predicate", "color", "component", "style", "message", "nbt_compound_tag", "nbt_tag",
            "nbt_path", "objective", "objective_criteria", "operation", "particle", "angle", "rotation",
            "scoreboard_slot", "score_holder", "swizzle", "team", "item_slot", "item_slots", "resource_location",
            "function", "entity_anchor", "int_range", "float_range", "dimension", "gamemode", "time",
            "resource_or_tag", "resource_or_tag_key", "resource", "resource_key", "resource_selector",
            "template_mirror", "template_rotation", "heightmap", "loot_table", "loot_predicate", "loot_modifier", "uuid"
        ];

        private static readonly string[] s_modernArgumentTypes1216 =
        [
            "brigadier:bool", "brigadier:float", "brigadier:double", "brigadier:integer", "brigadier:long", "brigadier:string",
            "entity", "game_profile", "block_pos", "column_pos", "vec3", "vec2", "block_state", "block_predicate",
            "item_stack", "item_predicate", "color", "hex_color", "component", "style", "message",
            "nbt_compound_tag", "nbt_tag", "nbt_path", "objective", "objective_criteria", "operation", "particle",
            "angle", "rotation", "scoreboard_slot", "score_holder", "swizzle", "team", "item_slot", "item_slots",
            "resource_location", "function", "entity_anchor", "int_range", "float_range", "dimension", "gamemode",
            "time", "resource_or_tag", "resource_or_tag_key", "resource", "resource_key", "resource_selector",
            "template_mirror", "template_rotation", "heightmap", "loot_table", "loot_predicate", "loot_modifier",
            "dialog", "uuid"
        ];

        private static int RootIdx = -1;
        private static CommandNode[] Nodes = Array.Empty<CommandNode>();
        private static bool HasLoadedTree;
        internal static string? LastReadError { get; private set; }

        public static bool IsCommandTreeAvailable => HasValidCommandTree();

        public static void Read(DataTypes dataTypes, PacketReader packetData, int protocolVersion)
        {
            Reset();
            ConsoleIO.OnDeclareMinecraftCommand(Array.Empty<string>());

            try
            {
                ReadCommandTree(dataTypes, packetData, protocolVersion);
            }
            catch (Exception ex)
            {
                LastReadError = ex.ToString();
                Reset();
            }

            ConsoleIO.OnDeclareMinecraftCommand(HasLoadedTree ? ExtractRootCommand() : Array.Empty<string>());
        }

        public static List<Tuple<string, string>> CollectSignArguments(string command)
        {
            List<Tuple<string, string>> needSigned = new();
            if (!HasValidCommandTree() || string.IsNullOrEmpty(command))
                return needSigned;

            return TryMatchNode(RootIdx, command, 0, needSigned, out List<Tuple<string, string>> matchedArguments)
                ? matchedArguments
                : [];
        }

        private static void ReadCommandTree(DataTypes dataTypes, PacketReader packetData, int protocolVersion)
        {
            int count = dataTypes.ReadNextVarInt(packetData);
            Nodes = new CommandNode[count];

            for (int i = 0; i < count; ++i)
            {
                byte flags = dataTypes.ReadNextByte(packetData);
                int[] children = ReadChildIndices(dataTypes, packetData);
                int redirectNode = (flags & NodeRedirectFlag) != 0 ? dataTypes.ReadNextVarInt(packetData) : -1;

                CommandNodeKind nodeKind = (CommandNodeKind)(flags & NodeTypeMask);
                CommandNode node = nodeKind switch
                {
                    CommandNodeKind.Root => new(flags, children, redirectNode),
                    CommandNodeKind.Literal => new(flags, children, redirectNode, dataTypes.ReadNextString(packetData)),
                    CommandNodeKind.Argument => ReadArgumentNode(dataTypes, packetData, protocolVersion, flags, children, redirectNode),
                    _ => throw new InvalidOperationException($"Unsupported DeclareCommands node type {(byte)nodeKind}.")
                };

                Nodes[i] = node;
            }

            RootIdx = dataTypes.ReadNextVarInt(packetData);
            HasLoadedTree = IsValidNodeIndex(RootIdx);
        }

        private static CommandNode ReadArgumentNode(
            DataTypes dataTypes,
            PacketReader packetData,
            int protocolVersion,
            byte flags,
            int[] children,
            int redirectNode)
        {
            string name = dataTypes.ReadNextString(packetData);
            int parserId = dataTypes.ReadNextVarInt(packetData);

            if (!TryResolveArgumentTypeLayout(protocolVersion, parserId, out ArgumentTypeLayout layout))
                throw new InvalidOperationException($"Unsupported DeclareCommands argument type id {parserId} for protocol {protocolVersion}.");

            CommandArgumentDescriptor descriptor = ReadArgumentDescriptor(dataTypes, packetData, layout);
            string? suggestionsType = (flags & NodeCustomSuggestionsFlag) != 0 ? dataTypes.ReadNextString(packetData) : null;

            return new(flags, children, redirectNode, name, descriptor, suggestionsType, parserId);
        }

        private static int[] ReadChildIndices(DataTypes dataTypes, PacketReader packetData)
        {
            int childCount = dataTypes.ReadNextVarInt(packetData);
            int[] children = new int[childCount];

            for (int i = 0; i < childCount; ++i)
                children[i] = dataTypes.ReadNextVarInt(packetData);

            return children;
        }

        private static CommandArgumentDescriptor ReadArgumentDescriptor(DataTypes dataTypes, PacketReader packetData, ArgumentTypeLayout layout)
        {
            switch (layout.PayloadKind)
            {
                case ArgumentPayloadKind.None:
                    return layout.CreateDescriptor();
                case ArgumentPayloadKind.BrigadierFloat:
                    ReadNumberBounds<float>(dataTypes, packetData, static (types, data) => types.ReadNextFloat(data));
                    return layout.CreateDescriptor();
                case ArgumentPayloadKind.BrigadierDouble:
                    ReadNumberBounds<double>(dataTypes, packetData, static (types, data) => types.ReadNextDouble(data));
                    return layout.CreateDescriptor();
                case ArgumentPayloadKind.BrigadierInteger:
                    ReadNumberBounds<int>(dataTypes, packetData, static (types, data) => types.ReadNextInt(data));
                    return layout.CreateDescriptor();
                case ArgumentPayloadKind.BrigadierLong:
                    ReadNumberBounds<long>(dataTypes, packetData, static (types, data) => types.ReadNextLong(data));
                    return layout.CreateDescriptor();
                case ArgumentPayloadKind.BrigadierString:
                    ArgumentConsumption consumption = dataTypes.ReadNextVarInt(packetData) switch
                    {
                        0 => ArgumentConsumption.SingleToken,
                        1 => ArgumentConsumption.QuotedStringOrWord,
                        2 => ArgumentConsumption.GreedyTail,
                        int stringType => throw new InvalidOperationException($"Unsupported brigadier:string type {stringType}.")
                    };
                    return layout.CreateDescriptor(consumption);
                case ArgumentPayloadKind.Entity:
                case ArgumentPayloadKind.ScoreHolder:
                    dataTypes.ReadNextByte(packetData);
                    return layout.CreateDescriptor();
                case ArgumentPayloadKind.Time:
                    dataTypes.ReadNextInt(packetData);
                    return layout.CreateDescriptor();
                case ArgumentPayloadKind.RegistryKey:
                case ArgumentPayloadKind.ForgeEnum:
                    dataTypes.ReadNextString(packetData);
                    return layout.CreateDescriptor();
                default:
                    throw new InvalidOperationException($"Unsupported DeclareCommands payload kind {layout.PayloadKind}.");
            }
        }

        private static void ReadNumberBounds<TValue>(DataTypes dataTypes, PacketReader packetData, Func<DataTypes, PacketReader, TValue> readValue)
        {
            byte flags = dataTypes.ReadNextByte(packetData);
            if ((flags & 0x01) != 0)
                _ = readValue(dataTypes, packetData);
            if ((flags & 0x02) != 0)
                _ = readValue(dataTypes, packetData);
        }

        private static string[] ExtractRootCommand()
        {
            if (!HasValidCommandTree())
                return Array.Empty<string>();

            List<string> commands = new();
            CommandNode root = Nodes[RootIdx];

            foreach (int child in root.Children)
            {
                if (!IsValidNodeIndex(child))
                    continue;

                string? childName = Nodes[child].Name;
                if (!string.IsNullOrEmpty(childName))
                    commands.Add(childName);
            }

            return commands.ToArray();
        }

        private static bool TryMatchNode(
            int nodeIdx,
            string command,
            int position,
            List<Tuple<string, string>> signedArguments,
            out List<Tuple<string, string>> matchedArguments)
        {
            matchedArguments = signedArguments;
            if (!IsValidNodeIndex(nodeIdx))
                return false;

            CommandNode node = Nodes[nodeIdx];
            if (!TryConsumeNode(node, command, position, out int nextPosition, out Tuple<string, string>? signedCapture))
                return false;

            List<Tuple<string, string>> currentArguments = signedArguments;
            if (signedCapture is not null)
            {
                currentArguments = new List<Tuple<string, string>>(signedArguments.Count + 1);
                currentArguments.AddRange(signedArguments);
                currentArguments.Add(signedCapture);
            }

            int traversalNodeIdx = ResolveRedirect(nodeIdx);
            bool canStopHere = node.IsExecutable ||
                               (traversalNodeIdx != nodeIdx && IsValidNodeIndex(traversalNodeIdx) && Nodes[traversalNodeIdx].IsExecutable);

            if (nextPosition == command.Length)
            {
                if (canStopHere)
                {
                    matchedArguments = currentArguments;
                    return true;
                }

                return false;
            }

            int childPosition = nextPosition;
            if (node.Kind != CommandNodeKind.Root)
            {
                if (command[childPosition] != ' ')
                    return false;
                childPosition++;
            }

            return TryMatchChildren(traversalNodeIdx, command, childPosition, currentArguments, out matchedArguments);
        }

        private static bool TryMatchChildren(
            int nodeIdx,
            string command,
            int position,
            List<Tuple<string, string>> signedArguments,
            out List<Tuple<string, string>> matchedArguments)
        {
            matchedArguments = signedArguments;
            if (!IsValidNodeIndex(nodeIdx))
                return false;

            int[] children = Nodes[nodeIdx].Children;

            for (int pass = 0; pass < 2; ++pass)
            {
                foreach (int childIdx in children)
                {
                    if (!IsValidNodeIndex(childIdx))
                        continue;

                    bool isLiteral = Nodes[childIdx].Kind == CommandNodeKind.Literal;
                    if ((pass == 0 && !isLiteral) || (pass == 1 && isLiteral))
                        continue;

                    if (TryMatchNode(childIdx, command, position, signedArguments, out matchedArguments))
                        return true;
                }
            }

            return false;
        }

        private static bool TryConsumeNode(
            CommandNode node,
            string command,
            int position,
            out int nextPosition,
            out Tuple<string, string>? signedCapture)
        {
            nextPosition = position;
            signedCapture = null;

            switch (node.Kind)
            {
                case CommandNodeKind.Root:
                    return true;
                case CommandNodeKind.Literal:
                    return TryConsumeLiteral(command, position, node.Name!, out nextPosition);
                case CommandNodeKind.Argument:
                    if (node.Argument is null || !TryConsumeArgument(command, position, node.Argument.Value, out nextPosition))
                        return false;

                    if (node.Argument.Value.IsSigned)
                        signedCapture = new Tuple<string, string>(node.Name!, command[position..nextPosition]);

                    return true;
                default:
                    return false;
            }
        }

        private static bool TryConsumeLiteral(string command, int position, string literal, out int nextPosition)
        {
            nextPosition = position;
            if (position + literal.Length > command.Length)
                return false;

            if (string.CompareOrdinal(command, position, literal, 0, literal.Length) != 0)
                return false;

            nextPosition = position + literal.Length;
            return nextPosition == command.Length || command[nextPosition] == ' ';
        }

        private static bool TryConsumeArgument(string command, int position, CommandArgumentDescriptor descriptor, out int nextPosition)
        {
            nextPosition = position;

            return descriptor.Consumption switch
            {
                ArgumentConsumption.SingleToken => TryConsumeSingleToken(command, position, out nextPosition),
                ArgumentConsumption.QuotedStringOrWord => TryConsumeQuotedStringOrWord(command, position, out nextPosition),
                ArgumentConsumption.GreedyTail => TryConsumeGreedyTail(command, position, out nextPosition),
                ArgumentConsumption.FixedTokenCount => TryConsumeFixedTokenCount(command, position, descriptor.TokenCount, out nextPosition),
                _ => false
            };
        }

        private static bool TryConsumeSingleToken(string command, int position, out int nextPosition)
        {
            nextPosition = position;
            if (position >= command.Length)
                return false;

            int cursor = position;
            while (cursor < command.Length && command[cursor] != ' ')
                cursor++;

            nextPosition = cursor;
            return cursor > position;
        }

        private static bool TryConsumeQuotedStringOrWord(string command, int position, out int nextPosition)
        {
            nextPosition = position;
            if (position >= command.Length)
                return false;

            if (command[position] != '"')
                return TryConsumeSingleToken(command, position, out nextPosition);

            bool escaped = false;
            for (int cursor = position + 1; cursor < command.Length; ++cursor)
            {
                char current = command[cursor];
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (current == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (current == '"')
                {
                    nextPosition = cursor + 1;
                    return nextPosition == command.Length || command[nextPosition] == ' ';
                }
            }

            return false;
        }

        private static bool TryConsumeGreedyTail(string command, int position, out int nextPosition)
        {
            nextPosition = command.Length;
            return position < command.Length;
        }

        private static bool TryConsumeFixedTokenCount(string command, int position, int tokenCount, out int nextPosition)
        {
            nextPosition = position;
            int cursor = position;

            for (int i = 0; i < tokenCount; ++i)
            {
                if (!TryConsumeSingleToken(command, cursor, out int tokenEnd))
                    return false;

                cursor = tokenEnd;
                if (i < tokenCount - 1)
                {
                    if (cursor >= command.Length || command[cursor] != ' ')
                        return false;

                    cursor++;
                }
            }

            nextPosition = cursor;
            return true;
        }

        private static int ResolveRedirect(int nodeIdx)
        {
            if (!IsValidNodeIndex(nodeIdx))
                return -1;

            HashSet<int> visited = new();
            int current = nodeIdx;

            while (IsValidNodeIndex(current) && Nodes[current].RedirectNode >= 0)
            {
                if (!visited.Add(current))
                    return current;

                current = Nodes[current].RedirectNode;
            }

            return IsValidNodeIndex(current) ? current : -1;
        }

        private static bool TryResolveArgumentTypeLayout(int protocolVersion, int parserId, out ArgumentTypeLayout layout)
        {
            return protocolVersion >= Protocol18Handler.MC_1_20_6_Version
                ? TryResolveModernArgumentTypeLayout(protocolVersion, parserId, out layout)
                : TryResolveLegacyArgumentTypeLayout(protocolVersion, parserId, out layout);
        }

        private static bool TryResolveModernArgumentTypeLayout(int protocolVersion, int parserId, out ArgumentTypeLayout layout)
        {
            string[] registry = protocolVersion switch
            {
                >= Protocol18Handler.MC_1_21_6_Version => s_modernArgumentTypes1216,
                >= Protocol18Handler.MC_1_21_5_Version => s_modernArgumentTypes1215,
                _ => s_modernArgumentTypes1206
            };

            if (parserId < 0 || parserId >= registry.Length)
            {
                layout = default;
                return false;
            }

            return s_argumentTypeCatalog.TryGetValue(ToCanonicalArgumentTypeName(registry[parserId]), out layout);
        }

        private static bool TryResolveLegacyArgumentTypeLayout(int protocolVersion, int parserId, out ArgumentTypeLayout layout)
        {
            string? name;

            if (protocolVersion <= Protocol18Handler.MC_1_19_2_Version)
            {
                name = parserId switch
                {
                    1 => "brigadier:float",
                    2 => "brigadier:double",
                    3 => "brigadier:integer",
                    4 => "brigadier:long",
                    5 => "brigadier:string",
                    6 => "minecraft:entity",
                    8 => "minecraft:block_pos",
                    9 => "minecraft:column_pos",
                    10 => "minecraft:vec3",
                    11 => "minecraft:vec2",
                    18 => "minecraft:message",
                    27 => "minecraft:rotation",
                    29 => "minecraft:score_holder",
                    43 => "minecraft:resource_or_tag",
                    44 => "minecraft:resource",
                    50 => "forge:enum",
                    _ => null
                };
            }
            else if (protocolVersion <= Protocol18Handler.MC_1_19_3_Version)
            {
                name = parserId switch
                {
                    1 => "brigadier:float",
                    2 => "brigadier:double",
                    3 => "brigadier:integer",
                    4 => "brigadier:long",
                    5 => "brigadier:string",
                    6 => "minecraft:entity",
                    8 => "minecraft:block_pos",
                    9 => "minecraft:column_pos",
                    10 => "minecraft:vec3",
                    11 => "minecraft:vec2",
                    18 => "minecraft:message",
                    27 => "minecraft:rotation",
                    29 => "minecraft:score_holder",
                    41 => "minecraft:resource_or_tag",
                    42 => "minecraft:resource_or_tag_key",
                    43 => "minecraft:resource",
                    44 => "minecraft:resource_key",
                    50 => "forge:enum",
                    _ => null
                };
            }
            else if (protocolVersion <= Protocol18Handler.MC_1_20_2_Version)
            {
                name = parserId switch
                {
                    1 => "brigadier:float",
                    2 => "brigadier:double",
                    3 => "brigadier:integer",
                    4 => "brigadier:long",
                    5 => "brigadier:string",
                    6 => "minecraft:entity",
                    8 => "minecraft:block_pos",
                    9 => "minecraft:column_pos",
                    10 => "minecraft:vec3",
                    11 => "minecraft:vec2",
                    18 => "minecraft:message",
                    27 => "minecraft:rotation",
                    29 => "minecraft:score_holder",
                    40 => "minecraft:time",
                    41 => "minecraft:resource_or_tag",
                    42 => "minecraft:resource_or_tag_key",
                    43 => "minecraft:resource",
                    44 => "minecraft:resource_key",
                    50 when protocolVersion == Protocol18Handler.MC_1_19_4_Version => "forge:enum",
                    51 when protocolVersion is >= Protocol18Handler.MC_1_20_Version and <= Protocol18Handler.MC_1_20_2_Version => "forge:enum",
                    _ => null
                };
            }
            else
            {
                name = parserId switch
                {
                    1 => "brigadier:float",
                    2 => "brigadier:double",
                    3 => "brigadier:integer",
                    4 => "brigadier:long",
                    5 => "brigadier:string",
                    6 => "minecraft:entity",
                    8 => "minecraft:block_pos",
                    9 => "minecraft:column_pos",
                    10 => "minecraft:vec3",
                    11 => "minecraft:vec2",
                    18 or 19 => "minecraft:message",
                    27 => "minecraft:rotation",
                    30 => "minecraft:score_holder",
                    41 => "minecraft:time",
                    42 => "minecraft:resource_or_tag",
                    43 => "minecraft:resource_or_tag_key",
                    44 => "minecraft:resource",
                    45 => "minecraft:resource_key",
                    52 => "forge:enum",
                    _ => null
                };
            }

            if (name is null)
            {
                layout = s_unknownLegacyArgumentType;
                return true;
            }

            return s_argumentTypeCatalog.TryGetValue(name, out layout);
        }

        private static string ToCanonicalArgumentTypeName(string rawName)
        {
            return rawName.Contains(':', StringComparison.Ordinal) ? rawName : "minecraft:" + rawName;
        }

        private static void Reset()
        {
            RootIdx = -1;
            Nodes = Array.Empty<CommandNode>();
            HasLoadedTree = false;
            LastReadError = null;
        }

        private static bool HasValidCommandTree()
        {
            return HasLoadedTree && IsValidNodeIndex(RootIdx);
        }

        private static bool IsValidNodeIndex(int nodeIdx)
        {
            return nodeIdx >= 0 && nodeIdx < Nodes.Length;
        }

        private static Dictionary<string, ArgumentTypeLayout> CreateArgumentTypeCatalog()
        {
            Dictionary<string, ArgumentTypeLayout> catalog = new(StringComparer.Ordinal);

            static void Add(
                Dictionary<string, ArgumentTypeLayout> items,
                string name,
                ArgumentPayloadKind payloadKind = ArgumentPayloadKind.None,
                ArgumentConsumption consumption = ArgumentConsumption.SingleToken,
                int tokenCount = 1,
                bool isSigned = false)
            {
                items[name] = new ArgumentTypeLayout(name, payloadKind, consumption, tokenCount, isSigned);
            }

            static void AddFixedTokens(Dictionary<string, ArgumentTypeLayout> items, string name, int tokenCount)
            {
                Add(items, name, consumption: ArgumentConsumption.FixedTokenCount, tokenCount: tokenCount);
            }

            Add(catalog, "brigadier:bool");
            Add(catalog, "brigadier:float", ArgumentPayloadKind.BrigadierFloat);
            Add(catalog, "brigadier:double", ArgumentPayloadKind.BrigadierDouble);
            Add(catalog, "brigadier:integer", ArgumentPayloadKind.BrigadierInteger);
            Add(catalog, "brigadier:long", ArgumentPayloadKind.BrigadierLong);
            Add(catalog, "brigadier:string", ArgumentPayloadKind.BrigadierString);
            Add(catalog, "minecraft:entity", ArgumentPayloadKind.Entity);
            Add(catalog, "minecraft:game_profile");
            AddFixedTokens(catalog, "minecraft:block_pos", 3);
            AddFixedTokens(catalog, "minecraft:column_pos", 2);
            AddFixedTokens(catalog, "minecraft:vec3", 3);
            AddFixedTokens(catalog, "minecraft:vec2", 2);
            Add(catalog, "minecraft:block_state");
            Add(catalog, "minecraft:block_predicate");
            Add(catalog, "minecraft:item_stack");
            Add(catalog, "minecraft:item_predicate");
            Add(catalog, "minecraft:color");
            Add(catalog, "minecraft:hex_color");
            Add(catalog, "minecraft:component");
            Add(catalog, "minecraft:style");
            Add(catalog, "minecraft:message", consumption: ArgumentConsumption.GreedyTail, isSigned: true);
            Add(catalog, "minecraft:nbt_compound_tag");
            Add(catalog, "minecraft:nbt_tag");
            Add(catalog, "minecraft:nbt_path");
            Add(catalog, "minecraft:objective");
            Add(catalog, "minecraft:objective_criteria");
            Add(catalog, "minecraft:operation");
            Add(catalog, "minecraft:particle");
            Add(catalog, "minecraft:angle");
            AddFixedTokens(catalog, "minecraft:rotation", 2);
            Add(catalog, "minecraft:scoreboard_slot");
            Add(catalog, "minecraft:score_holder", ArgumentPayloadKind.ScoreHolder);
            Add(catalog, "minecraft:swizzle");
            Add(catalog, "minecraft:team");
            Add(catalog, "minecraft:item_slot");
            Add(catalog, "minecraft:item_slots");
            Add(catalog, "minecraft:resource_location");
            Add(catalog, "minecraft:function");
            Add(catalog, "minecraft:entity_anchor");
            Add(catalog, "minecraft:int_range");
            Add(catalog, "minecraft:float_range");
            Add(catalog, "minecraft:dimension");
            Add(catalog, "minecraft:gamemode");
            Add(catalog, "minecraft:time", ArgumentPayloadKind.Time);
            Add(catalog, "minecraft:resource_or_tag", ArgumentPayloadKind.RegistryKey);
            Add(catalog, "minecraft:resource_or_tag_key", ArgumentPayloadKind.RegistryKey);
            Add(catalog, "minecraft:resource", ArgumentPayloadKind.RegistryKey);
            Add(catalog, "minecraft:resource_key", ArgumentPayloadKind.RegistryKey);
            Add(catalog, "minecraft:resource_selector", ArgumentPayloadKind.RegistryKey);
            Add(catalog, "minecraft:template_mirror");
            Add(catalog, "minecraft:template_rotation");
            Add(catalog, "minecraft:heightmap");
            Add(catalog, "minecraft:loot_table");
            Add(catalog, "minecraft:loot_predicate");
            Add(catalog, "minecraft:loot_modifier");
            Add(catalog, "minecraft:dialog");
            Add(catalog, "minecraft:uuid");
            Add(catalog, "forge:enum", ArgumentPayloadKind.ForgeEnum);

            return catalog;
        }

        private enum CommandNodeKind : byte
        {
            Root = 0,
            Literal = 1,
            Argument = 2
        }

        private enum ArgumentConsumption
        {
            SingleToken,
            QuotedStringOrWord,
            GreedyTail,
            FixedTokenCount
        }

        private enum ArgumentPayloadKind
        {
            None,
            BrigadierFloat,
            BrigadierDouble,
            BrigadierInteger,
            BrigadierLong,
            BrigadierString,
            Entity,
            ScoreHolder,
            Time,
            RegistryKey,
            ForgeEnum
        }

        private sealed record CommandNode(
            byte Flags,
            int[] Children,
            int RedirectNode = -1,
            string? Name = null,
            CommandArgumentDescriptor? Argument = null,
            string? SuggestionsType = null,
            int ParserId = -1)
        {
            public CommandNodeKind Kind => (CommandNodeKind)(Flags & NodeTypeMask);
            public bool IsExecutable => (Flags & NodeExecutableFlag) != 0;
            public bool IsRestricted => (Flags & NodeRestrictedFlag) != 0;
        }

        private readonly record struct CommandArgumentDescriptor(
            string Name,
            ArgumentConsumption Consumption,
            int TokenCount = 1,
            bool IsSigned = false);

        private readonly struct ArgumentTypeLayout
        {
            public string Name { get; }
            public ArgumentPayloadKind PayloadKind { get; }
            public ArgumentConsumption Consumption { get; }
            public int TokenCount { get; }
            public bool IsSigned { get; }

            public ArgumentTypeLayout(
                string name,
                ArgumentPayloadKind payloadKind = ArgumentPayloadKind.None,
                ArgumentConsumption consumption = ArgumentConsumption.SingleToken,
                int tokenCount = 1,
                bool isSigned = false)
            {
                Name = name;
                PayloadKind = payloadKind;
                Consumption = consumption;
                TokenCount = tokenCount;
                IsSigned = isSigned;
            }

            public CommandArgumentDescriptor CreateDescriptor()
            {
                return new CommandArgumentDescriptor(Name, Consumption, TokenCount, IsSigned);
            }

            public CommandArgumentDescriptor CreateDescriptor(ArgumentConsumption consumption)
            {
                return new CommandArgumentDescriptor(Name, consumption, TokenCount, IsSigned);
            }
        }
    }
}
