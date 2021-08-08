using System;
using System.Collections.Generic;
using System.Net;

/// !!! ATTENTION !!!
/// By using these functions you agree to the ToS of the Mojang API.
/// You should note that all public APIs are rate limited so you are expected to cache the results. 
/// This is currently set at 600 requests per 10 minutes but this may change.
/// Source: https://wiki.vg/Mojang_API
/// !!! ATTENTION !!!

namespace MinecraftClient.Protocol
{
    /// <summary>
    /// Provides methods to easily interact with the Mojang API.
    /// </summary>
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

                    // Workaround for converting Unix time to normal time.
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

        /// <summary>
        /// Obtain links to skin, skinmodel and cape of a player.
        /// </summary>
        /// <param uuid="uuid">UUID of a player</param>
        /// <returns>Dictionary with a link to the skin and cape of a player.</returns>
        public static Dictionary<string, string> SkinInfo(string uuid)
        {
            Dictionary<string, string> tempDict = new Dictionary<string, string>();
            Dictionary<string, Json.JSONData> textureDict;
            string base64SkinInfo;
            Json.JSONData decodedJsonSkinInfo;

            // Perform web request
            try
            {
                // Obtain the Base64 encoded skin information from the API. Discard the rest, since it can be obtained easier through other requests.
                base64SkinInfo = Json.ParseJson(wc.DownloadString("https://sessionserver.mojang.com/session/minecraft/profile/" + uuid)).Properties["properties"].DataArray[0].Properties["value"].StringValue;
            }
            catch (Exception) { return tempDict; }

            // Parse the decoded string to the JSON format.
            decodedJsonSkinInfo = Json.ParseJson(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64SkinInfo)));

            // Assert temporary variable for readablity.
            // Contains skin and cape information.
            textureDict = decodedJsonSkinInfo.Properties["textures"].Properties;

            // Can apparently be missing, if no custom skin is set.
            // Probably for completely new accounts. 
            // (Still exists after changing back to Steve or Alex skin.)
            if (textureDict.ContainsKey("SKIN"))
            {
                // Add the URL leading to the texture of the ingame skin.
                tempDict.Add("SkinURL", textureDict["SKIN"].Properties.ContainsKey("url") ? textureDict["SKIN"].Properties["url"].StringValue : string.Empty);

                // Detect whether the playermodel is based on Steve or Alex.
                // If the skin property contains metadata, which always contains "slim", it is an Alex based skin.
                tempDict.Add("PlayerModel", textureDict["SKIN"].Properties.ContainsKey("metadata") ? "Alex" : "Steve");
            }
            // Tested it on several players, this case never occured.
            else
            {
                // This player has assumingly never changed their skin.
                // Probably a completely new account.
                tempDict.Add("SkinURL", string.Empty);
                tempDict.Add("PlayerModel", DefaultModelAlex(uuid) ? "Alex" : "Steve");
            }

            // If a cape exists, add it, otherwise leave string empty.
            tempDict.Add("CapeURL",
                textureDict.ContainsKey("CAPE") ? textureDict["CAPE"].Properties["url"].StringValue : string.Empty);

            return tempDict;
        }

        /// <summary>
        /// Gets the playermodel that is assigned to the account by default. 
        /// (Before the skin is changed for the first time.)
        /// </summary>
        /// <param name="uuid">UUID of a Player</param>
        /// <returns>True if the default model for this UUID is Alex</returns>
        public static bool DefaultModelAlex(string uuid)
        {
            return hashCode(uuid) % 2 == 1;
        }

        /// <summary>
        /// Creates the hash of an UUID
        /// </summary>
        /// <param name="hash">UUID of a player.</param>
        /// <returns></returns>
        private static int hashCode(string hash)
        {
            byte[] data = GuidExtensions.ToLittleEndian(new Guid(hash)).ToByteArray();

            ulong msb = 0;
            ulong lsb = 0;
            for (int i = 0; i < 8; i++)
                msb = (msb << 8) | (uint)(data[i] & 0xff);
            for (int i = 8; i < 16; i++)
                lsb = (lsb << 8) | (uint)(data[i] & 0xff);
            var hilo = msb ^ lsb;

            return ((int)(hilo >> 32)) ^ (int)hilo;
        }
    }
}
