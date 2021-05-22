using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// This bot automatically runs actions when a user sends a message matching a specified rule
    /// </summary>
    class AutoRespond : ChatBot
    {
        private string matchesFile;
        private List<RespondRule> respondRules;
        private enum MessageType { Public, Private, Other };

        /// <summary>
        /// Create a new AutoRespond bot
        /// </summary>
        /// <param name="matchesFile">INI File to load matches from</param>
        public AutoRespond(string matchesFile)
        {
            this.matchesFile = matchesFile;
        }

        /// <summary>
        /// Describe a respond rule based on a simple match or a regex
        /// </summary>
        private class RespondRule
        {
            private Regex regex;
            private string match;
            private string actionPublic;
            private string actionPrivate;
            private string actionOther;
            private bool ownersOnly;
            private TimeSpan cooldown;
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
            public RespondRule(Regex regex, string actionPublic, string actionPrivate, string actionOther, bool ownersOnly, TimeSpan cooldown)
            {
                this.regex = regex;
                this.match = null;
                this.actionPublic = actionPublic;
                this.actionPrivate = actionPrivate;
                this.actionOther = actionOther;
                this.ownersOnly = ownersOnly;
                this.cooldown = cooldown;
                this.cooldownExpiration = DateTime.MinValue;
            }

            /// <summary>
            /// Create a respond rule from a match string and a reponse message or command
            /// </summary>
            /// <param name="match">Match string</param>
            /// <param name="actionPublic">Internal command to run for public messages</param>
            /// <param name="actionPrivate">Internal command to run for private messages</param>
            /// <param name="ownersOnly">Only match messages from bot owners</param>
            /// <param name="cooldown">Minimal cooldown between two matches</param>
            public RespondRule(string match, string actionPublic, string actionPrivate, string actionOther, bool ownersOnly, TimeSpan cooldown)
            {
                this.regex = null;
                this.match = match;
                this.actionPublic = actionPublic;
                this.actionPrivate = actionPrivate;
                this.actionOther = actionOther;
                this.ownersOnly = ownersOnly;
                this.cooldown = cooldown;
                this.cooldownExpiration = DateTime.MinValue;
            }

            /// <summary>
            /// Match the respond rule to the specified string and return a message or command to send if a match is detected
            /// </summary>
            /// <param name="username">Player who have sent the message</param>
            /// <param name="message">Message to match against the regex or match string</param>
            /// <param name="msgType">Type of the message public/private message, or other message</param>
            /// <param name="localVars">Dictionary to populate with match variables in case of Regex match</param>
            /// <returns>Internal command to run as a response to this user, or null if no match has been detected</returns>
            public string Match(string username, string message, MessageType msgType, Dictionary<string, object> localVars)
            {
                if (DateTime.Now < cooldownExpiration)
                    return null;

                string toSend = null;

                if (ownersOnly && (String.IsNullOrEmpty(username) || !Settings.Bots_Owners.Contains(username.ToLower())))
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
                return Translations.Get(
                    "bot.autoRespond.match",
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
            if (File.Exists(matchesFile))
            {
                Regex matchRegex = null;
                string matchString = null;
                string matchAction = null;
                string matchActionPrivate = null;
                string matchActionOther = null;
                bool ownersOnly = false;
                TimeSpan cooldown = TimeSpan.Zero;
                respondRules = new List<RespondRule>();

                LogDebugToConsoleTranslated("bot.autoRespond.loading", System.IO.Path.GetFullPath(matchesFile));

                foreach (string lineRAW in File.ReadAllLines(matchesFile, Encoding.UTF8))
                {
                    string line = lineRAW.Split('#')[0].Trim();
                    if (line.Length > 0)
                    {
                        if (line[0] == '[' && line[line.Length - 1] == ']')
                        {
                            switch (line.Substring(1, line.Length - 2).ToLower())
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
                                string argValue = line.Substring(argName.Length + 1);
                                switch (argName.ToLower())
                                {
                                    case "regex": matchRegex = new Regex(argValue); break;
                                    case "match": matchString = argValue; break;
                                    case "action": matchAction = argValue; break;
                                    case "actionprivate": matchActionPrivate = argValue; break;
                                    case "actionother": matchActionOther = argValue; break;
                                    case "ownersonly": ownersOnly = Settings.str2bool(argValue); break;
                                    case "cooldown": cooldown = TimeSpan.FromSeconds(Settings.str2int(argValue)); break;
                                }
                            }
                        }
                    }
                }
                CheckAddMatch(matchRegex, matchString, matchAction, matchActionPrivate, matchActionOther, ownersOnly, cooldown);
            }
            else
            {
                LogToConsoleTranslated("bot.autoRespond.file_not_found", System.IO.Path.GetFullPath(matchesFile));
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
        private void CheckAddMatch(Regex matchRegex, string matchString, string matchAction, string matchActionPrivate, string matchActionOther, bool ownersOnly, TimeSpan cooldown)
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
                        respondRules.Add(rule);
                        LogDebugToConsoleTranslated("bot.autoRespond.loaded_match", rule);
                    }
                    else LogDebugToConsoleTranslated("bot.autoRespond.no_trigger", rule);
                }
                else LogDebugToConsoleTranslated("bot.autoRespond.no_action", rule);
            }
        }

        /// <summary>
        /// Process messages from the server and test them against all matches
        /// </summary>
        /// <param name="text">Text from the server</param>
        public override void GetText(string text)
        {
            //Remove colour codes
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
            if (msgType == MessageType.Other || sender != Settings.Username)
            {
                foreach (RespondRule rule in respondRules)
                {
                    Dictionary<string, object> localVars = new Dictionary<string, object>();
                    string toPerform = rule.Match(sender, message, msgType, localVars);
                    if (!String.IsNullOrEmpty(toPerform))
                    {
                        string response = null;
                        LogToConsoleTranslated("bot.autoRespond.match_run", toPerform);
                        PerformInternalCommand(toPerform, ref response, localVars);
                        if (!String.IsNullOrEmpty(response))
                            LogToConsole(response);
                    }
                }
            }
        }
    }
}
