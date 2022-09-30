using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.ChatBots
{
    class Map : ChatBot
    {
        private readonly string baseDirectory = @"Rendered_Maps";

        private readonly Dictionary<int, McMap> cachedMaps = new();
        private bool shouldResize = true;
        private int resizeTo = 256;
        private bool autoRenderOnUpdate = true;
        private bool deleteAllOnExit = true;
        private bool notifyOnFirstUpdate = true;

        public override void Initialize()
        {
            if (!Directory.Exists(baseDirectory))
                Directory.CreateDirectory(baseDirectory);

            shouldResize = Settings.Map_Should_Resize;
            resizeTo = Settings.Map_Resize_To;

            if (resizeTo < 128)
                resizeTo = 128;

            autoRenderOnUpdate = Settings.Map_Auto_Render_On_Update;
            deleteAllOnExit = Settings.Map_Delete_All_On_Unload;
            notifyOnFirstUpdate = Settings.Map_Notify_On_First_Update;

            RegisterChatBotCommand("maps", "bot.map.cmd.desc", "maps list|render <id> or maps l|r <id>", OnMapCommand);
        }

        public override void OnUnload()
        {
            if (deleteAllOnExit)
            {
                DirectoryInfo di = new(baseDirectory);
                FileInfo[] files = di.GetFiles();

                foreach (FileInfo file in files)
                    file.Delete();
            }
        }

        public string OnMapCommand(string command, string[] args)
        {
            if (args.Length == 0 || (args.Length == 1 && (args[0].ToLower().Equals("list") || args[0].ToLower().Equals("l"))))
            {
                if (cachedMaps.Count == 0)
                    return Translations.TryGet("bot.map.no_maps");

                LogToConsoleTranslated("bot.map.received");

                foreach (var (key, value) in new SortedDictionary<int, McMap>(cachedMaps))
                    LogToConsoleTranslated("bot.map.list_item", key, value.LastUpdated);

                return "";
            }

            if (args.Length > 1)
            {
                if (args[0].ToLower().Equals("render") || args[0].ToLower().Equals("r"))
                {
                    if (args.Length < 2)
                        return "maps <list/render <id>> | maps <l/r <id>>";

                    if (int.TryParse(args[1], out int mapId))
                    {
                        if (!cachedMaps.ContainsKey(mapId))
                            return Translations.TryGet("bot.map.cmd.not_found", mapId);

                        try
                        {
                            McMap map = cachedMaps[mapId];
                            GenerateMapImage(map);
                        }
                        catch (Exception e)
                        {
                            LogDebugToConsole(e.StackTrace!);
                            return Translations.TryGet("bot.map.failed_to_render", mapId);
                        }

                        return "";
                    }

                    return Translations.TryGet("bot.map.cmd.invalid_id");
                }
            }

            return "";
        }

        public override void OnMapData(int mapid, byte scale, bool trackingPosition, bool locked, List<MapIcon> icons, byte columnsUpdated, byte rowsUpdated, byte mapCoulmnX, byte mapRowZ, byte[]? colors)
        {
            if (columnsUpdated == 0 && cachedMaps.ContainsKey(mapid))
                return;

            McMap map = new()
            {
                MapId = mapid,
                Scale = scale,
                TrackingPosition = trackingPosition,
                Locked = locked,
                MapIcons = icons,
                Width = rowsUpdated,
                Height = columnsUpdated,
                X = mapCoulmnX,
                Z = mapRowZ,
                Colors = colors,
                LastUpdated = DateTime.Now
            };

            if (!cachedMaps.ContainsKey(mapid))
            {
                cachedMaps.Add(mapid, map);

                if (notifyOnFirstUpdate)
                    LogToConsoleTranslated("bot.map.received_map", map.MapId);
            }
            else
            {
                cachedMaps.Remove(mapid);
                cachedMaps.Add(mapid, map);
            }

            if (autoRenderOnUpdate)
                GenerateMapImage(map);
        }

        private void GenerateMapImage(McMap map)
        {
            string fileName = baseDirectory + "/Map_" + map.MapId + ".jpg";

            if (File.Exists(fileName))
                File.Delete(fileName);

            Bitmap image = new(map.Width, map.Height);

            for (int x = 0; x < map.Width; ++x)
            {
                for (int y = 0; y < map.Height; ++y)
                {
                    byte inputColor = map.Colors![x + y * map.Width];
                    ColorRGBA color = MapColors.ColorByteToRGBA(inputColor);

                    if (color.Unknown)
                    {
                        string hexCode = new DataTypes(GetProtocolVersion()).ByteArrayToString(new byte[] { inputColor });
                        LogDebugToConsole("Unknown color encountered: " + inputColor + " (Hex: " + hexCode + "), using: RGB(248, 0, 248)");
                    }

                    image.SetPixel(x, y, Color.FromArgb(color.A, color.R, color.G, color.B));
                }
            }

            // Resize, double the image

            if (shouldResize)
                image = ResizeBitmap(image, resizeTo, resizeTo);

            image.Save(fileName);
            LogToConsole(Translations.TryGet("bot.map.rendered", map.MapId, fileName));
        }
        private Bitmap ResizeBitmap(Bitmap sourceBMP, int width, int height)
        {
            Bitmap result = new(width, height);
            using (Graphics g = Graphics.FromImage(result))
                g.DrawImage(sourceBMP, 0, 0, width, height);
            return result;
        }
    }

    internal class McMap
    {
        public int MapId { get; set; }
        public byte Scale { get; set; }
        public bool TrackingPosition { get; set; }
        public bool Locked { get; set; }
        public List<MapIcon>? MapIcons { get; set; }
        public byte Width { get; set; } // rows
        public byte Height { get; set; } // columns
        public byte X { get; set; }
        public byte Z { get; set; }
        public byte[]? Colors;
        public DateTime LastUpdated { get; set; }
    }

    class ColorRGBA
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; }
        public bool Unknown { get; set; } = false;
    }

    class MapColors
    {
        // When colors are updated in a new update, you can get them using the game code: net\minecraft\world\level\material\MaterialColor.java
        public static Dictionary<byte, byte[]> Colors = new()
        {
            //Color ID      R    G    B
            {0,  new byte[]{0,   0,   0}},
            {1,  new byte[]{127, 178, 56}},
            {2,  new byte[]{247, 233, 163}},
            {3,  new byte[]{199, 199, 199}},
            {4,  new byte[]{255, 0,   0}},
            {5,  new byte[]{160, 160, 255}},
            {6,  new byte[]{167, 167, 167}},
            {7,  new byte[]{0,   124, 0}},
            {8,  new byte[]{255, 255, 255}},
            {9,  new byte[]{164, 168, 184}},
            {10, new byte[]{151, 109, 77}},
            {11, new byte[]{112, 112, 112}},
            {12, new byte[]{64,  64,  255}},
            {13, new byte[]{143, 119, 72}},
            {14, new byte[]{255, 252, 245}},
            {15, new byte[]{216, 127, 51}},
            {16, new byte[]{178, 76,  216}},
            {17, new byte[]{102, 153, 216}},
            {18, new byte[]{229, 229, 51}},
            {19, new byte[]{127, 204, 25}},
            {20, new byte[]{242, 127, 165}},
            {21, new byte[]{76,  76,  76}},
            {22, new byte[]{153, 153, 153}},
            {23, new byte[]{76,  127, 153}},
            {24, new byte[]{127, 63,  178}},
            {25, new byte[]{51,  76,  178}},
            {26, new byte[]{102, 76,  51}},
            {27, new byte[]{102, 127, 51}},
            {28, new byte[]{153, 51,  51}},
            {29, new byte[]{25,  25,  25}},
            {30, new byte[]{250, 238, 77}},
            {31, new byte[]{92,  219, 213}},
            {32, new byte[]{74,  128, 255}},
            {33, new byte[]{0,   217, 58}},
            {34, new byte[]{129, 86,  49}},
            {35, new byte[]{112, 2,   0}},
            {36, new byte[]{209, 177, 161}},
            {37, new byte[]{159, 82,  36}},
            {38, new byte[]{149, 87,  108}},
            {39, new byte[]{112, 108, 138}},
            {40, new byte[]{186, 133, 36}},
            {41, new byte[]{103, 117, 53}},
            {42, new byte[]{160, 77,  78}},
            {43, new byte[]{57,  41,  35}},
            {44, new byte[]{135, 107, 98}},
            {45, new byte[]{87,  92,  92}},
            {46, new byte[]{122, 73,  88}},
            {47, new byte[]{76,  62,  92}},
            {48, new byte[]{76,  50,  35}},
            {49, new byte[]{76,  82,  42}},
            {50, new byte[]{142, 60,  46}},
            {51, new byte[]{37,  22,  16}},
            {52, new byte[]{189, 48,  49}},
            {53, new byte[]{148, 63,  97}},
            {54, new byte[]{92,  25,  29}},
            {55, new byte[]{22,  126, 134}},
            {56, new byte[]{58,  142, 140}},
            {57, new byte[]{86,  44,  62}},
            {58, new byte[]{20,  180, 133}},
            {59, new byte[]{100, 100, 100}},
            {60, new byte[]{216, 175, 147}},
            {61, new byte[]{127, 167, 150}}
        };

        public static ColorRGBA ColorByteToRGBA(byte receivedColorId)
        {
            // Divide received color id by 4 to get the base color id
            // Much thanks to DevBobcorn
            byte baseColorId = (byte)(receivedColorId >> 2);

            // Any new colors that we haven't added will be purple like in the missing CS: Source Texture
            if (!Colors.ContainsKey(baseColorId))
                return new ColorRGBA { R = 248, G = 0, B = 248, A = 255, Unknown = true };

            byte shadeId = (byte)(receivedColorId % 4);
            byte shadeMultiplier = 255;

            switch (shadeId)
            {
                case 0:
                    shadeMultiplier = 180;
                    break;

                case 1:
                    shadeMultiplier = 220;
                    break;

                case 3:
                    // NOTE: If we ever add map support below 1.8, this needs to be 220 before 1.8
                    shadeMultiplier = 135;
                    break;
            }

            return new ColorRGBA
            {
                R = (byte)((Colors[baseColorId][0] * shadeMultiplier) / 255),
                G = (byte)((Colors[baseColorId][1] * shadeMultiplier) / 255),
                B = (byte)((Colors[baseColorId][2] * shadeMultiplier) / 255),
                A = 255
            };
        }
    }
}
