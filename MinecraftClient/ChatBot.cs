using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;

namespace MinecraftClient
{
    ///
    /// Welcome to the Bot API file !
    /// The virtual class "ChatBot" contains anything you need for creating chat bots
    /// Inherit from this class while adding your bot class to the "ChatBots" folder.
    /// Override the methods you want for handling events: Initialize, Update, GetText.
    ///
    /// For testing your bot you can add it in McClient.cs (see comment at line ~199).
    /// Your bot will be loaded everytime MCC is started so that you can test/debug.
    ///
    /// Once your bot is fully written and tested, you can export it a standalone script.
    /// This way it can be loaded in newer MCC builds, without modifying MCC itself.
    /// See config/sample-script-with-chatbot.cs for a ChatBot script example.
    ///

    /// <summary>
    /// The virtual class containing anything you need for creating chat bots.
    /// </summary>
    public abstract class ChatBot
    {
        public enum DisconnectReason { InGameKick, LoginRejected, ConnectionLost, UserLogout };

        //Handler will be automatically set on bot loading, don't worry about this
        public void SetHandler(McClient handler) { this._handler = handler; }
        protected void SetMaster(ChatBot master) { this.master = master; }
        protected void LoadBot(ChatBot bot) { Handler.BotUnLoad(bot); Handler.BotLoad(bot); }
        protected List<ChatBot> GetLoadedChatBots() { return Handler.GetLoadedChatBots(); }
        protected void UnLoadBot(ChatBot bot) { Handler.BotUnLoad(bot); }
        private McClient _handler = null;
        private ChatBot master = null;
        private List<string> registeredPluginChannels = new List<String>();
        private McClient Handler
        {
            get
            {
                if (master != null)
                    return master.Handler;
                if (_handler != null)
                    return _handler;
                throw new InvalidOperationException(Translations.Get("exception.chatbot.init"));
            }
        }

        /* ================================================== */
        /*   Main methods to override for creating your bot   */
        /* ================================================== */

        /// <summary>
        /// Anything you want to initialize your bot, will be called on load by MinecraftCom
        /// This method is called only once, whereas AfterGameJoined() is called once per server join.
        ///
        /// NOTE: Chat messages cannot be sent at this point in the login process.
        /// If you want to send a message when the bot is loaded, use AfterGameJoined.
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// Called after the server has been joined successfully and chat messages are able to be sent.
        /// This method is called again after reconnecting to the server, whereas Initialize() is called only once.
        ///
        /// NOTE: This is not always right after joining the server - if the bot was loaded after logging
        /// in this is still called.
        /// </summary>
        public virtual void AfterGameJoined() { }

        /// <summary>
        /// Will be called every ~100ms (10fps) if loaded in MinecraftCom
        /// </summary>
        public virtual void Update() { }

        /// <summary>
        /// Any text sent by the server will be sent here by MinecraftCom
        /// </summary>
        /// <param name="text">Text from the server</param>
        public virtual void GetText(string text) { }

        /// <summary>
        /// Any text sent by the server will be sent here by MinecraftCom (extended variant)
        /// </summary>
        /// <remarks>
        /// You can use Json.ParseJson() to process the JSON string.
        /// </remarks>
        /// <param name="text">Text from the server</param>
        /// <param name="json">Raw JSON from the server. This parameter will be NULL on MC 1.5 or lower!</param>
        public virtual void GetText(string text, string json) { }
        
        /// <summary>
        /// Is called when the client has been disconnected fom the server
        /// </summary>
        /// <param name="reason">Disconnect Reason</param>
        /// <param name="message">Kick message, if any</param>
        /// <returns>Return TRUE if the client is about to restart</returns>
        public virtual bool OnDisconnect(DisconnectReason reason, string message) { return false; }

        /// <summary>
        /// Called when a plugin channel message is received.
        /// The given channel must have previously been registered with RegisterPluginChannel.
        /// This can be used to communicate with server mods or plugins.  See wiki.vg for more
        /// information about plugin channels: http://wiki.vg/Plugin_channel
        /// </summary>
        /// <param name="channel">The name of the channel</param>
        /// <param name="data">The payload for the message</param>
        public virtual void OnPluginMessage(string channel, byte[] data) { }

        /// <summary>
        /// Called when properties for the Player entity are received from the server
        /// </summary>
        /// <param name="prop">Dictionary of player properties</param>
        public virtual void OnPlayerProperty(Dictionary<string, Double> prop) { }

        /// <summary>
        /// Called when server TPS are recalculated by MCC based on world time updates
        /// </summary>
        /// <param name="tps">New estimated server TPS (between 0 and 20)</param>
        public virtual void OnServerTpsUpdate(Double tps) { }

        /// <summary>
        /// Called when a time changed
        /// </summary>
        /// <param name="WorldAge">World age</param>
        /// <param name="TimeOfDay">Time</param>
        public virtual void OnTimeUpdate(long WorldAge, long TimeOfDay) { }

        /// <summary>
        /// Called when an entity moved nearby
        /// </summary>
        /// <param name="entity">Entity with updated location</param>
        public virtual void OnEntityMove(Mapping.Entity entity) { }

        /// <summary>
        /// Called after an internal MCC command has been performed
        /// </summary>
        /// <param name="commandName">MCC Command Name</param>
        /// <param name="commandParams">MCC Command Parameters</param>
        /// <param name="Result">MCC command result</param>
        public virtual void OnInternalCommand(string commandName, string commandParams, string Result) { }

        /// <summary>
        /// Called when an entity spawned nearby
        /// </summary>
        /// <param name="entity">New Entity</param>
        public virtual void OnEntitySpawn(Mapping.Entity entity) { }

        /// <summary>
        /// Called when an entity despawns/dies nearby
        /// </summary>
        /// <param name="entity">Entity wich has just disappeared</param>
        public virtual void OnEntityDespawn(Mapping.Entity entity) { }

        /// <summary>
        /// Called when the player held item has changed
        /// </summary>
        /// <param name="slot">New slot ID</param>
        public virtual void OnHeldItemChange(byte slot) { }

