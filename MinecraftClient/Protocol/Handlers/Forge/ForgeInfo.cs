using System;
using System.Collections.Generic;

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
                    Mods = new List<ForgeMod>
                    {
                        new ForgeMod("forge", "ANY")
                    };
                    Version = fmlVersion;
                    break;
                case FMLVersion.FML3:
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
                case FMLVersion.FML3:
                    // Example ModInfo for Minecraft 1.18 and greater (FML3)
                    
                    // "forgeData": {
                    //     "channels": [],
                    //     "mods": [],
                    //     "truncated": false, // legacy versions see truncated lists, modern versions ignore this truncated flag (binary data has its own)
                    //     "fmlNetworkVersion": 3,
                    //     "d": "ȳ\u0000\u0000ࠨ㐤獋㙖⹌ᦘ̺⸱恤䒸⡑⛧沮婙㨹牥ఈㄵচ₀沮婙㨹牥ఈㄵচ倠⹡岙㜲獥䋊㷍᭳ႇׇ஌᜘㘴娘▅筳ص䰭宛㘲、\u0000ᠸጋ囗湌夜㘲杩棐䐱ᅱ挃☥ోᤗ㌮ఀ׈䬣 坖ɍ䮌ᤘ\r\n旉䠳ዣ◆䲌㜃瑥廮ⷉࠋ–䁠奚Ҵ㔱摜䂸ᅱ獳ౠᡚ㜷汥戊䂸űဓĠ嵛㖱数嫤Ǎ塰䛶ⶎᮚ㞳晲擞ᖝ″ዣ䘆ఋʂ潦令ඕ爈䖔⺁ᥚ⾹潳棤㦥ᬻ挐؅䅀㠹楬ۨ㣄উ瀀渀嬛㘼扩搢䃀熁挂♥\r\n墋㒺摬牜ࣜ䁠嘗湌孛㜴浩惂䠙熙排٥孁㒰ͮ屢Ӏ䠐⚐䷮ᣛ㊴瑳戚䢸熁匒إ஍᜚ܴ䫜巑፻ᚷؠ䀀ㆃ牵䋨㦥ࠫ㋣䗆䂌㨈慲䫬ᖱᮓᘧ汬尚ㆰ٫屲㣄ᆉ恳ಭ川㤷፫擨妅挫♖乮塘 㖱慰\r\n囆䓩\t"
                    // }

                    // 1.18 and greater, the mod list and channel list is compressed to forgeData["d"] for efficiency,
                    // - Here is how forge encode and decode them:
                    // https://github.com/MinecraftForge/MinecraftForge/blob/cb12df41e13da576b781be695f80728b9594c25f/src/main/java/net/minecraftforge/network/ServerStatusPing.java#L264
                    // - Here is the discussion:
                    // see https://github.com/MinecraftForge/MinecraftForge/pull/8169

                    string encodedData = data.Properties["d"].StringValue;
                    Queue<byte> dataPackage = decodeOptimized(encodedData);
                    DataTypes dataTypes = new DataTypes(Protocol18Handler.MC_1_18_1_Version);

                    //
                    // [ Truncated ][       Bool     ] // Unused
                    // [  Mod Size ][ Unsigned short ]
                    // 
                    dataTypes.ReadNextBool(dataPackage); // truncated: boolean
                    var modsSize = dataTypes.ReadNextUShort(dataPackage);

                    Dictionary<string, string> mods = new();
                    // Mod Array Definition: 
                    // [ Channel Size And Version Flag ][      VarInt     ]  // If the value at bit Mask 0x01 is 1, The Mod Version will be ignore.
                    //                                                       // The one-right-shifted int is the Channel List size.
                    // [             Mod Id            ][      String     ]
                    // [          Mod Version          ][ Optional String ]  // Depends on the Flag above
                    // [         Channel List          ][      Array      ] [    Channel Name    ][ String ]
                    //                                                      [   Channel Version  ][ String ]
                    //                                                      [ Required On Client ][  Bool  ]

                    for (var i = 0; i < modsSize; i++) {
                        var channelSizeAndVersionFlag = dataTypes.ReadNextVarInt(dataPackage);
                        var channelSize = channelSizeAndVersionFlag >> 1;

                        int VERSION_FLAG_IGNORESERVERONLY = 0b1;
                        var isIgnoreServerOnly = (channelSizeAndVersionFlag & VERSION_FLAG_IGNORESERVERONLY) != 0;
                        
                        var modId = dataTypes.ReadNextString(dataPackage);
                        
                        string IGNORESERVERONLY = "IGNORED";
                        var modVersion = isIgnoreServerOnly ? IGNORESERVERONLY : dataTypes.ReadNextString(dataPackage);
                    
                        for (var i1 = 0; i1 < channelSize; i1++) {
                            dataTypes.ReadNextString(dataPackage); // channelName
                            dataTypes.ReadNextString(dataPackage); // channelVersion
                            dataTypes.ReadNextBool(dataPackage); // requiredOnClient
                        }
 
                        mods.Add(modId, modVersion);
                        Mods.Add(new ForgeMod(modId, modVersion));
                    }

                    // Ignore the left data, which is NonMod Channel List
                    // [ nonMod Channel Count ][ VarInt ]
                    // [ nonMod Channel List  ][ Array ] [    Channel Name    ][ String ]
                    //                                   [   Channel Version  ][  Bool  ]
                    //                                   [ Required On Client ][  Bool  ]

                    break;
                default:
                    throw new NotImplementedException("FMLVersion '" + fmlVersion + "' not implemented!");
            }
        }

        /// <summary>
        /// Decompress binary data ForgeData["d"] (FML 3)
        /// </summary>
        /// <param name="encodedData">The encoded data.</param>
        /// <returns>Decoded forge data Queue<byte>.</returns>
        /// <para>
        /// 1.18 and greater, the mod list and channel list is compressed for efficiency
        /// The code below is converted from forge source code, see:
        /// https://github.com/MinecraftForge/MinecraftForge/blob/cb12df41e13da576b781be695f80728b9594c25f/src/main/java/net/minecraftforge/network/ServerStatusPing.java#L361
        /// </para>
        private static Queue<byte> decodeOptimized(string encodedData) {
            int size0 = encodedData[0];
            int size1 = encodedData[1];
            int size = size0 | (size1 << 15);

            List<byte> packageData = new();

            int stringIndex = 2;
            int buffer = 0;
            int bitsInBuf = 0;

            while (stringIndex < encodedData.Length)
            {
                while (bitsInBuf >= 8)
                {
                    packageData.Add((byte)buffer);
                    buffer >>= 8;
                    bitsInBuf -= 8;
                }

                char c = encodedData[stringIndex];
                buffer |= (c & 0x7FFF) << bitsInBuf;
                bitsInBuf += 15;
                stringIndex++;
            }

            while (packageData.Count < size)
            {
                packageData.Add((byte)buffer);
                buffer >>= 8;
                bitsInBuf -= 8;
            }

            return new Queue<byte>(packageData.ToArray());
        }
    }
}
