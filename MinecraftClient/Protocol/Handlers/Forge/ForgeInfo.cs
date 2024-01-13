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
                    
                    //  {
                    //      "enforcesSecureChat": true,
                    //      "forgeData": {
                    //          "channels": [],
                    //          "mods": [],
                    //          "truncated": false, // legacy versions see truncated lists, modern versions ignore this truncated flag (binary data has its own)
                    //          "fmlNetworkVersion": 3,
                    //          "d": "ȳ\u0000\u0000ࠨ㐤獋㙖⹌ᦘ̺⸱恤䒸⡑⛧沮婙㨹牥ఈㄵচ₀沮婙㨹牥ఈㄵচ倠⹡岙㜲獥䋊㷍᭳ႇׇ஌᜘㘴娘▅筳ص䰭宛㘲、\u0000ᠸጋ囗湌夜㘲杩棐䐱ᅱ挃☥ోᤗ㌮ఀ׈䬣 坖ɍ䮌ᤘ\r\n旉䠳ዣ◆䲌㜃瑥廮ⷉࠋ–䁠奚Ҵ㔱摜䂸ᅱ獳ౠᡚ㜷汥戊䂸űဓĠ嵛㖱数嫤Ǎ塰䛶ⶎᮚ㞳晲擞ᖝ″ዣ䘆ఋʂ潦令ඕ爈䖔⺁ᥚ⾹潳棤㦥ᬻ挐؅䅀㠹楬ۨ㣄উ瀀渀嬛㘼扩搢䃀熁挂♥\r\n墋㒺摬牜ࣜ䁠嘗湌孛㜴浩惂䠙熙排٥孁㒰ͮ屢Ӏ䠐⚐䷮ᣛ㊴瑳戚䢸熁匒إ஍᜚ܴ䫜巑፻ᚷؠ䀀ㆃ牵䋨㦥ࠫ㋣䗆䂌㨈慲䫬ᖱᮓᘧ汬尚ㆰ٫屲㣄ᆉ恳ಭ川㤷፫擨妅挫♖乮塘 㖱慰\r\n囆䓩\t"
                    //      },
                    //      "description": {
                    //          "text": "A Minecraft Server"
                    //      },
                    //      "players": {
                    //          "max": 100,
                    //          "online": 0
                    //      },
                    //      "version": {
                    //          "name": "1.20.1",
                    //          "protocol": 763
                    //      }
                    //  }

                    // All buffer data are encoded and write to forgeData["d"]
                    // https://github.com/MinecraftForge/MinecraftForge/blob/cb12df41e13da576b781be695f80728b9594c25f/src/main/java/net/minecraftforge/network/ServerStatusPing.java#L264
                    
                    // 1.18 and greater, the buffer is encoded for efficiency
                    // see https://github.com/MinecraftForge/MinecraftForge/pull/8169

                    string encodedData = data.Properties["d"].StringValue;
                    Queue<byte> dataPackage = decodeOptimized(encodedData);
                    DataTypes dataTypes = new DataTypes(Protocol18Handler.MC_1_18_1_Version);

                    //
                    // [truncated][boolean] placeholder for whether we are truncating
                    // [Mod Size][unsigned short] short so that we can replace it later in case of truncation
                    // 
                    bool truncated = dataTypes.ReadNextBool(dataPackage);
                    var modsSize = dataTypes.ReadNextUShort(dataPackage);

                    Dictionary<string, string> channels = new();
                    Dictionary<string, string> mods = new();

                    for (var i = 0; i < modsSize; i++) {
                        var channelSizeAndVersionFlag = dataTypes.ReadNextVarInt(dataPackage);
                        var channelSize = channelSizeAndVersionFlag >> 1;

                        int VERSION_FLAG_IGNORESERVERONLY = 0b1;
                        var isIgnoreServerOnly = (channelSizeAndVersionFlag & VERSION_FLAG_IGNORESERVERONLY) != 0;
                        
                        var modId = dataTypes.ReadNextString(dataPackage);
                        
                        string IGNORESERVERONLY = "";// it was "OHNOES\uD83D\uDE31\uD83D\uDE31\uD83D\uDE31\uD83D\uDE31\uD83D\uDE31\uD83D\uDE31\uD83D\uDE31\uD83D\uDE31\uD83D\uDE31\uD83D\uDE31\uD83D\uDE31\uD83D\uDE31\uD83D\uDE31\uD83D\uDE31\uD83D\uDE31\uD83D\uDE31\uD83D\uDE31";
                        var modVersion = isIgnoreServerOnly ? IGNORESERVERONLY : dataTypes.ReadNextString(dataPackage);
                    
                        for (var i1 = 0; i1 < channelSize; i1++) {
                            var channelName = dataTypes.ReadNextString(dataPackage);
                            var channelVersion = dataTypes.ReadNextString(dataPackage);
                            var requiredOnClient = dataTypes.ReadNextBool(dataPackage);
                            channels.Add(modId + ":" + channelName, channelVersion + ":" + requiredOnClient);
                        }
 
                        mods.Add(modId, modVersion);
                        Mods.Add(new ForgeMod(modId, modVersion));
                    }

                    var nonModChannelCount = dataTypes.ReadNextVarInt(dataPackage);
                    for (var i = 0; i < nonModChannelCount; i++) {
                        var channelName = dataTypes.ReadNextString(dataPackage);
                        var channelVersion = dataTypes.ReadNextString(dataPackage);
                        var requiredOnClient = dataTypes.ReadNextBool(dataPackage);
                        channels.Add(channelName, channelVersion + ":" + requiredOnClient);
                    }

                    break;
                default:
                    throw new NotImplementedException("FMLVersion '" + fmlVersion + "' not implemented!");
            }
        }

        // https://github.com/MinecraftForge/MinecraftForge/blob/cb12df41e13da576b781be695f80728b9594c25f/src/main/java/net/minecraftforge/network/ServerStatusPing.java#L361
        // Decode binary data ForgeData["d"] to Queue<byte>
        private static Queue<byte> decodeOptimized(string encodedData) {
            // Console.WriteLine("Got encoded data:" + encodedData + ", decoding...");
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
