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
        internal ForgeInfo(Json.JSONData data)
        {
            // Example ModInfo (with spacing):

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

            this.Mods = new List<ForgeMod>();
            foreach (Json.JSONData mod in data.Properties["modList"].DataArray)
            {
                String modid = mod.Properties["modid"].StringValue;
                String version = mod.Properties["version"].StringValue;

                this.Mods.Add(new ForgeMod(modid, version));
            }
        }
    }
}
