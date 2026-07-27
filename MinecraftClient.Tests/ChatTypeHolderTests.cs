using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.Tests;

public sealed class ChatTypeHolderTests
{
    [Fact]
    public void ReferenceHolderUsesOneBasedWireIdIn121AndNewer()
    {
        var dataTypes = new DataTypes(Protocol18Handler.MC_1_21_Version);
        var packetData = new Queue<byte>(DataTypes.GetVarInt(2));

        int chatTypeId = ChatParser.ReadChatTypeHolder(
            dataTypes,
            packetData,
            Protocol18Handler.MC_1_21_Version,
            out var directDecoration);

        Assert.Equal(1, chatTypeId);
        Assert.Null(directDecoration);
        Assert.Empty(packetData);
    }

    [Fact]
    public void RegistryIdRemainsUnchangedBefore121()
    {
        var dataTypes = new DataTypes(Protocol18Handler.MC_1_20_6_Version);
        var packetData = new Queue<byte>(DataTypes.GetVarInt(2));

        int chatTypeId = ChatParser.ReadChatTypeHolder(
            dataTypes,
            packetData,
            Protocol18Handler.MC_1_20_6_Version,
            out var directDecoration);

        Assert.Equal(2, chatTypeId);
        Assert.Null(directDecoration);
        Assert.Empty(packetData);
    }

    [Fact]
    public void DirectHolderConsumesChatAndNarrationDecorations()
    {
        var dataTypes = new DataTypes(Protocol18Handler.MC_1_21_Version);
        var packetBytes = new List<byte>();
        packetBytes.AddRange(DataTypes.GetVarInt(0));
        AddDecoration(packetBytes, dataTypes, "chat.type.text", 0, 2);
        AddDecoration(packetBytes, dataTypes, "chat.type.text.narrate", 0, 2);
        var packetData = new Queue<byte>(packetBytes);

        int chatTypeId = ChatParser.ReadChatTypeHolder(
            dataTypes,
            packetData,
            Protocol18Handler.MC_1_21_Version,
            out var directDecoration);

        Assert.Equal(-1, chatTypeId);
        Assert.NotNull(directDecoration);
        Assert.Equal("chat.type.text", directDecoration.TranslationKey);
        Assert.Equal(
            [ChatParser.ChatTypeParameter.Sender, ChatParser.ChatTypeParameter.Content],
            directDecoration.Parameters);
        Assert.Empty(packetData);
    }

    [Fact]
    public void RegistryDecorationControlsParameterSelectionAndOrdering()
    {
        Dictionary<int, ChatParser.MessageType>? originalChatTypes = ChatParser.ChatId2Type;
        try
        {
            ChatParser.ClearChatTypeDecorations();
            ChatParser.ChatId2Type = [];
            var chatTypeData = new Dictionary<string, object>
            {
                ["chat"] = new Dictionary<string, object>
                {
                    ["translation_key"] = "commands.message.display.outgoing",
                    ["parameters"] = new object[] { "target", "content" }
                }
            };
            ChatParser.ReadChatType(42, "example:custom_chat", chatTypeData);
            var message = new ChatMessage(
                "hello",
                false,
                42,
                Guid.Empty,
                null,
                "Alice",
                "Bob",
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                null,
                false);

            string rendered = ChatParser.ParseSignedChat(message);

            Assert.Equal("You whisper to Bob: hello", rendered);
        }
        finally
        {
            ChatParser.ClearChatTypeDecorations();
            ChatParser.ChatId2Type = originalChatTypes;
        }
    }

    [Fact]
    public void UnknownTranslationKeyIsUsedAsVanillaFormatPattern()
    {
        Dictionary<int, ChatParser.MessageType>? originalChatTypes = ChatParser.ChatId2Type;
        try
        {
            ChatParser.ClearChatTypeDecorations();
            ChatParser.ChatId2Type = [];
            var chatTypeData = new Dictionary<string, object>
            {
                ["chat"] = new Dictionary<string, object>
                {
                    ["translation_key"] = "%s",
                    ["parameters"] = new object[] { "sender", "content" }
                }
            };
            ChatParser.ReadChatType(42, "ordinary:custom_chat", chatTypeData);
            var message = new ChatMessage(
                "hello",
                false,
                42,
                Guid.Empty,
                null,
                "Alice » hello",
                null,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                null,
                false);

            string rendered = ChatParser.ParseSignedChat(message);

            Assert.Equal("Alice » hello", rendered);
        }
        finally
        {
            ChatParser.ClearChatTypeDecorations();
            ChatParser.ChatId2Type = originalChatTypes;
        }
    }

    private static void AddDecoration(
        List<byte> packetBytes,
        DataTypes dataTypes,
        string translationKey,
        params int[] parameters)
    {
        packetBytes.AddRange(dataTypes.GetString(translationKey));
        packetBytes.AddRange(DataTypes.GetVarInt(parameters.Length));
        foreach (int parameter in parameters)
            packetBytes.AddRange(DataTypes.GetVarInt(parameter));
        packetBytes.AddRange(dataTypes.GetNbtTag(new Dictionary<string, object>()));
    }
}
