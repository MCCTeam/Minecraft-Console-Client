using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        public class ForgeMod
        {
            public ForgeMod(String ModID, String Version)
            {
                this.ModID = ModID;
                this.Version = Version;
            }

            public readonly String ModID;
            public readonly String Version;

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
                    this.Mods = new List<ForgeMod>();
                    this.Mods.Add(new ForgeMod("forge", "ANY"));
                    this.Version = fmlVersion;
                    break;
                default:
                    throw new InvalidOperationException(Translations.Get("error.forgeforce"));
            }
        }

        /// <summary>
        /// Create a new ForgeInfo from the given data.
        /// </summary>
        /// <param name="data">The modinfo JSON tag.</param>
        /// <param name="fmlVersion">Forge protocol version</param>
        internal ForgeInfo(Json.JSONData data, FMLVersion fmlVersion)
        {
            this.Mods = new List<ForgeMod>();
            this.Version = fmlVersion;

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

                        this.Mods.Add(new ForgeMod(modid, modversion));
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

                        this.Mods.Add(new ForgeMod(modid, modmarker));
                    }

                    break;

                default:
                    throw new NotImplementedException("FMLVersion '" + fmlVersion + "' not implemented!");
            }
        }
    }
}
