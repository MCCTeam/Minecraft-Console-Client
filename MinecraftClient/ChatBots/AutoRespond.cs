using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using MinecraftClient.CommandHandler;
using MinecraftClient.Scripting;
using Tomlet.Attributes;
using static MinecraftClient.Settings;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// This bot automatically runs actions when a user sends a message matching a specified rule
    /// </summary>
    public class AutoRespond : ChatBot
    {
        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "AutoRespond";

            public bool Enabled = false;

            public string Matches_File = @"matches.ini";

            [TomlInlineComment("$ChatBot.AutoRespond.Match_Colors$")]
            public bool Match_Colors = false;

            public void OnSettingUpdate()
            {
                Matches_File ??= string.Empty;

                if (!Enabled) return;

                if (!File.Exists(Matches_File))
                {
                    LogToConsole(BotName, string.Format(Translations.bot_autoRespond_file_not_found, Path.GetFullPath(Matches_File)));
                    LogToConsole(BotName, Translations.general_bot_unload);
                    Enabled = false;
                }
            }
        }

        private List<RespondRule>? respondRules;
        private enum MessageType { Public, Private, Other };

        /// <summary>
        /// Describe a respond rule based on a simple match or a regex
        /// </summary>
        private class RespondRule
        {
            private readonly Regex? regex;
            private readonly string? match;
            private readonly string? actionPublic;
            private readonly string? actionPrivate;
            private readonly string? actionOther;
            private readonly bool ownersOnly;
            private readonly TimeSpan cooldown;
            private DateTime cooldownExpiration;

            /// <summary>
            /// Create a respond rule from a regex and a reponse message or command
            /// </summary>
            /// <param name="regex">Regex</param>
            /// <param name="actionPublic">Internal command to run for public messages</param>
            /// <param name="actionPrivate">Internal command to run for private messages</param>
            /// <param name="actionOther">Internal command to run for any other messages</param>
            /// <param name="ownersOnly">Only match messages from bot owners</param>
            /// <param name="cooldown">Minimal cooldown between two matches</param>
            public RespondRule(Regex regex, string? actionPublic, string? actionPrivate, string? actionOther, bool ownersOnly, TimeSpan cooldown)
            {
                this.regex = regex;
                match = null;
                this.actionPublic = actionPublic;
                this.actionPrivate = actionPrivate;
                this.actionOther = actionOther;
                this.ownersOnly = ownersOnly;
                this.cooldown = cooldown;
                cooldownExpiration = DateTime.MinValue;
            }

            /// <summary>
            /// Create a respond rule from a match string and a reponse message or command
            /// </summary>
            /// <param name="match">Match string</param>
            /// <param name="actionPublic">Internal command to run for public messages</param>
            /// <param name="actionPrivate">Internal command to run for private messages</param>
            /// <param name="ownersOnly">Only match messages from bot owners</param>
            /// <param name="cooldown">Minimal cooldown between two matches</param>
            public RespondRule(string? match, string? actionPublic, string? actionPrivate, string? actionOther, bool ownersOnly, TimeSpan cooldown)
            {
                regex = null;
                this.match = match;
                this.actionPublic = actionPublic;
                this.actionPrivate = actionPrivate;
                this.actionOther = actionOther;
                this.ownersOnly = ownersOnly;
                this.cooldown = cooldown;
                cooldownExpiration = DateTime.MinValue;
            }

            /// <summary>
            /// Match the respond rule to the specified string and return a message or command to send if a match is detected
            /// </summary>
            /// <param name="username">Player who have sent the message</param>
            /// <param name="message">Message to match against the regex or match string</param>
            /// <param name="msgType">Type of the message public/private message, or other message</param>
            /// <param name="localVars">Dictionary to populate with match variables in case of Regex match</param>
            /// <returns>Internal command to run as a response to this user, or null if no match has been detected</returns>
            public string? Match(string username, string message, MessageType msgType, Dictionary<string, object> localVars)
            {
                if (DateTime.Now < cooldownExpiration)
                    return null;

                string? toSend = null;

                if (ownersOnly && (String.IsNullOrEmpty(username) || !Settings.Config.Main.Advanced.BotOwners.Contains(username.ToLower())))
                    return null;

                switch (msgType)
                {
                    case MessageType.Public: toSend = actionPublic; break;
                    case MessageType.Private: toSend = actionPrivate; break;
                    case MessageType.Other: toSend = actionOther; break;
                }

                if (String.IsNullOrEmpty(toSend))
                    return null;

                if (regex != null)
                {
                    if (regex.IsMatch(message))
                    {
                        cooldownExpiration = DateTime.Now + cooldown;
                        Match regexMatch = regex.Match(message);
                        localVars["match_0"] = regexMatch.Groups[0].Value;
                        for (int i = regexMatch.Groups.Count - 1; i >= 1; i--)
                        {
                            toSend = toSend.Replace("$" + i, regexMatch.Groups[i].Value);
                            localVars["match_" + i] = regexMatch.Groups[i].Value;
                        }
                        toSend = toSend.Replace("$u", username);
                        localVars["match_u"] = username;
                        return toSend;
                    }
                }
                else if (!String.IsNullOrEmpty(match))
                {
                    if (message.ToLower().Contains(match.ToLower()))
                    {
                        cooldownExpiration = DateTime.Now + cooldown;
                        localVars["match_0"] = message;
                        localVars["match_u"] = username;
                        return toSend.Replace("$u", username);
                    }
                }

                return null;
            }

            /// <summary>
            /// Get a string representation of the RespondRule
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return string.Format(
                    Translations.bot_autoRespond_match,
                    match,
                    regex,
                    actionPublic,
                    actionPrivate,
                    actionOther,
                    ownersOnly,
                    (int)cooldown.TotalSeconds
                );
            }
        }

        /// <summary>
        /// Initialize the AutoRespond bot from the matches file
        /// </summary>
        public override void Initialize()
        {
            if (File.Exists(Config.Matches_File))
            {
                Regex? matchRegex = null;
                string? matchString = null;
                string? matchAction = null;
                string? matchActionPrivate = null;
                string? matchActionOther = null;
                bool ownersOnly = false;
                TimeSpan cooldown = TimeSpan.Zero;
                respondRules = new List<RespondRule>();

                LogDebugToConsole(string.Format(Translations.bot_autoRespond_loading, System.IO.Path.GetFullPath(Config.Matches_File)));

                foreach (string lineRAW in File.ReadAllLines(Config.Matches_File, Encoding.UTF8))
                {
                    string line = lineRAW.Split('#')[0].Trim();
                    if (line.Length > 0)
                    {
                        if (line[0] == '[' && line[^1] == ']')
                        {
                            switch (line[1..^1].ToLower())
                            {
                                case "match":
                                    CheckAddMatch(matchRegex, matchString, matchAction, matchActionPrivate, matchActionOther, ownersOnly, cooldown);
                                    matchRegex = null;
                                    matchString = null;
                                    matchAction = null;
                                    matchActionPrivate = null;
                                    matchActionOther = null;
                                    ownersOnly = false;
                                    cooldown = TimeSpan.Zero;
                                    break;
                            }
                        }
                        else
                        {
                            string argName = line.Split('=')[0];
                            if (line.Length > (argName.Length + 1))
                            {
                                string argValue = line[(argName.Length + 1)..];
                                switch (argName.ToLower())
                                {
                                    case "regex": matchRegex = new Regex(argValue); break;
                                    case "match": matchString = argValue; break;
                                    case "action": matchAction = argValue; break;
                                    case "actionprivate": matchActionPrivate = argValue; break;
                                    case "actionother": matchActionOther = argValue; break;
                                    case "ownersonly": ownersOnly = bool.Parse(argValue); break;
                                    case "cooldown": cooldown = TimeSpan.FromSeconds(int.Parse(argValue, NumberStyles.Any, CultureInfo.CurrentCulture)); break;
                                }
                            }
                        }
                    }
                }
                CheckAddMatch(matchRegex, matchString, matchAction, matchActionPrivate, matchActionOther, ownersOnly, cooldown);
            }
            else
            {
                LogToConsole(string.Format(Translations.bot_autoRespond_file_not_found, System.IO.Path.GetFullPath(Config.Matches_File)));
                UnloadBot(); //No need to keep the bot active
            }
        }

        /// <summary>
        /// Create a new respond rule from the provided arguments, only if they are valid: at least one match and one action
        /// </summary>
        /// <param name="matchRegex">Matching regex</param>
        /// <param name="matchString">Matching string</param>
        /// <param name="matchAction">Action if the matching message is public</param>
        /// <param name="matchActionPrivate">Action if the matching message is private</param>
        /// <param name="ownersOnly">Only match messages from bot owners</param>
        /// <param name="cooldown">Minimal cooldown between two matches</param>
        private void CheckAddMatch(Regex? matchRegex, string? matchString, string? matchAction, string? matchActionPrivate, string? matchActionOther, bool ownersOnly, TimeSpan cooldown)
        {
            if (matchRegex != null || matchString != null || matchAction != null || matchActionPrivate != null || matchActionOther != null || ownersOnly || cooldown != TimeSpan.Zero)
            {
                RespondRule rule = matchRegex != null
                    ? new RespondRule(matchRegex, matchAction, matchActionPrivate, matchActionOther, ownersOnly, cooldown)
                    : new RespondRule(matchString, matchAction, matchActionPrivate, matchActionOther, ownersOnly, cooldown);

                if (matchAction != null || matchActionPrivate != null || matchActionOther != null)
                {
                    if (matchRegex != null || matchString != null)
                    {
                        respondRules!.Add(rule);
                        LogDebugToConsole(string.Format(Translations.bot_autoRespond_loaded_match, rule));
                    }
                    else LogDebugToConsole(string.Format(Translations.bot_autoRespond_no_trigger, rule));
                }
                else LogDebugToConsole(string.Format(Translations.bot_autoRespond_no_action, rule));
            }
        }

        /// <summary>
        /// Process messages from the server and test them against all matches
        /// </summary>
        /// <param name="text">Text from the server</param>
        public override void GetText(string text)
        {
            //Remove colour codes
            if (!Config.Match_Colors)
                text = GetVerbatim(text);

            //Get Message type
            string sender = "", message = "";
            MessageType msgType = MessageType.Other;
            if (IsChatMessage(text, ref message, ref sender))
                msgType = MessageType.Public;
            else if (IsPrivateMessage(text, ref message, ref sender))
                msgType = MessageType.Private;
            else message = text;

            //Do not process messages sent by the bot itself
            if (msgType == MessageType.Other || sender != InternalConfig.Username)
            {
                foreach (RespondRule rule in respondRules!)
                {
                    Dictionary<string, object> localVars = new();
                    string? toPerform = rule.Match(sender, message, msgType, localVars);
                    if (!string.IsNullOrEmpty(toPerform))
                    {
                        CmdResult response = new();
                        LogToConsole(string.Format(Translations.bot_autoRespond_match_run, toPerform));
                        PerformInternalCommand(toPerform, ref response, localVars);
                        if (response.status != CmdResult.Status.Done || !string.IsNullOrWhiteSpace(response.result))
                            LogToConsole(response);
                    }
                }
            }
        }
    }
}
