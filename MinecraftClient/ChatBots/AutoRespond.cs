using System;
using System.IO;
using System.Collections.Generic;
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

            /// <summary>
            /// Create a respond rule from a regex and a reponse message or command
            /// </summary>
            /// <param name="regex">Regex</param>
            /// <param name="actionPublic">Internal command to run for public messages</param>
            /// <param name="actionPrivate">Internal command to run for private messages</param>
            /// <param name="actionOther">Internal command to run for any other messages</param>
            /// <param name="ownersOnly">Only match messages from bot owners</param>
            public RespondRule(Regex regex, string actionPublic, string actionPrivate, string actionOther, bool ownersOnly)
            {
                this.regex = regex;
                this.match = null;
                this.actionPublic = actionPublic;
                this.actionPrivate = actionPrivate;
                this.actionOther = actionOther;
                this.ownersOnly = ownersOnly;
            }

            /// <summary>
            /// Create a respond rule from a match string and a reponse message or command
            /// </summary>
            /// <param name="match">Match string</param>
            /// <param name="actionPublic">Internal command to run for public messages</param>
            /// <param name="actionPrivate">Internal command to run for private messages</param>
            /// <param name="ownersOnly">Only match messages from bot owners</param>
            public RespondRule(string match, string actionPublic, string actionPrivate, string actionOther, bool ownersOnly)
            {
                this.regex = null;
                this.match = match;
                this.actionPublic = actionPublic;
                this.actionPrivate = actionPrivate;
                this.actionOther = actionOther;
                this.ownersOnly = ownersOnly;
            }

            /// <summary>
            /// Match the respond rule to the specified string and return a message or command to send if a match is detected
            /// </summary>
            /// <param name="username">Player who have sent the message</param>
            /// <param name="message">Message to match against the regex or match string</param>
            /// <param name="msgType">Type of the message public/private message, or other message</param>
            /// <returns>Internal command to run as a response to this user, or null if no match has been detected</returns>
            public string Match(string username, string message, MessageType msgType)
            {
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
                        Match regexMatch = regex.Match(message);
                        for (int i = regexMatch.Groups.Count - 1; i >= 1; i--)
                            toSend = toSend.Replace("$" + i, regexMatch.Groups[i].Value);
                        toSend = toSend.Replace("$u", username);
                        return toSend;
                    }
                }
                else if (!String.IsNullOrEmpty(match))
                {
                    if (message.ToLower().Contains(match.ToLower()))
                    {
                        return toSend.Replace("$u", username);
                    }
                }

                return null;
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
                respondRules = new List<RespondRule>();

                foreach (string lineRAW in File.ReadAllLines(matchesFile))
                {
                    string line = lineRAW.Split('#')[0].Trim();
                    if (line.Length > 0)
                    {
                        if (line[0] == '[' && line[line.Length - 1] == ']')
                        {
                            switch (line.Substring(1, line.Length - 2).ToLower())
                            {
                                case "match":
                                    CheckAddMatch(matchRegex, matchString, matchAction, matchActionPrivate, matchActionOther, ownersOnly);
                                    matchRegex = null;
                                    matchString = null;
                                    matchAction = null;
                                    matchActionPrivate = null;
                                    matchActionOther = null;
                                    ownersOnly = false;
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
                                }
                            }
                        }
                    }
                }
                CheckAddMatch(matchRegex, matchString, matchAction, matchActionPrivate, matchActionOther, ownersOnly);
            }
            else
            {
                LogToConsole("File not found: '" + matchesFile + "'");
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
        private void CheckAddMatch(Regex matchRegex, string matchString, string matchAction, string matchActionPrivate, string matchActionOther, bool ownersOnly)
        {
            if (matchAction != null || matchActionPrivate != null || matchActionOther != null)
            {
                if (matchRegex != null)
                {
                    respondRules.Add(new RespondRule(matchRegex, matchAction, matchActionPrivate, matchActionOther, ownersOnly));
                }
                else if (matchString != null)
                {
                    respondRules.Add(new RespondRule(matchString, matchAction, matchActionPrivate, matchActionOther, ownersOnly));
                }
            }
        }

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
                    string toPerform = rule.Match(sender, message, msgType);
                    if (!String.IsNullOrEmpty(toPerform))
                    {
                        string response = null;
                        LogToConsole(toPerform);
                        PerformInternalCommand(toPerform, ref response);
                        if (!String.IsNullOrEmpty(response))
                            LogToConsole(response);
                    }
                }
            }
        }
    }
}