        /// <summary>
        /// Called when the player health has been updated
        /// </summary>
        /// <param name="health">New player health</param>
        /// <param name="food">New food level</param>
        public virtual void OnHealthUpdate(float health, int food) { }

        /// <summary>
        /// Called when an explosion occurs on the server
        /// </summary>
        /// <param name="explode">Explosion location</param>
        /// <param name="recordcount">Amount of blocks blown up</param>
        public virtual void OnExplosion(Location explode, float strength, int recordcount) { }

        /// <summary>
        /// Called when experience updates
        /// </summary>
        /// <param name="Experiencebar">Between 0 and 1</param>
        /// <param name="Level">Level</param>
        /// <param name="TotalExperience">Total Experience</param>
        public virtual void OnSetExperience(float Experiencebar, int Level, int TotalExperience) { }

        /// <summary>
        /// Called when the Game Mode has been updated for a player
        /// </summary>
        /// <param name="playername">Player Name</param>
        /// <param name="uuid">Player UUID</param>
        /// <param name="gamemode">New Game Mode (0: Survival, 1: Creative, 2: Adventure, 3: Spectator).</param>
        public virtual void OnGamemodeUpdate(string playername, Guid uuid, int gamemode) { }
        
        /// <summary>
        /// Called when the Latency has been updated for a player
        /// </summary>
        /// <param name="playername">Player Name</param>
        /// <param name="uuid">Player UUID</param>
        /// <param name="latency">Latency.</param>
        public virtual void OnLatencyUpdate(string playername, Guid uuid, int latency) { }
        
        /// <summary>
        /// Called when the Latency has been updated for a player
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="playername">Player Name</param>
        /// <param name="uuid">Player UUID</param>
        /// <param name="latency">Latency.</param>
        public virtual void OnLatencyUpdate(Entity entity, string playername, Guid uuid, int latency) { }
        
        /// <summary>
        /// Called when a map was updated
        /// </summary>
        /// <param name="mapid"></param>
        /// <param name="scale"></param>
        /// <param name="trackingposition"></param>
        /// <param name="locked"></param>
        /// <param name="iconcount"></param>
        public virtual void OnMapData(int mapid, byte scale, bool trackingposition, bool locked, int iconcount) { }

        /// <summary>
        /// Called when tradeList is received from server
        /// </summary>
        /// <param name="windowID">Window ID</param>
        /// <param name="trades">List of trades.</param>
        /// <param name="villagerInfo">Contains Level, Experience, IsRegularVillager and CanRestock .</param>
        public virtual void OnTradeList(int windowID, List<VillagerTrade> trades, VillagerInfo villagerInfo) { }

        /// <summary>
        /// Called when received a title from the server
        /// <param name="action"> 0 = set title, 1 = set subtitle, 3 = set action bar, 4 = set times and display, 4 = hide, 5 = reset</param>
        /// <param name="titletext"> title text</param>
        /// <param name="subtitletext"> suntitle text</param>
        /// <param name="actionbartext"> action bar text</param>
        /// <param name="fadein"> Fade In</param>
        /// <param name="stay"> Stay</param>
        /// <param name="fadeout"> Fade Out</param>
        /// <param name="json"> json text</param>
        public virtual void OnTitle(int action, string titletext, string subtitletext, string actionbartext, int fadein, int stay, int fadeout, string json) { }

        /// <summary>
        /// Called when an entity equipped
        /// </summary>
        /// <param name="entity"> Entity</param>
        /// <param name="slot"> Equipment slot. 0: main hand, 1: off hand, 2–5: armor slot (2: boots, 3: leggings, 4: chestplate, 5: helmet)</param>
        /// <param name="item"> Item)</param>
        public virtual void OnEntityEquipment(Entity entity, int slot, Item item) { }
        
        /// <summary>
        /// Called when an entity has effect applied
        /// </summary>
        /// <param name="entityid">entity ID</param>
        /// <param name="effect">effect id</param>
        /// <param name="amplifier">effect amplifier</param>
        /// <param name="duration">effect duration</param>
        /// <param name="flags">effect flags</param>
        public virtual void OnEntityEffect(Entity entity, Effects effect, int amplifier, int duration, byte flags) { }

        /// <summary>
        /// Called when a scoreboard objective updated
        /// </summary>
        /// <param name="objectivename">objective name</param>
        /// <param name="mode">0 to create the scoreboard. 1 to remove the scoreboard. 2 to update the display text.</param>
        /// <param name="objectivevalue">Only if mode is 0 or 2. The text to be displayed for the score</param>
        /// <param name="type">Only if mode is 0 or 2. 0 = "integer", 1 = "hearts".</param>
        public virtual void OnScoreboardObjective(string objectivename, byte mode, string objectivevalue, int type, string json) { }
        
        /// <summary>
        /// Called when a scoreboard updated
        /// </summary>
        /// <param name="entityname">The entity whose score this is. For players, this is their username; for other entities, it is their UUID.</param>
        /// <param name="action">0 to create/update an item. 1 to remove an item.</param>
        /// <param name="objectivename">The name of the objective the score belongs to</param>
        /// <param name="value">The score to be displayed next to the entry. Only sent when Action does not equal 1.</param>
        public virtual void OnUpdateScore(string entityname, byte action, string objectivename, int value) { }

        /// <summary>
        /// Called when an inventory/container was updated by server
        /// </summary>
        /// <param name="inventoryId"></param>
        public virtual void OnInventoryUpdate(int inventoryId) { }

        /// <summary>
        /// Called when a container was opened
        /// </summary>
        /// <param name="inventoryId"></param>
        public virtual void OnInventoryOpen(int inventoryId) { }

        /// <summary>
        /// Called when a container was closed
        /// </summary>
        /// <param name="inventoryId"></param>
        public virtual void OnInventoryClose(int inventoryId) { }

        /// <summary>
        /// Called when a player joined the game
        /// </summary>
        /// <param name="uuid">UUID of the player</param>
        /// <param name="name">Name of the player</param>
        public virtual void OnPlayerJoin(Guid uuid, string name) { }

