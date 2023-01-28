using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Brigadier.NET;
using Brigadier.NET.Builder;
using ImageMagick;
using MinecraftClient.CommandHandler;
using MinecraftClient.CommandHandler.Patch;
using MinecraftClient.Mapping;
using MinecraftClient.Scripting;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots
{
    public class Map : ChatBot
    {
        public const string CommandName = "maps";

        public static Configs Config = new();

        public struct QueuedMap
        {
            public string FileName;
            public int MapId;
        }

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "Map";

            public bool Enabled = false;

            [TomlInlineComment("$ChatBot.Map.Render_In_Console$")]
            public bool Render_In_Console = true;

            [TomlInlineComment("$ChatBot.Map.Save_To_File$")]
            public bool Save_To_File = false;

            [TomlInlineComment("$ChatBot.Map.Auto_Render_On_Update$")]
            public bool Auto_Render_On_Update = false;

            [TomlInlineComment("$ChatBot.Map.Delete_All_On_Unload$")]
            public bool Delete_All_On_Unload = true;

            [TomlInlineComment("$ChatBot.Map.Notify_On_First_Update$")]
            public bool Notify_On_First_Update = true;

            [TomlInlineComment("$ChatBot.Map.Rasize_Rendered_Image$")]
            public bool Rasize_Rendered_Image = false;

            [TomlInlineComment("$ChatBot.Map.Resize_To$")]
            public int Resize_To = 512;

            [TomlPrecedingComment("$ChatBot.Map.Send_Rendered_To_Bridges$")]
            public bool Send_Rendered_To_Discord = false;
            public bool Send_Rendered_To_Telegram = false;

            public void OnSettingUpdate()
            {
                if (Resize_To <= 0)
                    Resize_To = 128;
            }
        }

        private readonly string baseDirectory = @"Rendered_Maps";

        internal readonly Dictionary<int, McMap> cachedMaps = new();

        private readonly Queue<QueuedMap> discordQueue = new();

        public override void Initialize()
        {
            if (!Directory.Exists(baseDirectory))
                Directory.CreateDirectory(baseDirectory);

            DeleteRenderedMaps();

            McClient.dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CommandName)
                    .Executes(r => OnCommandHelp(r.Source, string.Empty))
                )
            );

            McClient.dispatcher.Register(l => l.Literal(CommandName)
                .Executes(r => OnCommandList(r.Source))
                .Then(l => l.Literal("list")
                    .Executes(r => OnCommandList(r.Source)))
                .Then(l => l.Literal("render")
                    .Then(l => l.Argument("MapID", MccArguments.MapBotMapId())
                        .Executes(r => OnCommandRender(r.Source, Arguments.GetInteger(r, "MapID")))))
                .Then(l => l.Literal("_help")
                    .Executes(r => OnCommandHelp(r.Source, string.Empty))
                    .Redirect(McClient.dispatcher.GetRoot().GetChild("help").GetChild(CommandName)))
            );
        }

        public override void OnUnload()
        {
            McClient.dispatcher.Unregister(CommandName);
            McClient.dispatcher.GetRoot().GetChild("help").RemoveChild(CommandName);
            DeleteRenderedMaps();
        }

        private int OnCommandHelp(CmdResult r, string? cmd)
        {
            return r.SetAndReturn(cmd switch
            {
#pragma warning disable format // @formatter:off
                _           =>   Translations.error_usage + ": /maps <list/render <id>>"
                                   + '\n' + McClient.dispatcher.GetAllUsageString(CommandName, false),
#pragma warning restore format // @formatter:on
            });
        }

        private int OnCommandList(CmdResult r)
        {
            if (cachedMaps.Count == 0)
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.bot_map_no_maps);

            LogToConsole(Translations.bot_map_received);

            foreach (var (key, value) in new SortedDictionary<int, McMap>(cachedMaps))
                LogToConsole(string.Format(Translations.bot_map_list_item, key, value.LastUpdated));

            return r.SetAndReturn(CmdResult.Status.Done);
        }

        private int OnCommandRender(CmdResult r, int mapId)
        {
            if (!cachedMaps.ContainsKey(mapId))
                return r.SetAndReturn(CmdResult.Status.Fail, string.Format(Translations.bot_map_cmd_not_found, mapId));

            try
            {
                McMap map = cachedMaps[mapId];
                if (Config.Save_To_File)
                    SaveToFile(map);

                if (Config.Render_In_Console)
                    RenderInConsole(map);

                return r.SetAndReturn(CmdResult.Status.Done);
            }
            catch (Exception e)
            {
                LogDebugToConsole(e.StackTrace!);
                return r.SetAndReturn(CmdResult.Status.Fail, string.Format(Translations.bot_map_failed_to_render, mapId));
            }
        }

        private void DeleteRenderedMaps()
        {
            if (Config.Delete_All_On_Unload)
            {
                DirectoryInfo di = new(baseDirectory);
                FileInfo[] files = di.GetFiles();

                foreach (FileInfo file in files)
                    file.Delete();
            }
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
                    LogToConsole(string.Format(Translations.bot_map_received_map, map.MapId));
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

            LogToConsole(string.Format(Translations.bot_map_rendered, map.MapId, fileName));

            if (Config.Rasize_Rendered_Image)
            {
                using (var image = new MagickImage(fileName))
                {
                    var size = new MagickGeometry(Config.Resize_To, Config.Resize_To);
                    size.IgnoreAspectRatio = true;

                    image.Resize(size);
                    image.Write(fileName);
                    LogToConsole(string.Format(Translations.bot_map_resized_rendered_image, map.MapId, Config.Resize_To));
                }
            }

            if (Config.Send_Rendered_To_Discord || Config.Send_Rendered_To_Telegram)
            {
                // We need to queue up images because Discord/Telegram Bridge is not ready immediatelly
                if (DiscordBridge.Config.Enabled || TelegramBridge.Config.Enabled)
                    discordQueue.Enqueue(new QueuedMap { FileName = fileName, MapId = map.MapId });
            }
        }

        public override void Update()
        {
            DiscordBridge? discordBridge = DiscordBridge.GetInstance();
            TelegramBridge? telegramBridge = TelegramBridge.GetInstance();

            if (Config.Send_Rendered_To_Discord)
            {
                if (discordBridge == null || (discordBridge != null && !discordBridge.IsConnected))
                    return;
            }

            if (Config.Send_Rendered_To_Telegram)
            {
                if (telegramBridge == null || (telegramBridge != null && !telegramBridge.IsConnected))
                    return;
            }

            if (discordQueue.Count > 0)
            {
                QueuedMap map = discordQueue.Dequeue();
                string fileName = map.FileName;

                // We must convert to a PNG in order to send to Discord, BMP does not work
                string newFileName = fileName.Replace(".bmp", ".png");
                using (var image = new MagickImage(fileName))
                {
                    image.Write(newFileName);

                    if (Config.Send_Rendered_To_Discord)
                        discordBridge!.SendImage(newFileName, $"> A render of the map with an id: **{map.MapId}**");

                    if (Config.Send_Rendered_To_Telegram)
                        telegramBridge!.SendImage(newFileName, $"A render of the map with an id: *{map.MapId}*");

                    newFileName = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + newFileName;

                    if (Config.Send_Rendered_To_Discord)
                        LogToConsole(string.Format(Translations.bot_map_sent_to_discord, map.MapId));

                    if (Config.Send_Rendered_To_Telegram)
                        LogToConsole(string.Format(Translations.bot_map_sent_to_telegram, map.MapId));

                    // Wait for 2 seconds and then try until file is free for deletion
                    // 10 seconds timeout
                    Task.Run(async () =>
                    {
                        await Task.Delay(2000);

                        var time = Stopwatch.StartNew();

                        while (time.ElapsedMilliseconds < 10000) // 10 seconds
                        {
                            try
                            {
                                // Delete the temporary file
                                if (File.Exists(newFileName))
                                    File.Delete(newFileName);
                            }
                            catch (IOException) { }
                        }
                    });
                }
            }
        }

        private static void RenderInConsole(McMap map)
        {
            StringBuilder sb = new();
            int consoleWidth = Math.Max(Console.BufferWidth, Settings.Config.Main.Advanced.MinTerminalWidth) / 2;
            int consoleHeight = Math.Max(Console.BufferHeight, Settings.Config.Main.Advanced.MinTerminalHeight) - 1;
            int scaleX = (map.Width + consoleWidth - 1) / consoleWidth;
            int scaleY = (map.Height + consoleHeight - 1) / consoleHeight;
            int scale = Math.Max(scaleX, scaleY);
            if (scale > 1)
                sb.AppendLine(string.Format(Translations.bot_map_scale, map.Width, map.Height, map.Width / scale, map.Height / scale));

            for (int base_y = 0; base_y < map.Height; base_y += scale)
            {
                string lastFg = string.Empty, lagtBg = string.Empty;
                for (int base_x = 0; base_x < map.Width; base_x += scale)
                {
                    int RUL = 0, GUL = 0, BUL = 0, RUR = 0, GUR = 0, BUR = 0;
                    int RDL = 0, GDL = 0, BDL = 0, RDR = 0, GDR = 0, BDR = 0;
                    double mid = (double)(scale - 1) / 2;
                    for (int dy = 0; dy < scale; ++dy)
                    {
                        for (int dx = 0; dx < scale; ++dx)
                        {
                            int x = Math.Min(base_x + dx, map.Width - 1);
                            int y = Math.Min(base_y + dy, map.Height - 1);
                            ColorRGBA color = MapColors.ColorByteToRGBA(map.Colors![x + y * map.Width]);
                            if (dx <= mid)
                            {
                                if (dy <= mid)
                                {
                                    RUL += color.R; GUL += color.G; BUL += color.B;
                                }
                                if (dy >= mid)
                                {
                                    RDL += color.R; GDL += color.G; BDL += color.B;
                                }
                            }
                            if (dx >= mid)
                            {
                                if (dy <= mid)
                                {
                                    RUR += color.R; GUR += color.G; BUR += color.B;
                                }
                                if (dy >= mid)
                                {
                                    RDR += color.R; GDR += color.G; BDR += color.B;
                                }
                            }
                        }
                    }

                    int pixel_cnt = ((scale + 1) / 2) * ((scale + 1) / 2);
                    RDL = (int)Math.Round((double)RDL / pixel_cnt);
                    GDL = (int)Math.Round((double)GDL / pixel_cnt);
                    BDL = (int)Math.Round((double)BDL / pixel_cnt);
                    RDR = (int)Math.Round((double)RDR / pixel_cnt);
                    GDR = (int)Math.Round((double)GDR / pixel_cnt);
                    BDR = (int)Math.Round((double)BDR / pixel_cnt);

                    RUL = (int)Math.Round((double)RUL / pixel_cnt);
                    GUL = (int)Math.Round((double)GUL / pixel_cnt);
                    BUL = (int)Math.Round((double)BUL / pixel_cnt);
                    RUR = (int)Math.Round((double)RUR / pixel_cnt);
                    GUR = (int)Math.Round((double)GUR / pixel_cnt);
                    BUR = (int)Math.Round((double)BUR / pixel_cnt);

                    string colorCode = ColorHelper.GetColorEscapeCode((byte)RUL, (byte)GUL, (byte)BUL, true);
                    if (lastFg != colorCode) { sb.Append(colorCode); lastFg = colorCode; }
                    colorCode = ColorHelper.GetColorEscapeCode((byte)RDL, (byte)GDL, (byte)BDL, false);
                    if (lagtBg != colorCode) { sb.Append(colorCode); lagtBg = colorCode; }
                    sb.Append('▀');

                    colorCode = ColorHelper.GetColorEscapeCode((byte)RUR, (byte)GUR, (byte)BUR, true);
                    if (lastFg != colorCode) { sb.Append(colorCode); lastFg = colorCode; }
                    colorCode = ColorHelper.GetColorEscapeCode((byte)RDR, (byte)GDR, (byte)BDR, false);
                    if (lagtBg != colorCode) { sb.Append(colorCode); lagtBg = colorCode; }
                    sb.Append('▀');
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

    internal class MapColors
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
