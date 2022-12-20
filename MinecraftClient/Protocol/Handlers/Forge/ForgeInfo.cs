using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MinecraftClient.Protocol.Handlers.Forge
{
    /// <summary>
    /// Contains information about a modded server install.
    /// </summary>
    public class ForgeInfo
    {
        /// <summary>
        /// Represents an individual forge mod.
        /// </summary>
        public record ForgeMod
        {
            public ForgeMod(string? modID, string? version)
            {
                ModID = modID;
                Version = ModMarker = version;
            }

            [JsonPropertyName("modId")]
            public string? ModID { init; get; }

            [JsonPropertyName("version")]
            public string? Version { init; get; }

            [JsonPropertyName("modmarker")]
            public string? ModMarker { init; get; }

            public override string ToString()
            {
                return ModID + " v" + Version;
            }
        }

        public List<ForgeMod> Mods;
        internal FMLVersion Version;

        /// <summary>
        /// Create a new ForgeInfo with the given version.
        /// </summary>
        /// <param name="fmlVersion">FML version to use</param>
        internal ForgeInfo(FMLVersion fmlVersion)
        {
            switch (fmlVersion)
            {
                case FMLVersion.FML2:
                    Mods = new List<ForgeMod>
                    {
                        new ForgeMod("forge", "ANY")
                    };
                    Version = fmlVersion;
                    break;
                default:
                    throw new InvalidOperationException(Translations.error_forgeforce);
            }
        }

        /// <summary>
        /// Create a new ForgeInfo from the given data.
        /// </summary>
        /// <param name="data">The modinfo JSON tag.</param>
        /// <param name="fmlVersion">Forge protocol version</param>
        internal ForgeInfo(Json.JSONData data, FMLVersion fmlVersion)
        {
            Mods = new List<ForgeMod>();
            Version = fmlVersion;

            switch (fmlVersion)
            {
                case FMLVersion.FML:

                    // Example ModInfo for Minecraft 1.12 and lower (FML)

                    // "modinfo": {
                    //     "type": "FML",
                    //     "modList": [{
                    //         "modid": "mcp",
                    //         "version": "9.05"
                    //     }, {
                    //         "modid": "FML",
                    //         "version": "8.0.99.99"
                    //     }, {
                    //         "modid": "Forge",
                    //         "version": "11.14.3.1512"
                    //     }, {
                    //         "modid": "rpcraft",
                    //         "version": "Beta 1.3 - 1.8.0"
                    //     }]
                    // }

                    foreach (Json.JSONData mod in data.Properties["modList"].DataArray)
                    {
                        String modid = mod.Properties["modid"].StringValue;
                        String modversion = mod.Properties["version"].StringValue;

                        Mods.Add(new ForgeMod(modid, modversion));
                    }

                    break;

                case FMLVersion.FML2:

                    // Example ModInfo for Minecraft 1.13 and greater (FML2)

                    // "forgeData": {
                    //     "channels": [{
                    //         "res": "minecraft:unregister",
                    //         "version": "FML2",
                    //         "required": true
                    //     }, {
                    //         "res": "minecraft:register",
                    //         "version": "FML2",
                    //         "required": true
                    //     }],
                    //     "mods": [{
                    //         "modId": "minecraft",
                    //         "modmarker": "1.15.2"
                    //     }, {
                    //         "modId": "forge",
                    //         "modmarker": "ANY"
                    //     }, {
                    //         "modId": "rats",
                    //         "modmarker": "5.3.2"
                    //     }, {
                    //         "modId": "citadel",
                    //         "modmarker": "1.1.11"
                    //     }],
                    //     "fmlNetworkVersion": 2
                    // }

                    foreach (Json.JSONData mod in data.Properties["mods"].DataArray)
                    {
                        String modid = mod.Properties["modId"].StringValue;
                        String modmarker = mod.Properties["modmarker"].StringValue;

                        Mods.Add(new ForgeMod(modid, modmarker));
                    }

                    break;

                default:
                    throw new NotImplementedException("FMLVersion '" + fmlVersion + "' not implemented!");
            }
        }

        /// <summary>
        /// Create a new ForgeInfo from the given data.
        /// </summary>
        /// <param name="data">The modinfo JSON tag.</param>
        /// <param name="fmlVersion">Forge protocol version</param>
        internal ForgeInfo(ForgeMod[] mods, FMLVersion fmlVersion)
        {
            Mods = new(mods);
            Version = fmlVersion;
        }
    }
}
