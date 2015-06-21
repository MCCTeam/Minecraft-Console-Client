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

            /// <summary>
            /// Create a respond rule from a regex and a reponse message or command
            /// </summary>
            /// <param name="regex">Regex</param>
            /// <param name="actionPublic">Internal command to run for public messages</param>
            /// <param name="actionPrivate">Internal command to run for private messages</param>
            public RespondRule(Regex regex, string actionPublic, string actionPrivate)
            {
                this.regex = regex;
                this.match = null;
                this.actionPublic = actionPublic;
                this.actionPrivate = actionPrivate;
            }

            /// <summary>
            /// Create a respond rule from a match string and a reponse message or command
            /// </summary>
            /// <param name="match">Match string</param>
            /// <param name="actionPublic">Internal command to run for public messages</param>
            /// <param name="actionPrivate">Internal command to run for private messages</param>
            public RespondRule(string match, string actionPublic, string actionPrivate)
            {
                this.regex = null;
                this.match = match;
                this.actionPublic = actionPublic;
                this.actionPrivate = actionPrivate;
            }

            /// <summary>
            /// Match the respond rule to the specified string and return a message or command to send if a match is detected
            /// </summary>
            /// <param name="username">Player who have sent the message</param>
            /// <param name="message">Message to match against the regex or match string</param>
            /// <param name="privateMsg">True if the provided message was sent privately eg with /tell</param>
            /// <returns>Internal command to run as a response to this user, or null if no match has been detected</returns>
            public string Match(string username, string message, bool privateMsg)
            {
                if (regex != null)
                {
                    if (regex.IsMatch(message))
                    {
                        Match regexMatch = regex.Match(message);
                        string toSend = privateMsg ? actionPrivate : actionPublic;
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
                        return (privateMsg
                                ? actionPrivate
                                : actionPublic).Replace("$u", username);
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
                                    CheckAddMatch(matchRegex, matchString, matchAction, matchActionPrivate);
                                    matchRegex = null;
                                    matchString = null;
                                    matchAction = null;
                                    matchActionPrivate = null;
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
                                }
                            }
                        }
                    }
                }
                CheckAddMatch(matchRegex, matchString, matchAction, matchActionPrivate);
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
        private void CheckAddMatch(Regex matchRegex, string matchString, string matchAction, string matchActionPrivate)
        {
            if (matchAction != null || matchActionPrivate != null)
            {
                if (matchActionPrivate == null)
                {
                    matchActionPrivate = matchAction;
                }

                if (matchRegex != null)
                {
                    respondRules.Add(new RespondRule(matchRegex, matchAction, matchActionPrivate));
                }
                else if (matchString != null)
                {
                    respondRules.Add(new RespondRule(matchString, matchAction, matchActionPrivate));
                }
            }
        }

        public override void GetText(string text)
        {
            //Remove colour codes
            text = GetVerbatim(text);

            //Check if this is a valid message
            string sender = "", message = "";
            bool chatMessage = IsChatMessage(text, ref message, ref sender);
            bool privateMessage = false;
            if (!chatMessage)
                privateMessage = IsPrivateMessage(text, ref message, ref sender);

            //Process only chat messages sent by another user
            if ((chatMessage || privateMessage) && sender != Settings.Username)
            {
                foreach (RespondRule rule in respondRules)
                {
                    string toPerform = rule.Match(sender, message, privateMessage);
                    if (toPerform != null)
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