        /// <summary>
        /// Called when a player left the game
        /// </summary>
        /// <param name="uuid">UUID of the player</param>
        /// <param name="name">Name of the player</param>
        public virtual void OnPlayerLeave(Guid uuid, string name) { }
        
        /// <summary>
        /// Called when the player deaths
        /// </summary>
        public virtual void OnDeath() { }
        
        /// <summary>
        /// Called when the player respawns
        /// </summary>
        public virtual void OnRespawn() { }

        /// <summary>
        /// Called when the health of an entity changed
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="health">The health of the entity</param>
        public virtual void OnEntityHealth(Entity entity, float health) { }

        /// <summary>
        /// Called when the metadata of an entity changed
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="metadata">The metadata of the entity</param>
        /// <param name="protocolversion">Ptotocol version</param>
        public virtual void OnEntityMetadata(Entity entity, Dictionary<int, object> metadata) { }

        /// <summary>
        /// Called when a network packet received or sent
        /// </summary>
        /// <remarks>
        /// You need to enable this event by calling <see cref="SetNetworkPacketEventEnabled(bool)"/> with True before you can use this event
        /// </remarks>
        /// <param name="packetID">Packet ID</param>
        /// <param name="packetData">A copy of Packet Data</param>
        /// <param name="isLogin">The packet is login phase or playing phase</param>
        /// <param name="isInbound">The packet is received from server or sent by client</param>
        public virtual void OnNetworkPacket(int packetID, List<byte> packetData, bool isLogin, bool isInbound) { }

        /* =================================================================== */
        /*  ToolBox - Methods below might be useful while creating your bot.   */
        /*  You should not need to interact with other classes of the program. */
        /*  All the methods in this ChatBot class should do the job for you.   */
        /* =================================================================== */

        /// <summary>
        /// Send text to the server. Can be anything such as chat messages or commands
        /// </summary>
        /// <param name="text">Text to send to the server</param>
        /// <param name="sendImmediately">Bypass send queue (Deprecated, still there for compatibility purposes but ignored)</param>
        /// <returns>TRUE if successfully sent (Deprectated, always returns TRUE for compatibility purposes with existing scripts)</returns>
        protected bool SendText(string text, bool sendImmediately = false)
        {
            LogToConsole("Sending '" + text + "'");
            Handler.SendText(text);
            return true;
        }

        /// <summary>
        /// Perform an internal MCC command (not a server command, use SendText() instead for that!)
        /// </summary>
        /// <param name="command">The command to process</param>
        /// <param name="localVars">Local variables passed along with the command</param>
        /// <returns>TRUE if the command was indeed an internal MCC command</returns>
        protected bool PerformInternalCommand(string command, Dictionary<string, object> localVars = null)
        {
            string temp = "";
            return Handler.PerformInternalCommand(command, ref temp, localVars);
        }

        /// <summary>
        /// Perform an internal MCC command (not a server command, use SendText() instead for that!)
        /// </summary>
        /// <param name="command">The command to process</param>
        /// <param name="response_msg">May contain a confirmation or error message after processing the command, or "" otherwise.</param>
        /// <param name="localVars">Local variables passed along with the command</param>
        /// <returns>TRUE if the command was indeed an internal MCC command</returns>
        protected bool PerformInternalCommand(string command, ref string response_msg, Dictionary<string, object> localVars = null)
        {
            return Handler.PerformInternalCommand(command, ref response_msg, localVars);
        }

        /// <summary>
        /// Remove color codes ("§c") from a text message received from the server
        /// </summary>
        public static string GetVerbatim(string text)
        {
            if ( String.IsNullOrEmpty(text) )
                return String.Empty;

            int idx = 0;
            var data = new char[text.Length];

            for ( int i = 0; i < text.Length; i++ )
                if ( text[i] != '§' )
                    data[idx++] = text[i];
                else
                    i++;

            return new string(data, 0, idx);
        }

        /// <summary>
        /// Verify that a string contains only a-z A-Z 0-9 and _ characters.
        /// </summary>
        public static bool IsValidName(string username)
        {
            if (String.IsNullOrEmpty(username))
                return false;

            foreach (char c in username)
                if (!((c >= 'a' && c <= 'z')
                        || (c >= 'A' && c <= 'Z')
                        || (c >= '0' && c <= '9')
                        || c == '_') )
                    return false;

            return true;
        }

