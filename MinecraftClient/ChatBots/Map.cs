﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using MinecraftClient.Mapping;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots
{
    public class Map : ChatBot
    {
        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "Map";

            public bool Enabled = false;

            [TomlInlineComment("$config.ChatBot.Map.Render_In_Console$")]
            public bool Render_In_Console = true;

            [TomlInlineComment("$config.ChatBot.Map.Save_To_File$")]
            public bool Save_To_File = false;

            [TomlInlineComment("$config.ChatBot.Map.Auto_Render_On_Update$")]
            public bool Auto_Render_On_Update = false;

            [TomlInlineComment("$config.ChatBot.Map.Delete_All_On_Unload$")]
            public bool Delete_All_On_Unload = true;

            [TomlInlineComment("$config.ChatBot.Map.Notify_On_First_Update$")]
            public bool Notify_On_First_Update = true;

            public void OnSettingUpdate() { }
        }

        private readonly string baseDirectory = @"Rendered_Maps";

        private readonly Dictionary<int, McMap> cachedMaps = new();

        public override void Initialize()
        {
            if (!Directory.Exists(baseDirectory))
                Directory.CreateDirectory(baseDirectory);

            RegisterChatBotCommand("maps", "bot.map.cmd.desc", "maps list|render <id> or maps l|r <id>", OnMapCommand);
        }

        public override void OnUnload()
        {
            if (Config.Delete_All_On_Unload)
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

                    if (int.TryParse(args[1], NumberStyles.Any, CultureInfo.CurrentCulture, out int mapId))
                    {
                        if (!cachedMaps.ContainsKey(mapId))
                            return Translations.TryGet("bot.map.cmd.not_found", mapId);

                        try
                        {
                            McMap map = cachedMaps[mapId];
                            if (Config.Save_To_File)
                                SaveToFile(map);

                            if (Config.Render_In_Console)
                                RenderInConsole(map);

                            return "";
                        }
                        catch (Exception e)
                        {
                            LogDebugToConsole(e.StackTrace!);
                            return Translations.TryGet("bot.map.failed_to_render", mapId);
                        }
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

            if (rowsUpdated <= 0 && columnsUpdated <= 0)
                return;

            McMap map = new()
            {
                MapId = mapid,
                Scale = scale,
                TrackingPosition = trackingPosition,
                Locked = locked,
                MapIcons = icons,
                Width = columnsUpdated,
                Height = rowsUpdated,
                X = mapCoulmnX,
                Z = mapRowZ,
                Colors = colors,
                LastUpdated = DateTime.Now
            };

            if (!cachedMaps.ContainsKey(mapid))
            {
                cachedMaps.Add(mapid, map);

                if (Config.Notify_On_First_Update)
                    LogToConsoleTranslated("bot.map.received_map", map.MapId);
            }
            else
            {
                McMap old_map = cachedMaps[mapid];
                lock (old_map)
                {
                    for (int x = 0; x < map.Width; ++x)
                        for (int y = 0; y < map.Height; ++y)
                            old_map.Colors![(map.X + x) + (map.Z + y) * old_map.Width] = map.Colors![x + y * map.Width];
                }
                map = old_map;
            }

            if (Config.Auto_Render_On_Update)
            {
                if (Config.Save_To_File)
                    SaveToFile(map);

                if (Config.Render_In_Console)
                    RenderInConsole(map);
            }
        }

        private void SaveToFile(McMap map)
        {
            string fileName = baseDirectory + Path.DirectorySeparatorChar + "Map_" + map.MapId.ToString().PadLeft(5, '0') + ".bmp";

            if (File.Exists(fileName))
                File.Delete(fileName);

            using FileStream file = File.OpenWrite(fileName);
            file.Write(BitConverter.GetBytes((ushort)0x4d42)); // WORD File Header bfType: "BM"
            file.Write(BitConverter.GetBytes((uint)(14 + 40 + 3 * map.Width * map.Height))); // DWORD File Header bfSize
            file.Write(BitConverter.GetBytes((ushort)0)); // WORD File Header bfReserved1
            file.Write(BitConverter.GetBytes((ushort)0)); // WORD File Header bfReserved2
            file.Write(BitConverter.GetBytes((uint)54)); // DWORD File Header bfOffBits
            file.Write(BitConverter.GetBytes((uint)40)); //  DWORD Info Header biSize
            file.Write(BitConverter.GetBytes((uint)map.Width)); // LONG Info Header biWidth
            file.Write(BitConverter.GetBytes((uint)map.Height)); // LONG Info Header biHeight
            file.Write(BitConverter.GetBytes((ushort)1)); // WORD Info Header biPlanes
            file.Write(BitConverter.GetBytes((ushort)24)); // WORD Info Header biBitCount
            file.Write(BitConverter.GetBytes((uint)0x00)); // DWORD Info Header biCompression: BI_RGB
            file.Write(BitConverter.GetBytes((uint)0)); // DWORD Info Header biSizeImage
            file.Write(BitConverter.GetBytes((uint)0)); // LONG Info Header biXPelsPerMeter
            file.Write(BitConverter.GetBytes((uint)0)); // LONG Info Header biYPelsPerMeter
            file.Write(BitConverter.GetBytes((uint)0)); // DWORD Info Header biClrUsed
            file.Write(BitConverter.GetBytes((uint)0)); // DWORD Info Header biClrImportant
            Span<byte> pixel = stackalloc byte[3];
            for (int y = map.Height - 1; y >= 0; --y)
            {
                for (int x = 0; x < map.Width; ++x)
                {
                    ColorRGBA color = MapColors.ColorByteToRGBA(map.Colors![x + y * map.Width]);
                    pixel[0] = color.B; pixel[1] = color.G; pixel[2] = color.R;
                    file.Write(pixel);
                }
            }
            file.Close();
            LogToConsole(Translations.TryGet("bot.map.rendered", map.MapId, fileName));
        }

        private void RenderInConsole(McMap map)
        {
            StringBuilder sb = new();

            int consoleWidth = Math.Max(Console.BufferWidth, Settings.Config.Main.Advanced.MinTerminalWidth) / 2;
            int consoleHeight = Math.Max(Console.BufferHeight, Settings.Config.Main.Advanced.MinTerminalHeight) - 1;

            int scaleX = (map.Width + consoleWidth - 1) / consoleWidth;
            int scaleY = (map.Height + consoleHeight - 1) / consoleHeight;
            int scale = Math.Max(scaleX, scaleY);
            if (scale > 1)
                sb.AppendLine(Translations.Get("bot.map.scale", map.Width, map.Height, map.Width / scale, map.Height / scale));

            for (int base_y = 0; base_y < map.Height; base_y += scale)
            {
                int last_R = -1, last_G = -1, last_B = -1;
                for (int base_x = 0; base_x < map.Width; base_x += scale)
                {
                    int RL = 0, GL = 0, BL = 0, RR = 0, GR = 0, BR = 0;
                    double mid_dx = (double)(scale - 1) / 2;
                    for (int dy = 0; dy < scale; ++dy)
                    {
                        for (int dx = 0; dx < scale; ++dx)
                        {
                            int x = Math.Min(base_x + dx, map.Width - 1);
                            int y = Math.Min(base_y + dy, map.Height - 1);
                            ColorRGBA color = MapColors.ColorByteToRGBA(map.Colors![x + y * map.Width]);
                            if (dx <= mid_dx)
                            {
                                RL += color.R; GL += color.G; BL += color.B;
                            }
                            if (dx >= mid_dx)
                            {
                                RR += color.R; GR += color.G; BR += color.B;
                            }
                        }
                    }

                    int pixel_cnt = ((scale + 1) / 2) * scale;
                    RL = (int)Math.Round((double)RL / pixel_cnt);
                    GL = (int)Math.Round((double)GL / pixel_cnt);
                    BL = (int)Math.Round((double)BL / pixel_cnt);
                    RR = (int)Math.Round((double)RR / pixel_cnt);
                    GR = (int)Math.Round((double)GR / pixel_cnt);
                    BR = (int)Math.Round((double)BR / pixel_cnt);

                    if (RL == last_R && GL == last_G && BL == last_B)
                        sb.Append(' ');
                    else
                    {
                        sb.Append(ColorHelper.GetColorEscapeCode((byte)RL, (byte)GL, (byte)BL, false)).Append(' ');
                        last_R = RL; last_G = GL; last_B = BL;
                    }

                    if (RR == last_R && GR == last_G && BR == last_B)
                        sb.Append(' ');
                    else
                    {
                        sb.Append(ColorHelper.GetColorEscapeCode((byte)RR, (byte)GR, (byte)BR, false)).Append(' ');
                        last_R = RR; last_G = GR; last_B = BR;
                    }
                }
                if (base_y >= map.Height - scale)
                    sb.Append(ColorHelper.GetResetEscapeCode());
                else
                    sb.AppendLine(ColorHelper.GetResetEscapeCode());
            }
            ConsoleIO.WriteLine(sb.ToString());
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
                return new(248, 0, 248, 255, true);

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

            return new(
                r: (byte)((Colors[baseColorId][0] * shadeMultiplier) / 255),
                g: (byte)((Colors[baseColorId][1] * shadeMultiplier) / 255), 
                b: (byte)((Colors[baseColorId][2] * shadeMultiplier) / 255), 
                a: 255
            );
        }
    }
}
