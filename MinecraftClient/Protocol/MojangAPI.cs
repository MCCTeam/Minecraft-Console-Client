using System;
using System.Collections.Generic;
using System.Net;

namespace MinecraftClient.Protocol
{
    public static class MojangAPI
    {
        // Initialize webclient for all functions
        private static readonly WebClient wc = new WebClient();

        // Can be removed in newer C# versions.
        // Replace with DateTimeOffset.FromUnixTimeMilliseconds()
        /// <summary>
        /// Converts a Unix time to a normal Datetime
        /// </summary>
        /// <param name="unixTimeStamp">A unix timestamp as double</param>
        /// <returns>Datetime of unix timestamp</returns>
        private static DateTime unixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddMilliseconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }
        // Can be removed in newer C# versions.

        /// <summary>
        /// Obtain the UUID of a Player through its name
        /// </summary>
        /// <param name="name">Playername</param>
        /// <returns>UUID as string</returns>
        public static string NameToUuid(string name)
        {
            try
            {
                return Json.ParseJson(wc.DownloadString("https://api.mojang.com/users/profiles/minecraft/" + name)).Properties["id"].StringValue;
            }
            catch (Exception) { return string.Empty; }
        }

        /// <summary>
        /// Obtain the Name of a player through its UUID
        /// </summary>
        /// <param name="uuid">UUID of a player</param>
        /// <returns>Players UUID</returns>
        public static string UuidToCurrentName(string uuid)
        {
            // Perform web request
            try
            {
                var nameChanges = Json.ParseJson(wc.DownloadString("https://api.mojang.com/user/profiles/" + uuid + "/names")).DataArray;

                // Names are sorted from past to most recent. We need to get the last name in the list
                return nameChanges[nameChanges.Count - 1].Properties["name"].StringValue;
            }
            catch (Exception) { return string.Empty; }
        }

        /// <summary>
        /// Get the name history from a UUID
        /// </summary>
        /// <param name="uuid">UUID of a player</param>
        /// <returns>Name history, as a dictionary</returns>
        public static Dictionary<string, DateTime> UuidToNameHistory(string uuid)
        {
            Dictionary<string, DateTime> tempDict = new Dictionary<string, DateTime>();
            List<Json.JSONData> jsonDataList;

            // Perform web request
            try
            {
                jsonDataList = Json.ParseJson(wc.DownloadString("https://api.mojang.com/user/profiles/" + uuid + "/names")).DataArray;
            }
            catch (Exception) { return tempDict; }

            foreach (Json.JSONData jsonData in jsonDataList)
            {
                if (jsonData.Properties.Count > 1)
                {
                    // Time is saved as long in the Unix format.
                    // Convert it to normal time, before adding it to the dictionary.
                    //
                    // !! FromUnixTimeMilliseconds does not exist in the current version. !!
                    // DateTimeOffset creationDate = DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(jsonData.Properties["changedToAt"].StringValue));
                    //

                    // Workaround for converting Unix time to normal time
                    DateTimeOffset creationDate = unixTimeStampToDateTime(Convert.ToDouble(jsonData.Properties["changedToAt"].StringValue));

                    // Add Keyvaluepair to dict.
                    tempDict.Add(jsonData.Properties["name"].StringValue, creationDate.DateTime);
                }
                // The first entry does not contain a change date.
                else if (jsonData.Properties.Count > 0)
                {
                    // Add an undefined time to it.
                    tempDict.Add(jsonData.Properties["name"].StringValue, new DateTime());
                }
            }

            return tempDict;
        }

        /// <summary>
        /// Get the Mojang API status
        /// </summary>
        /// <returns>Dictionary of the Mojang services</returns>
        public static Dictionary<string, string> GetMojangServiceStatus()
        {
            Dictionary<string, string> tempDict = new Dictionary<string, string>();
            List<Json.JSONData> jsonDataList;

            // Perform web request
            try
            {
                jsonDataList = Json.ParseJson(wc.DownloadString("https://status.mojang.com/check")).DataArray;
            }
            catch (Exception) { return tempDict; }

            // Convert JSONData to string and parse it to a dictionary.
            foreach (Json.JSONData jsonData in jsonDataList)
            {
                if (jsonData.Properties.Count > 0)
                {
                    foreach (KeyValuePair<string, Json.JSONData> keyValuePair in jsonData.Properties)
                    {
                        // Service name to status
                        tempDict.Add(keyValuePair.Key, keyValuePair.Value.StringValue);
                    }
                }
            }

            return tempDict;
        }
    }
}
