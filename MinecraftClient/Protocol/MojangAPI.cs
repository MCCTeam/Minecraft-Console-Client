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
    // Enum to display the status of different services
    public enum ServiceStatus { red, yellow, green };

    /// <summary>
    /// Information about a players Skin.
    /// Empty string if not available.
    /// </summary>
    public class SkinInfo
    {
        public readonly string SkinUrl;
        public readonly string CapeUrl;
        public readonly string SkinModel;

        public SkinInfo(string skinUrl = "", string capeUrl = "", string skinModel = "")
        {
            SkinUrl = skinUrl;
            CapeUrl = capeUrl;
            SkinModel = skinModel;
        }
    }

    /// <summary>
    /// Status of the single Mojang services
    /// </summary>
    public class MojangServiceStatus
    {
        public readonly ServiceStatus MinecraftNet;
        public readonly ServiceStatus SessionMinecraftNet;
        public readonly ServiceStatus AccountMojangCom;
        public readonly ServiceStatus AuthserverMojangCom;
        public readonly ServiceStatus SessionserverMojangCom;
        public readonly ServiceStatus ApiMojangCom;
        public readonly ServiceStatus TexturesMinecraftNet;
        public readonly ServiceStatus MojangCom;

        public MojangServiceStatus(ServiceStatus minecraftNet = ServiceStatus.red,
            ServiceStatus sessionMinecraftNet = ServiceStatus.red,
            ServiceStatus accountMojangCom = ServiceStatus.red,
            ServiceStatus authserverMojangCom = ServiceStatus.red,
            ServiceStatus sessionserverMojangCom = ServiceStatus.red,
            ServiceStatus apiMojangCom = ServiceStatus.red,
            ServiceStatus texturesMinecraftNet = ServiceStatus.red,
            ServiceStatus mojangCom = ServiceStatus.red)
        {
            MinecraftNet = minecraftNet;
            SessionMinecraftNet = sessionMinecraftNet;
            AccountMojangCom = accountMojangCom;
            AuthserverMojangCom = authserverMojangCom;
            SessionserverMojangCom = sessionserverMojangCom;
            ApiMojangCom = apiMojangCom;
            TexturesMinecraftNet = texturesMinecraftNet;
            MojangCom = mojangCom;
        }
    }

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
        /// Converts a string to a ServiceStatus enum.
        /// </summary>
        /// <param name="s">string to convert</param>
        /// <returns>ServiceStatus enum, red as default.</returns>
        private static ServiceStatus stringToServiceStatus(string s)
        {
            ServiceStatus servStat;

            if (Enum.TryParse(s, out servStat))
            {
                return servStat;
            }
            else
            {
                // return red as standard value.
                return ServiceStatus.red;
            }
        }

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
        public static MojangServiceStatus GetMojangServiceStatus()
        {
            List<Json.JSONData> jsonDataList = new List<Json.JSONData>();

            // Perform web request
            try
            {
                jsonDataList = Json.ParseJson(wc.DownloadString("https://status.mojang.com/check")).DataArray;
            }
            catch (Exception) { new MojangServiceStatus(); }

            // Convert string to enum values and store them inside a MojangeServiceStatus object.
            return new MojangServiceStatus(minecraftNet: stringToServiceStatus(jsonDataList[0].Properties["minecraft.net"].StringValue),
                sessionMinecraftNet: stringToServiceStatus(jsonDataList[1].Properties["session.minecraft.net"].StringValue),
                accountMojangCom: stringToServiceStatus(jsonDataList[2].Properties["account.mojang.com"].StringValue),
                authserverMojangCom: stringToServiceStatus(jsonDataList[3].Properties["authserver.mojang.com"].StringValue),
                sessionserverMojangCom: stringToServiceStatus(jsonDataList[4].Properties["sessionserver.mojang.com"].StringValue),
                apiMojangCom: stringToServiceStatus(jsonDataList[5].Properties["api.mojang.com"].StringValue),
                texturesMinecraftNet: stringToServiceStatus(jsonDataList[6].Properties["textures.minecraft.net"].StringValue),
                mojangCom: stringToServiceStatus(jsonDataList[7].Properties["mojang.com"].StringValue)
                );
        }

        /// <summary>
        /// Obtain links to skin, skinmodel and cape of a player.
        /// </summary>
        /// <param uuid="uuid">UUID of a player</param>
        /// <returns>Dictionary with a link to the skin and cape of a player.</returns>
        public static SkinInfo GetSkinInfo(string uuid)
        {
            Dictionary<string, Json.JSONData> textureDict;
            string base64SkinInfo;
            Json.JSONData decodedJsonSkinInfo;

            // Perform web request
            try
            {
                // Obtain the Base64 encoded skin information from the API. Discard the rest, since it can be obtained easier through other requests.
                base64SkinInfo = Json.ParseJson(wc.DownloadString("https://sessionserver.mojang.com/session/minecraft/profile/" + uuid)).Properties["properties"].DataArray[0].Properties["value"].StringValue;
            }
            catch (Exception) { return new SkinInfo(); }

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
                return new SkinInfo(skinUrl: textureDict["SKIN"].Properties.ContainsKey("url") ? textureDict["SKIN"].Properties["url"].StringValue : string.Empty,
                    capeUrl: textureDict.ContainsKey("CAPE") ? textureDict["CAPE"].Properties["url"].StringValue : string.Empty,
                    skinModel: textureDict["SKIN"].Properties.ContainsKey("metadata") ? "Alex" : "Steve");
            }
            // Tested it on several players, this case never occured.
            else
            {
                // This player has assumingly never changed their skin.
                // Probably a completely new account.
                return new SkinInfo(capeUrl: textureDict.ContainsKey("CAPE") ? textureDict["CAPE"].Properties["url"].StringValue : string.Empty,
                    skinModel: DefaultModelAlex(uuid) ? "Alex" : "Steve");
            }
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
