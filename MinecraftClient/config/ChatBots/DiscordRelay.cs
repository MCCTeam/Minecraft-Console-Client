//MCCScript 1.0
MCC.LoadBot(new DiscordRelay());
//MCCScript Extensions
//dll websocket-server.dll
//dll simple-discord-api.dll
//using SimpleDiscordApi;

public class DiscordRelay : ChatBot
{
    /**
      MCC Discord Relaying Bot, Created by ReinforceZwei

      IMPORTANT!!!
      You are required to download two DLLs library from:

      https://github.com/ReinforceZwei/Minecraft-Console-Client/releases/tag/v0.0.1-beta

      And place them together with your MinecraftClient.exe before loading this bot
     */
    
    // Fill in the below information
    private readonly string Token = "";
    private readonly string ChannelId = ""; // Which channel should the ingame message be sent to
    private readonly ulong BotOwnerId = 0;  // Only owner can issue bot command

    private readonly string CommandPrefix = "$";

    private DiscordGatewayApi dcc;

    public override void Initialize()
    {
        DiscordGatewayApi.SetDebugMode(false);
        dcc = new DiscordGatewayApi(Token);
        dcc.MessageCreate += (s, e) => 
        {
            if (!e.Message.Author.IsBot)
            {
                string msg = e.Message.Content;
                if (e.Message.Author.Id == BotOwnerId)
                {
                    if (msg.StartsWith(CommandPrefix))
                    {
                        string result = "";
                        msg = msg.Substring(CommandPrefix.Length); // Remove command prefix
                        PerformInternalCommand(msg, ref result);
                        if (!string.IsNullOrEmpty(result))
                            e.Message.Reply("```" + result + "```");
                    }
                }
            }
        };
        dcc.Ready += (s, e) => 
        {
            LogToConsole("Discord gateway connected");
        };
        dcc.GatewayDisconnect += (s, e) =>
        {
            LogToConsole("Discord gateway disconnected");
        };
        if (!string.IsNullOrEmpty(Token)
            && !string.IsNullOrEmpty(ChannelId)
            && BotOwnerId != 0)
        {
            dcc.Connect();
            dcc.Identify();
        }
        else
        {
            LogToConsole("Please fill in all infomation");
            UnloadBot();
        }
    }

    public override void GetText(string text)
    {
        // Relay to Discord
        text = GetVerbatim(text);
        DiscordRestApi.ChannelCreateMessage(Token, ChannelId, text);
    }
}