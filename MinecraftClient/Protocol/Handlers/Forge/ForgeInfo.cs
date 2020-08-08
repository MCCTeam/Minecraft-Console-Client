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

        /// <summary>
        /// Create a new ForgeInfo from the given data.
        /// </summary>
        /// <param name="data">The modinfo JSON tag.</param>
        /// <exception cref="System.ArgumentException">Thrown on missing mod list in JSON data</exception>
        internal ForgeInfo(Json.JSONData data)
        {
            this.Mods = new List<ForgeMod>();
            bool listFound = false;

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

            if (data.Properties.ContainsKey("modList") && data.Properties["modList"].Type == Json.JSONData.DataType.Array)
            {
                listFound = true;

                foreach (Json.JSONData mod in data.Properties["modList"].DataArray)
                {
                    String modid = mod.Properties["modid"].StringValue;
                    String version = mod.Properties["version"].StringValue;

                    this.Mods.Add(new ForgeMod(modid, version));
                }
            }

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

            if (data.Properties.ContainsKey("mods") && data.Properties["mods"].Type == Json.JSONData.DataType.Array)
            {
                listFound = true;

                foreach (Json.JSONData mod in data.Properties["mods"].DataArray)
                {
                    String modid = mod.Properties["modId"].StringValue;
                    String version = mod.Properties["modmarker"].StringValue;

                    this.Mods.Add(new ForgeMod(modid, version));
                }
            }

            if (!listFound)
                throw new ArgumentException("Missing mod list", "data");
        }
    }
}
