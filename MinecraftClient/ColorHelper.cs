using System;
using static MinecraftClient.Settings.ConsoleConfigHealper.ConsoleConfig;

namespace MinecraftClient
{
    public static class ColorHelper
    {
        // ANSI escape code - Colors: https://en.wikipedia.org/wiki/ANSI_escape_code#Colors

        private static readonly Tuple<ColorRGBA, int>[] ColorMap4 = new Tuple<ColorRGBA, int>[]
        {
                    new(new(0, 0, 0), 40),
                    new(new(128, 0, 0), 41),
                    new(new(0, 128, 0), 42),
                    new(new(151, 109, 77), 43),
                    new(new(45, 45, 180), 44),
                    new(new(128, 0, 128), 45),
                    new(new(138, 138, 220), 46),
                    new(new(174, 164, 115), 47),
                    new(new(96, 96, 96), 100),
                    new(new(255, 0, 0), 101),
                    new(new(127, 178, 56), 102),
                    new(new(213, 201, 140), 103),
                    new(new(64, 64, 255), 104),
                    new(new(255, 0, 255), 105),
                    new(new(0, 255, 255), 106),
                    new(new(255, 255, 255), 107),
        };

        private static readonly Tuple<ColorRGBA, int>[] ColorMap8 = new Tuple<ColorRGBA, int>[]
        {
                    new(new(0, 0, 0), 0),
                    new(new(128, 0, 0), 1),
                    new(new(0, 128, 0), 2),
                    new(new(128, 128, 0), 3),
                    new(new(0, 0, 128), 4),
                    new(new(128, 0, 128), 5),
                    new(new(0, 128, 128), 6),
                    new(new(192, 192, 192), 7),
                    new(new(128, 128, 128), 8),
                    new(new(255, 0, 0), 9),
                    new(new(0, 255, 0), 10),
                    new(new(255, 255, 0), 11),
                    new(new(0, 0, 255), 12),
                    new(new(255, 0, 255), 13),
                    new(new(0, 255, 255), 14),
                    new(new(255, 255, 255), 15),
                    new(new(8, 8, 8), 232),
                    new(new(18, 18, 18), 233),
                    new(new(28, 28, 28), 234),
                    new(new(38, 38, 38), 235),
                    new(new(48, 48, 48), 236),
                    new(new(58, 58, 58), 237),
                    new(new(68, 68, 68), 238),
                    new(new(78, 78, 78), 239),
                    new(new(88, 88, 88), 240),
                    new(new(98, 98, 98), 241),
                    new(new(108, 108, 108), 242),
                    new(new(118, 118, 118), 243),
                    new(new(128, 128, 128), 244),
                    new(new(138, 138, 138), 245),
                    new(new(148, 148, 148), 246),
                    new(new(158, 158, 158), 247),
                    new(new(168, 168, 168), 248),
                    new(new(178, 178, 178), 249),
                    new(new(188, 188, 188), 250),
                    new(new(198, 198, 198), 251),
                    new(new(208, 208, 208), 252),
                    new(new(218, 218, 218), 253),
                    new(new(228, 228, 228), 254),
                    new(new(238, 238, 238), 255),
        };

        private static readonly byte[] ColorMap8_Step = new byte[] { 0, 95, 135, 175, 215, 255 };

        public static string GetColorEscapeCode(byte R, byte G, byte B, bool foreground)
        {
            return GetColorEscapeCode(R, G, B, foreground, Settings.Config.Console.General.ConsoleColorMode);
        }

        public static string GetColorEscapeCode(byte R, byte G, byte B, bool foreground, ConsoleColorModeType colorDepth)
        {
            switch (colorDepth)
            {
                case ConsoleColorModeType.disable:
                    return string.Empty;

                case ConsoleColorModeType.legacy_4bit:
                    {
                        ColorRGBA color = new(R, G, B);
                        int best_idx = 0;
                        double min_distance = ColorMap4[0].Item1.Distance(color);
                        for (int i = 1; i < ColorMap4.Length; ++i)
                        {
                            double distance = ColorMap4[i].Item1.Distance(color);
                            if (distance < min_distance)
                            {
                                min_distance = distance;
                                best_idx = i;
                            }
                        }
                        if (foreground)
                            return $"§{best_idx:X}";
                        else
                            return $"§§{best_idx:X}";
                    }

                case ConsoleColorModeType.vt100_4bit:
                    {
                        ColorRGBA color = new(R, G, B);
                        int best_idx = 0;
                        double min_distance = ColorMap4[0].Item1.Distance(color);
                        for (int i = 1; i < ColorMap4.Length; ++i)
                        {
                            double distance = ColorMap4[i].Item1.Distance(color);
                            if (distance < min_distance)
                            {
                                min_distance = distance;
                                best_idx = i;
                            }
                        }
                        return string.Format("\u001b[{0}m", ColorMap4[best_idx].Item2 - (foreground ? 10 : 0));
                    }

                case ConsoleColorModeType.vt100_8bit:
                    {
                        ColorRGBA color = new(R, G, B);
                        int R_idx = (int)(R <= 95 ? Math.Round(R / 95.0) : 1 + Math.Round((R - 95.0) / 40.0));
                        int G_idx = (int)(G <= 95 ? Math.Round(G / 95.0) : 1 + Math.Round((G - 95.0) / 40.0));
                        int B_idx = (int)(B <= 95 ? Math.Round(B / 95.0) : 1 + Math.Round((B - 95.0) / 40.0));

                        int best_idx = -1;
                        double min_distance = color.Distance(new ColorRGBA(ColorMap8_Step[R_idx],
                                                                           ColorMap8_Step[G_idx],
                                                                           ColorMap8_Step[B_idx]));
                        for (int i = 0; i < ColorMap8.Length; ++i)
                        {
                            double distance = ColorMap8[i].Item1.Distance(color);
                            if (distance < min_distance)
                            {
                                min_distance = distance;
                                best_idx = i;
                            }
                        }

                        if (best_idx == -1)
                            return string.Format("\u001B[{0};5;{1}m", (foreground ? 38 : 48), 16 + (36 * R_idx) + (6 * G_idx) + B_idx);
                        else
                            return string.Format("\u001B[{0};5;{1}m", (foreground ? 38 : 48), ColorMap8[best_idx].Item2);
                    }

                case ConsoleColorModeType.vt100_24bit:
                    return string.Format("\u001B[{0};2;{1};{2};{3}m", (foreground ? 38 : 48), R, G, B);

                default:
                    return string.Empty;
            }
        }

        public static string GetResetEscapeCode()
        {
            return "\u001b[0m";
        }
    }

    public class ColorRGBA
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; }
        public bool Unknown { get; set; } = false;

        public double Distance(ColorRGBA color)
        {
            double r_mean = (double)(this.R + color.R) / 2.0;
            double R = this.R - color.R;
            double G = this.G - color.G;
            double B = this.B - color.B;
            return Math.Sqrt((2.0 + r_mean / 256.0) * (R * R) + 4.0 * (G * G) + (2.0 + (255 - r_mean) / 256.0) * (B * B));
        }
        public ColorRGBA(byte r, byte g, byte b, byte a, bool unknown)
        {
            R = r;
            G = g;
            B = b;
            A = a;
            Unknown = unknown;
        }

        public ColorRGBA(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public ColorRGBA(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
            A = 255;
        }
    }
}
