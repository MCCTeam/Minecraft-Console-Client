using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

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
    public record SkinInfo(string SkinUrl = "", string CapeUrl = "", string SkinModel = "");

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
        private static readonly HttpClient httpClient = new();

        // Can be removed in newer C# versions.
        // Replace with DateTimeOffset.FromUnixTimeMilliseconds()
        /// <summary>
        /// Converts a Unix time to a normal Datetime
        /// </summary>
        /// <param name="unixTimeStamp">A unix timestamp as double</param>
        /// <returns>Datetime of unix timestamp</returns>
        private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddMilliseconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }
        // Can be removed in newer C# versions.

        /// <summary>
        /// Converts a string to a ServiceStatus enum.
        /// </summary>
        /// <param name="s">string to convert</param>
        /// <returns>ServiceStatus enum, red as default.</returns>
        private static ServiceStatus StringToServiceStatus(string s)
        {

            if (Enum.TryParse(s, out ServiceStatus servStat))
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
                Task<string> fetchTask = httpClient.GetStringAsync("https://api.mojang.com/users/profiles/minecraft/" + name);
                fetchTask.Wait();
                string result = Json.ParseJson(fetchTask.Result)!["id"]!.GetStringValue();
                fetchTask.Dispose();
                return result;
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
                Task<string> fetchTask = httpClient.GetStringAsync("https://api.mojang.com/user/profiles/" + uuid + "/names");
                fetchTask.Wait();
                var nameChanges = Json.ParseJson(fetchTask.Result)!.AsArray();
                fetchTask.Dispose();

                // Names are sorted from past to most recent. We need to get the last name in the list
                return nameChanges[^1]!["name"]!.GetStringValue();
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
            Dictionary<string, DateTime> tempDict = new();
            System.Text.Json.Nodes.JsonArray jsonDataList;

            // Perform web request
            try
            {
                Task<string> fetchTask = httpClient.GetStringAsync("https://api.mojang.com/user/profiles/" + uuid + "/names");
                fetchTask.Wait();
                jsonDataList = Json.ParseJson(fetchTask.Result)!.AsArray();
                fetchTask.Dispose();
            }
            catch (Exception) { return tempDict; }

            foreach (var jsonData in jsonDataList)
            {
                var obj = jsonData!.AsObject();
                if (obj.Count > 1)
                {
                    DateTimeOffset creationDate = UnixTimeStampToDateTime(Convert.ToDouble(jsonData["changedToAt"].GetStringValue()));

                    tempDict.Add(jsonData["name"]!.GetStringValue(), creationDate.DateTime);
                }
                // The first entry does not contain a change date.
                else if (obj.Count > 0)
                {
                    // Add an undefined time to it.
                    tempDict.Add(jsonData["name"]!.GetStringValue(), new DateTime());
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
            System.Text.Json.Nodes.JsonArray jsonDataList;

            // Perform web request
            try
            {
                Task<string> fetchTask = httpClient.GetStringAsync("https://status.mojang.com/check");
                fetchTask.Wait();
                jsonDataList = Json.ParseJson(fetchTask.Result)!.AsArray();
                fetchTask.Dispose();
            }
            catch (Exception)
            {
                return new MojangServiceStatus();
            }

            // Convert string to enum values and store them inside a MojangeServiceStatus object.
            return new MojangServiceStatus(minecraftNet: StringToServiceStatus(jsonDataList[0]!["minecraft.net"]!.GetStringValue()),
                sessionMinecraftNet: StringToServiceStatus(jsonDataList[1]!["session.minecraft.net"]!.GetStringValue()),
                accountMojangCom: StringToServiceStatus(jsonDataList[2]!["account.mojang.com"]!.GetStringValue()),
                authserverMojangCom: StringToServiceStatus(jsonDataList[3]!["authserver.mojang.com"]!.GetStringValue()),
                sessionserverMojangCom: StringToServiceStatus(jsonDataList[4]!["sessionserver.mojang.com"]!.GetStringValue()),
                apiMojangCom: StringToServiceStatus(jsonDataList[5]!["api.mojang.com"]!.GetStringValue()),
                texturesMinecraftNet: StringToServiceStatus(jsonDataList[6]!["textures.minecraft.net"]!.GetStringValue()),
                mojangCom: StringToServiceStatus(jsonDataList[7]!["mojang.com"]!.GetStringValue())
                );
        }

        /// <summary>
        /// Obtain links to skin, skinmodel and cape of a player.
        /// </summary>
        /// <param uuid="uuid">UUID of a player</param>
        /// <returns>Dictionary with a link to the skin and cape of a player.</returns>
        public static SkinInfo GetSkinInfo(string uuid)
        {
            System.Text.Json.Nodes.JsonObject textureObj;
            string base64SkinInfo;
            System.Text.Json.Nodes.JsonNode? decodedJsonSkinInfo;

            // Perform web request
            try
            {
                Task<string> fetchTask = httpClient.GetStringAsync("https://sessionserver.mojang.com/session/minecraft/profile/" + uuid);
                fetchTask.Wait();
                // Obtain the Base64 encoded skin information from the API. Discard the rest, since it can be obtained easier through other requests.
                base64SkinInfo = Json.ParseJson(fetchTask.Result)!["properties"]![0]!["value"]!.GetStringValue();
                fetchTask.Dispose();
            }
            catch (Exception) { return new SkinInfo(); }

            // Parse the decoded string to the JSON format.
            decodedJsonSkinInfo = Json.ParseJson(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64SkinInfo)));

            // Assert temporary variable for readablity.
            // Contains skin and cape information.
            textureObj = decodedJsonSkinInfo!["textures"]!.AsObject();

            // Can apparently be missing, if no custom skin is set.
            if (textureObj.ContainsKey("SKIN"))
            {
                return new SkinInfo(SkinUrl: textureObj["SKIN"]!["url"] is not null ? textureObj["SKIN"]!["url"]!.GetStringValue() : string.Empty,
                    CapeUrl: textureObj.ContainsKey("CAPE") ? textureObj["CAPE"]!["url"]!.GetStringValue() : string.Empty,
                    SkinModel: textureObj["SKIN"]!["metadata"] is not null ? "Alex" : "Steve");
            }
            else
            {
                return new SkinInfo(CapeUrl: textureObj.ContainsKey("CAPE") ? textureObj["CAPE"]!["url"]!.GetStringValue() : string.Empty,
                    SkinModel: DefaultModelAlex(uuid) ? "Alex" : "Steve");
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
            return HashCode(uuid) % 2 == 1;
        }

        /// <summary>
        /// Creates the hash of an UUID
        /// </summary>
        /// <param name="hash">UUID of a player.</param>
        /// <returns></returns>
        private static int HashCode(string hash)
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