        /// <summary>
        /// Returns true if the text passed is a private message sent to the bot
        /// </summary>
        /// <param name="text">text to test</param>
        /// <param name="message">if it's a private message, this will contain the message</param>
        /// <param name="sender">if it's a private message, this will contain the player name that sends the message</param>
        /// <returns>Returns true if the text is a private message</returns>
        protected static bool IsPrivateMessage(string text, ref string message, ref string sender)
        {
            if (String.IsNullOrEmpty(text))
                return false;

            text = GetVerbatim(text);

            //User-defined regex for private chat messages
            if (Settings.ChatFormat_Private != null)
            {
                Match regexMatch = Settings.ChatFormat_Private.Match(text);
                if (regexMatch.Success && regexMatch.Groups.Count >= 3)
                {
                    sender = regexMatch.Groups[1].Value;
                    message = regexMatch.Groups[2].Value;
                    return IsValidName(sender);
                }
            }

            //Built-in detection routine for private messages
            if (Settings.ChatFormat_Builtins)
            {
                string[] tmp = text.Split(' ');
                try
                {
                    //Detect vanilla /tell messages
                    //Someone whispers message (MC 1.5)
                    //Someone whispers to you: message (MC 1.7)
                    if (tmp.Length > 2 && tmp[1] == "whispers")
                    {
                        if (tmp.Length > 4 && tmp[2] == "to" && tmp[3] == "you:")
                        {
                            message = text.Substring(tmp[0].Length + 18); //MC 1.7
                        }
                        else message = text.Substring(tmp[0].Length + 10); //MC 1.5
                        sender = tmp[0];
                        return IsValidName(sender);
                    }

                    //Detect Essentials (Bukkit) /m messages
                    //[Someone -> me] message
                    //[~Someone -> me] message
                    else if (text[0] == '[' && tmp.Length > 3 && tmp[1] == "->"
                            && (tmp[2].ToLower() == "me]" || tmp[2].ToLower() == "moi]")) //'me' is replaced by 'moi' in french servers
                    {
                        message = text.Substring(tmp[0].Length + 4 + tmp[2].Length + 1);
                        sender = tmp[0].Substring(1);
                        if (sender[0] == '~') { sender = sender.Substring(1); }
                        return IsValidName(sender);
                    }

                    //Detect Modified server messages. /m
                    //[Someone @ me] message
                    else if (text[0] == '[' && tmp.Length > 3 && tmp[1] == "@"
                            && (tmp[2].ToLower() == "me]" || tmp[2].ToLower() == "moi]")) //'me' is replaced by 'moi' in french servers
                    {
                        message = text.Substring(tmp[0].Length + 4 + tmp[2].Length + 0);
                        sender = tmp[0].Substring(1);
                        if (sender[0] == '~') { sender = sender.Substring(1); }
                        return IsValidName(sender);
                    }

                    //Detect Essentials (Bukkit) /me messages with some custom prefix
                    //[Prefix] [Someone -> me] message
                    //[Prefix] [~Someone -> me] message
                    else if (text[0] == '[' && tmp[0][tmp[0].Length - 1] == ']'
                            && tmp[1][0] == '[' && tmp.Length > 4 && tmp[2] == "->"
                            && (tmp[3].ToLower() == "me]" || tmp[3].ToLower() == "moi]"))
                    {
                        message = text.Substring(tmp[0].Length + 1 + tmp[1].Length + 4 + tmp[3].Length + 1);
                        sender = tmp[1].Substring(1);
                        if (sender[0] == '~') { sender = sender.Substring(1); }
                        return IsValidName(sender);
                    }

                    //Detect Essentials (Bukkit) /me messages with some custom rank
                    //[Someone [rank] -> me] message
                    //[~Someone [rank] -> me] message
                    else if (text[0] == '[' && tmp.Length > 3 && tmp[2] == "->"
                            && (tmp[3].ToLower() == "me]" || tmp[3].ToLower() == "moi]"))
                    {
                        message = text.Substring(tmp[0].Length + 1 + tmp[1].Length + 4 + tmp[2].Length + 1);
                        sender = tmp[0].Substring(1);
                        if (sender[0] == '~') { sender = sender.Substring(1); }
                        return IsValidName(sender);
                    }

                    //Detect HeroChat PMsend
                    //From Someone: message
                    else if (text.StartsWith("From "))
                    {
                        sender = text.Substring(5).Split(':')[0];
                        message = text.Substring(text.IndexOf(':') + 2);
                        return IsValidName(sender);
                    }
                    else return false;
                }
                catch (IndexOutOfRangeException) { /* Not an expected chat format */ }
                catch (ArgumentOutOfRangeException) { /* Same here */ }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the text passed is a public message written by a player on the chat
        /// </summary>
        /// <param name="text">text to test</param>
        /// <param name="message">if it's message, this will contain the message</param>
        /// <param name="sender">if it's message, this will contain the player name that sends the message</param>
        /// <returns>Returns true if the text is a chat message</returns>
        protected static bool IsChatMessage(string text, ref string message, ref string sender)
        {
            if (String.IsNullOrEmpty(text))
                return false;

            text = GetVerbatim(text);

            //User-defined regex for public chat messages
            if (Settings.ChatFormat_Public != null)
            {
                Match regexMatch = Settings.ChatFormat_Public.Match(text);
                if (regexMatch.Success && regexMatch.Groups.Count >= 3)
                {
                    sender = regexMatch.Groups[1].Value;
                    message = regexMatch.Groups[2].Value;
                    return IsValidName(sender);
                }
            }

            //Built-in detection routine for public messages
            if (Settings.ChatFormat_Builtins)
            {
                string[] tmp = text.Split(' ');

                //Detect vanilla/factions Messages
                //<Someone> message
                //<*Faction Someone> message
                //<*Faction Someone>: message
                //<*Faction ~Nicknamed>: message
                if (text[0] == '<')
                {
                    try
                    {
                        text = text.Substring(1);
                        string[] tmp2 = text.Split('>');
                        sender = tmp2[0];
                        message = text.Substring(sender.Length + 2);
                        if (message.Length > 1 && message[0] == ' ')
                        { message = message.Substring(1); }
                        tmp2 = sender.Split(' ');
                        sender = tmp2[tmp2.Length - 1];
                        if (sender[0] == '~') { sender = sender.Substring(1); }
                        return IsValidName(sender);
                    }
                    catch (IndexOutOfRangeException) { /* Not a vanilla/faction message */ }
                    catch (ArgumentOutOfRangeException) { /* Same here */ }
                }

                //Detect HeroChat Messages
                //Public chat messages
                //[Channel] [Rank] User: Message
                else if (text[0] == '[' && text.Contains(':') && tmp.Length > 2)
                {
                    try
                    {
                        int name_end = text.IndexOf(':');
                        int name_start = text.Substring(0, name_end).LastIndexOf(']') + 2;
                        sender = text.Substring(name_start, name_end - name_start);
                        message = text.Substring(name_end + 2);
                        return IsValidName(sender);
                    }
                    catch (IndexOutOfRangeException) { /* Not a herochat message */ }
                    catch (ArgumentOutOfRangeException) { /* Same here */ }
                }

                //Detect (Unknown Plugin) Messages
                //**Faction<Rank> User : Message
                else if (text[0] == '*'
                    && text.Length > 1
                    && text[1] != ' '
                    && text.Contains('<') && text.Contains('>')
                    && text.Contains(' ') && text.Contains(':')
                    && text.IndexOf('*') < text.IndexOf('<')
                    && text.IndexOf('<') < text.IndexOf('>')
                    && text.IndexOf('>') < text.IndexOf(' ')
                    && text.IndexOf(' ') < text.IndexOf(':'))
                {
                    try
                    {
                        string prefix = tmp[0];
                        string user = tmp[1];
                        string semicolon = tmp[2];
                        if (prefix.All(c => char.IsLetterOrDigit(c) || new char[] { '*', '<', '>', '_' }.Contains(c))
                            && semicolon == ":")
                        {
                            message = text.Substring(prefix.Length + user.Length + 4);
                            return IsValidName(user);
                        }
                    }
                    catch (IndexOutOfRangeException) { /* Not a <unknown plugin> message */ }
                    catch (ArgumentOutOfRangeException) { /* Same here */ }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the text passed is a teleport request (Essentials)
        /// </summary>
        /// <param name="text">Text to parse</param>
        /// <param name="sender">Will contain the sender's username, if it's a teleport request</param>
        /// <returns>Returns true if the text is a teleport request</returns>
        protected static bool IsTeleportRequest(string text, ref string sender)
        {
            if (String.IsNullOrEmpty(text))
                return false;

            text = GetVerbatim(text);

            //User-defined regex for teleport requests
            if (Settings.ChatFormat_TeleportRequest != null)
            {
                Match regexMatch = Settings.ChatFormat_TeleportRequest.Match(text);
                if (regexMatch.Success && regexMatch.Groups.Count >= 2)
                {
                    sender = regexMatch.Groups[1].Value;
                    return IsValidName(sender);
                }
            }

            //Built-in detection routine for teleport requests
            if (Settings.ChatFormat_Builtins)
            {
                string[] tmp = text.Split(' ');

                //Detect Essentials teleport requests, prossibly with
                //nicknamed names or other modifications such as HeroChat
                if (text.EndsWith("has requested to teleport to you.")
                    || text.EndsWith("has requested that you teleport to them."))
                {
                    //<Rank> Username has requested...
                    //[Rank] Username has requested...
                    if (((tmp[0].StartsWith("<") && tmp[0].EndsWith(">"))
                        || (tmp[0].StartsWith("[") && tmp[0].EndsWith("]")))
                        && tmp.Length > 1)
                        sender = tmp[1];

                    //Username has requested...
                    else sender = tmp[0];

                    //~Username has requested...
                    if (sender.Length > 1 && sender[0] == '~')
                        sender = sender.Substring(1);

                    //Final check on username validity
                    return IsValidName(sender);
                }
            }

            return false;
        }

        /// <summary>
        /// Write some text in the console. Nothing will be sent to the server.
        /// </summary>
        /// <param name="text">Log text to write</param>
        protected void LogToConsole(object text)
        {
            if (_handler == null || master == null)
                ConsoleIO.WriteLogLine(String.Format("[{0}] {1}", this.GetType().Name, text));
            else
                Handler.Log.Info(String.Format("[{0}] {1}", this.GetType().Name, text));
            string logfile = Settings.ExpandVars(Settings.chatbotLogFile);

            if (!String.IsNullOrEmpty(logfile))
            {
                if (!File.Exists(logfile))
                {
                    try { Directory.CreateDirectory(Path.GetDirectoryName(logfile)); }
                    catch { return; /* Invalid path or access denied */ }
                    try { File.WriteAllText(logfile, ""); }
                    catch { return; /* Invalid file name or access denied */ }
                }

                File.AppendAllLines(logfile, new string[] { GetTimestamp() + ' ' + text });
            }
        }

        /// <summary>
        /// Write some text in the console, but only if DebugMessages is enabled in INI file. Nothing will be sent to the server.
        /// </summary>
        /// <param name="text">Debug log text to write</param>
        protected void LogDebugToConsole(object text)
        {
            if (Settings.DebugMessages)
                LogToConsole(text);
        }

        /// <summary>
        /// Write the translated text in the console by giving a translation key. Nothing will be sent to the server.
        /// </summary>
        /// <param name="key">Translation key</param>
        /// <param name="args"></param>
        protected void LogToConsoleTranslated(string key, params object[] args)
        {
            LogToConsole(Translations.TryGet(key, args));
        }

        /// <summary>
        /// Write the translated text in the console by giving a translation key, but only if DebugMessages is enabled in INI file. Nothing will be sent to the server.
        /// </summary>
        /// <param name="key">Translation key</param>
        /// <param name="args"></param>
        protected void LogDebugToConsoleTranslated(string key, params object[] args)
        {
            LogDebugToConsole(Translations.TryGet(key, args));
        }

        /// <summary>
        /// Disconnect from the server and restart the program
        /// It will unload and reload all the bots and then reconnect to the server
        /// </summary>
        /// <param name="ExtraAttempts">In case of failure, maximum extra attempts before aborting</param>
        /// <param name="delaySeconds">Optional delay, in seconds, before restarting</param>
        protected void ReconnectToTheServer(int ExtraAttempts = 3, int delaySeconds = 0)
        {
            if (Settings.DebugMessages)
                ConsoleIO.WriteLogLine(Translations.Get("chatbot.reconnect", this.GetType().Name));
            McClient.ReconnectionAttemptsLeft = ExtraAttempts;
            Program.Restart(delaySeconds);
        }

        /// <summary>
        /// Disconnect from the server and exit the program
        /// </summary>
        protected void DisconnectAndExit()
        {
            Program.Exit();
        }

        /// <summary>
        /// Unload the chatbot, and release associated memory.
        /// </summary>
        protected void UnloadBot()
        {
            Handler.BotUnLoad(this);
        }

        /// <summary>
        /// Send a private message to a player
        /// </summary>
        /// <param name="player">Player name</param>
        /// <param name="message">Message</param>
        protected void SendPrivateMessage(string player, string message)
        {
            SendText(String.Format("/{0} {1} {2}", Settings.PrivateMsgsCmdName, player, message));
        }

        /// <summary>
        /// Run a script from a file using a Scripting bot
        /// </summary>
        /// <param name="filename">File name</param>
        /// <param name="playername">Player name to send error messages, if applicable</param>
        /// <param name="localVars">Local variables for use in the Script</param>
        protected void RunScript(string filename, string playername = null, Dictionary<string, object> localVars = null)
        {
            Handler.BotLoad(new ChatBots.Script(filename, playername, localVars));
        }

        /// <summary>
        /// Load an additional ChatBot
        /// </summary>
        /// <param name="chatBot">ChatBot to load</param>
        protected void BotLoad(ChatBot chatBot)
        {
            Handler.BotLoad(chatBot);
        }

        /// <summary>
        /// Check whether Terrain and Movements is enabled.
        /// </summary>
        /// <returns>Enable status.</returns>
        public bool GetTerrainEnabled()
        {
            return Handler.GetTerrainEnabled();
        }

        /// <summary>
        /// Enable or disable Terrain and Movements.
        /// Please note that Enabling will be deferred until next relog, respawn or world change.
        /// </summary>
        /// <param name="enabled">Enabled</param>
        /// <returns>TRUE if the setting was applied immediately, FALSE if delayed.</returns>
        public bool SetTerrainEnabled(bool enabled)
        {
            return Handler.SetTerrainEnabled(enabled);
        }

        /// <summary>
        /// Get entity handling status
        /// </summary>
        /// <returns></returns>
        /// <remarks>Entity Handling cannot be enabled in runtime (or after joining server)</remarks>
        public bool GetEntityHandlingEnabled()
        {
            return Handler.GetEntityHandlingEnabled();
        }

        /// <summary>
        /// start Sneaking
        /// </summary>
        protected bool Sneak(bool on)
        {
            return SendEntityAction(on ? Protocol.EntityActionType.StartSneaking : Protocol.EntityActionType.StopSneaking);
        }

        /// <summary>
        /// Send Entity Action
        /// </summary>
        private bool SendEntityAction(Protocol.EntityActionType entityAction)
        {
            return Handler.SendEntityAction(entityAction);
        }

        /// <summary>
        /// Attempt to dig a block at the specified location
        /// </summary>
        /// <param name="location">Location of block to dig</param>
        /// <param name="swingArms">Also perform the "arm swing" animation</param>
        /// <param name="lookAtBlock">Also look at the block before digging</param>
        protected bool DigBlock(Location location, bool swingArms = true, bool lookAtBlock = true)
        {
            return Handler.DigBlock(location, swingArms, lookAtBlock);
        }

        /// <summary>
        /// SetSlot
        /// </summary>
        protected void SetSlot(int slotNum)
        {
            Handler.ChangeSlot((short)slotNum);
        }

        /// <summary>
        /// Get the current Minecraft World
        /// </summary>
        /// <returns>Minecraft world or null if associated setting is disabled</returns>
        protected Mapping.World GetWorld()
        {
            if (GetTerrainEnabled())
                return Handler.GetWorld();
            return null;
        }
        
        /// <summary>
        /// Get all Entities
        /// </summary>
        /// <returns>All Entities</returns>
        protected Dictionary<int, Entity> GetEntities()
        {
            return Handler.GetEntities();
        }

        /// <summary>
        /// Get all players Latency
        /// </summary>
        /// <returns>All players latency</returns>
        protected Dictionary<string, int> GetPlayersLatency()
        {
            return Handler.GetPlayersLatency();
        }
        
        /// <summary>
        /// Get the current location of the player (Feet location)
        /// </summary>
        /// <returns>Minecraft world or null if associated setting is disabled</returns>
        protected Mapping.Location GetCurrentLocation()
        {
            return Handler.GetCurrentLocation();
        }

        /// <summary>
        /// Move to the specified location
        /// </summary>
        /// <param name="location">Location to reach</param>
        /// <param name="allowUnsafe">Allow possible but unsafe locations thay may hurt the player: lava, cactus...</param>
        /// <param name="allowDirectTeleport">Allow non-vanilla teleport instead of computing path, but may cause invalid moves and/or trigger anti-cheat plugins</param>
        /// <returns>True if a path has been found</returns>
        protected bool MoveToLocation(Mapping.Location location, bool allowUnsafe = false, bool allowDirectTeleport = false)
        {
            return Handler.MoveTo(location, allowUnsafe, allowDirectTeleport);
        }

        /// <summary>
        /// Look at the specified location
        /// </summary>
        /// <param name="location">Location to look at</param>
        protected void LookAtLocation(Mapping.Location location)
        {
            Handler.UpdateLocation(Handler.GetCurrentLocation(), location);
        }

        /// <summary>
        /// Get a Y-M-D h:m:s timestamp representing the current system date and time
        /// </summary>
        protected static string GetTimestamp()
        {
            DateTime time = DateTime.Now;
            return String.Format("{0}-{1}-{2} {3}:{4}:{5}",
                time.Year.ToString("0000"),
                time.Month.ToString("00"),
                time.Day.ToString("00"),
                time.Hour.ToString("00"),
                time.Minute.ToString("00"),
                time.Second.ToString("00"));
        }

        /// <summary>
        /// Load entries from a file as a string array, removing duplicates and empty lines
        /// </summary>
        /// <param name="file">File to load</param>
        /// <returns>The string array or an empty array if failed to load the file</returns>
        protected string[] LoadDistinctEntriesFromFile(string file)
        {
            if (File.Exists(file))
            {
                //Read all lines from file, remove lines with no text, convert to lowercase,
                //remove duplicate entries, convert to a string array, and return the result.
                return File.ReadAllLines(file, Encoding.UTF8)
                        .Where(line => !String.IsNullOrWhiteSpace(line))
                        .Select(line => line.ToLower())
                        .Distinct().ToArray();
            }
            else
            {
                LogToConsole("File not found: " + System.IO.Path.GetFullPath(file));
                return new string[0];
            }
        }

        /// <summary>
        /// Return the Server Port where the client is connected to
        /// </summary>
        /// <returns>Server Port where the client is connected to</returns>
        protected int GetServerPort()
        {
            return Handler.GetServerPort();
        }

        /// <summary>
        /// Return the Server Host where the client is connected to
        /// </summary>
        /// <returns>Server Host where the client is connected to</returns>
        protected string GetServerHost()
        {
            return Handler.GetServerHost();
        }

        /// <summary>
        /// Return the Username of the current account
        /// </summary>
        /// <returns>Username of the current account</returns>
        protected string GetUsername()
        {
            return Handler.GetUsername();
        }
        
        /// <summary>
        /// Return the Gamemode of the current account
        /// </summary>
        /// <returns>Username of the current account</returns>
        protected int GetGamemode()
        {
            return Handler.GetGamemode();
        }
        
        /// <summary>
        /// Return the UserUUID of the current account
        /// </summary>
        /// <returns>UserUUID of the current account</returns>
        protected string GetUserUUID()
        {
            return Handler.GetUserUUID();
        }

        /// <summary>
        /// Return the list of currently online players
        /// </summary>
        /// <returns>List of online players</returns>
        protected string[] GetOnlinePlayers()
        {
            return Handler.GetOnlinePlayers();
        }

        /// <summary>
        /// Get a dictionary of online player names and their corresponding UUID
        /// </summary>
        /// <returns>
        ///     dictionary of online player whereby
        ///     UUID represents the key
        ///     playername represents the value</returns>
        protected Dictionary<string, string> GetOnlinePlayersWithUUID()
        {
            return Handler.GetOnlinePlayersWithUUID();
        }

        /// <summary>
        /// Registers the given plugin channel for use by this chatbot.
        /// </summary>
        /// <param name="channel">The name of the channel to register</param>
        protected void RegisterPluginChannel(string channel)
        {
            this.registeredPluginChannels.Add(channel);
            Handler.RegisterPluginChannel(channel, this);
        }

        /// <summary>
        /// Unregisters the given plugin channel, meaning this chatbot can no longer use it.
        /// </summary>
        /// <param name="channel">The name of the channel to unregister</param>
        protected void UnregisterPluginChannel(string channel)
        {
            this.registeredPluginChannels.RemoveAll(chan => chan == channel);
            Handler.UnregisterPluginChannel(channel, this);
        }

        /// <summary>
        /// Sends the given plugin channel message to the server, if the channel has been registered.
        /// See http://wiki.vg/Plugin_channel for more information about plugin channels.
        /// </summary>
        /// <param name="channel">The channel to send the message on.</param>
        /// <param name="data">The data to send.</param>
        /// <param name="sendEvenIfNotRegistered">Should the message be sent even if it hasn't been registered by the server or this bot?  (Some Minecraft channels aren't registered)</param>
        /// <returns>Whether the message was successfully sent.  False if there was a network error or if the channel wasn't registered.</returns>
        protected bool SendPluginChannelMessage(string channel, byte[] data, bool sendEvenIfNotRegistered = false)
        {
            if (!sendEvenIfNotRegistered)
            {
                if (!this.registeredPluginChannels.Contains(channel))
                {
                    return false;
                }
            }
            return Handler.SendPluginChannelMessage(channel, data, sendEvenIfNotRegistered);
        }

        /// <summary>
        /// Get server current TPS (tick per second)
        /// </summary>
        /// <returns>tps</returns>
        protected Double GetServerTPS()
        {
            return Handler.GetServerTPS();
        }

        /// <summary>
        /// Interact with an entity
        /// </summary>
        /// <param name="EntityID"></param>
        /// <param name="type">0: interact, 1: attack, 2: interact at</param>
        /// <param name="hand">Hand.MainHand or Hand.OffHand</param>
        /// <returns>TRUE in case of success</returns>
        protected bool InteractEntity(int EntityID, int type, Hand hand = Hand.MainHand)
        {
            return Handler.InteractEntity(EntityID, type, hand);
        }

        /// <summary>
        /// Give Creative Mode items into regular/survival Player Inventory
        /// </summary>
        /// <remarks>(obviously) requires to be in creative mode</remarks>
        /// </summary>
        /// <param name="slot">Destination inventory slot</param>
        /// <param name="itemType">Item type</param>
        /// <param name="count">Item count</param>
        /// <returns>TRUE if item given successfully</returns>
        protected bool CreativeGive(int slot, ItemType itemType, int count, Dictionary<string, object> nbt = null)
        {
            return Handler.DoCreativeGive(slot, itemType, count, nbt);
        }

        /// <summary>
        /// Plays animation (Player arm swing)
        /// </summary>
        /// <param name="hand">Hand.MainHand or Hand.OffHand</param>
        /// <returns>TRUE if animation successfully done</returns>
        public bool SendAnimation(Hand hand = Hand.MainHand)
        {
            return Handler.DoAnimation((int)hand);
        }

        /// <summary>
        /// Use item currently in the player's hand (active inventory bar slot)
        /// </summary>
        /// <returns>TRUE if successful</returns>
        protected bool UseItemInHand()
        {
            return Handler.UseItemOnHand();
        }

        /// <summary>
        /// Check inventory handling enable status
        /// </summary>
        /// <returns>TRUE if inventory handling is enabled</returns>
        public bool GetInventoryEnabled()
        {
            return Handler.GetInventoryEnabled();
        }

        /// <summary>
        /// Place the block at hand in the Minecraft world
        /// </summary>
        /// <param name="location">Location to place block to</param>
        /// <param name="blockFace">Block face (e.g. Direction.Down when clicking on the block below to place this block)</param>
        /// <param name="hand">Hand.MainHand or Hand.OffHand</param>
        /// <returns>TRUE if successfully placed</returns>
        public bool SendPlaceBlock(Location location, Direction blockFace, Hand hand = Hand.MainHand)
        {
            return Handler.PlaceBlock(location, blockFace, hand);
        }

        /// <summary>
        /// Get the player's inventory. Do not write to it, will not have any effect server-side.
        /// </summary>
        /// <returns>Player inventory</returns>
        protected Container GetPlayerInventory()
        {
            Container container = Handler.GetPlayerInventory();
            return container == null ? null : new Container(container.ID, container.Type, container.Title, container.Items);
        }

        /// <summary>
        /// Get all inventories, player and container(s). Do not write to them. Will not have any effect server-side.
        /// </summary>
        /// <returns>All inventories</returns>
        public Dictionary<int, Container> GetInventories()
        {
            return Handler.GetInventories();
        }

        /// <summary>
        /// Perform inventory action
        /// </summary>
        /// <param name="inventoryId">Inventory ID</param>
        /// <param name="slot">Slot ID</param>
        /// <param name="actionType">Action Type</param>
        /// <returns>TRUE in case of success</returns>
        protected bool WindowAction(int inventoryId, int slot, WindowActionType actionType)
        {
            return Handler.DoWindowAction(inventoryId, slot, actionType);
        }

        /// <summary>
        /// Get inventory action helper
        /// </summary>
        /// <param name="container">Inventory Container</param>
        /// <returns>ItemMovingHelper instance</returns>
        protected ItemMovingHelper GetItemMovingHelper(Container container)
        {
            return new ItemMovingHelper(container, Handler);
        }

        /// <summary>
        /// Change player selected hotbar slot
        /// </summary>
        /// <param name="slot">0-8</param>
        /// <returns>True if success</returns>
        protected bool ChangeSlot(short slot)
        {
            return Handler.ChangeSlot(slot);
        }

        /// <summary>
        /// Get current player selected hotbar slot
        /// </summary>
        /// <returns>0-8</returns>
        protected byte GetCurrentSlot()
        {
            return Handler.GetCurrentSlot();
        }
        
        /// <summary>
        /// Clean all inventory
        /// </summary>
        /// <returns>TRUE if the uccessfully clear</returns>
        protected bool ClearInventories()
        {
            return Handler.ClearInventories();
        }
        
        /// <summary>
        /// Update sign text
        /// </summary>
        /// <param name="location"> sign location</param>
        /// <param name="line1"> text one</param>
        /// <param name="line2"> text two</param>
        /// <param name="line3"> text three</param>
        /// <param name="line4"> text1 four</param>
        protected bool UpdateSign(Location location, string line1, string line2, string line3, string line4)
        {
            return Handler.UpdateSign(location, line1, line2, line3, line4);
        }

        /// <summary>
        /// Selects villager trade
        /// </summary>
        /// <param name="selectedSlot">Trade slot to select, starts at 0.</param>
        protected bool SelectTrade(int selectedSlot)
        {
            return Handler.SelectTrade(selectedSlot);
        }
        
        /// <summary>
        /// Update command block
        /// </summary>
        /// <param name="location">command block location</param>
        /// <param name="command">command</param>
        /// <param name="mode">command block mode</param>
        /// <param name="flags">command block flags</param>
        protected bool UpdateCommandBlock(Location location, string command, CommandBlockMode mode, CommandBlockFlags flags)
        {
            return Handler.UpdateCommandBlock(location, command, mode, flags);
        }

        /// <summary>
        /// Register a command in command prompt
        /// </summary>
        /// <param name="cmdName">Name of the command</param>
        /// <param name="cmdDesc">Description/usage of the command</param>
        /// <param name="callback">Method for handling the command</param>
        /// <returns>True if successfully registered</returns>
        protected bool RegisterChatBotCommand(string cmdName, string cmdDesc, string cmdUsage, CommandRunner callback)
        {
            return Handler.RegisterCommand(cmdName, cmdDesc, cmdUsage, callback);
        }

        /// <summary>
        /// Close a opened inventory
        /// </summary>
        /// <param name="inventoryID"></param>
        /// <returns>True if success</returns>
        protected bool CloseInventory(int inventoryID)
        {
            return Handler.CloseInventory(inventoryID);
        }

        /// <summary>
        /// Get max length for chat messages
        /// </summary>
        /// <returns>Max length, in characters</returns>
        protected int GetMaxChatMessageLength()
        {
            return Handler.GetMaxChatMessageLength();
        }
        
        /// <summary>
        /// Respawn player
        /// </summary>
        protected bool Respawn()
        {
            if (Handler.GetHealth() <= 0)
                return Handler.SendRespawnPacket();
            else return false;
        }

        /// <summary>
        /// Enable or disable network packet event calling. If you want to capture every packet including login phase, please enable this in <see cref="Initialize()"/>
        /// </summary>
        /// <remarks>
        /// Enable this may increase memory usage.
        /// </remarks>
        /// <param name="enabled"></param>
        protected void SetNetworkPacketEventEnabled(bool enabled)
        {
            Handler.SetNetworkPacketCaptureEnabled(enabled);
        }

        /// <summary>
        /// Get the minecraft protcol number currently in use
        /// </summary>
        /// <returns>Protcol number</returns>
        protected int GetProtocolVersion()
        {
            return Handler.GetProtocolVersion();
        }

        /// <summary>
        /// Command runner definition.
        /// Returned string will be the output of the command
        /// </summary>
        /// <param name="command">Full command</param>
        /// <param name="args">Arguments in the command</param>
        /// <returns>Command result to display to the user</returns>
        public delegate string CommandRunner(string command, string[] args);

        /// <summary>
        /// Command class with constructor for creating command for ChatBots.
        /// </summary>
        public class ChatBotCommand : Command
        {
            public CommandRunner Runner;

            private readonly string _cmdName;
            private readonly string _cmdDesc;
            private readonly string _cmdUsage;

            public override string CmdName { get { return _cmdName; } }
            public override string CmdUsage { get { return _cmdUsage; } }
            public override string CmdDesc { get { return _cmdDesc; } }

            public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
            {
                return this.Runner(command, getArgs(command));
            }

            /// <summary>
            /// ChatBotCommand Constructor
            /// </summary>
            /// <param name="cmdName">Name of the command</param>
            /// <param name="cmdDesc">Description of the command. Support tranlation.</param>
            /// <param name="cmdUsage">Usage of the command</param>
            /// <param name="callback">Method for handling the command</param>
            public ChatBotCommand(string cmdName, string cmdDesc, string cmdUsage, CommandRunner callback)
            {
                this._cmdName = cmdName;
                this._cmdDesc = cmdDesc;
                this._cmdUsage = cmdUsage;
                this.Runner = callback;
            }
        }
    }
}
