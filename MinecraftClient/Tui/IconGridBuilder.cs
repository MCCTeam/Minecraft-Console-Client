using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace MinecraftClient.Tui
{
    internal static class IconGridBuilder
    {
        internal static Grid BuildFromRgba(byte[] rgba, int srcWidth, int srcHeight, int displaySize)
        {
            int cellCols = displaySize;
            int cellRows = displaySize / 2;

            var grid = new Grid();
            for (int c = 0; c < cellCols; c++)
                grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Auto));
            for (int r = 0; r < cellRows; r++)
                grid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Auto));

            for (int row = 0; row < cellRows; row++)
            {
                for (int col = 0; col < cellCols; col++)
                {
                    int topPixelY = row * 2;
                    int bottomPixelY = row * 2 + 1;

                    var topColor = SamplePixel(rgba, srcWidth, srcHeight, col, topPixelY, cellCols, displaySize);
                    var bottomColor = SamplePixel(rgba, srcWidth, srcHeight, col, bottomPixelY, cellCols, displaySize);

                    var cell = new TextBlock
                    {
                        Text = "\u2580",
                        Foreground = new SolidColorBrush(topColor),
                        Background = new SolidColorBrush(bottomColor),
                        Padding = new Thickness(0),
                        Margin = new Thickness(0),
                    };

                    Grid.SetRow(cell, row);
                    Grid.SetColumn(cell, col);
                    grid.Children.Add(cell);
                }
            }

            return grid;
        }

        internal static Grid BuildFromBase64(string base64Data, int displaySize)
        {
            byte[] imageBytes;
            try
            {
                imageBytes = Convert.FromBase64String(base64Data);
            }
            catch
            {
                return new Grid();
            }

            return BuildFromImageBytes(imageBytes, displaySize) ?? new Grid();
        }

        internal static Grid? BuildFromImageBytes(byte[] imageBytes, int displaySize)
        {
            int srcWidth, srcHeight;
            byte[] rgba;
            try
            {
                (srcWidth, srcHeight, rgba) = DecodeImageToRgba(imageBytes);
            }
            catch
            {
                return null;
            }

            return BuildFromRgba(rgba, srcWidth, srcHeight, displaySize);
        }

        internal static (int Width, int Height, byte[] Rgba) DecodeImageToRgba(byte[] imageData)
        {
            using var image = new ImageMagick.MagickImage(imageData);
            int w = (int)image.Width;
            int h = (int)image.Height;

            using var pixels = image.GetPixelsUnsafe();
            var rgba = new byte[w * h * 4];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var pixel = pixels.GetPixel(x, y)!;
                    int idx = (y * w + x) * 4;
                    var color = pixel.ToColor()!;
                    rgba[idx] = (byte)(color.R >> 8);
                    rgba[idx + 1] = (byte)(color.G >> 8);
                    rgba[idx + 2] = (byte)(color.B >> 8);
                    rgba[idx + 3] = (byte)(color.A >> 8);
                }
            }

            return (w, h, rgba);
        }

        private static Color SamplePixel(byte[] rgba, int srcW, int srcH, int dstX, int dstY, int dstW, int dstH)
        {
            int srcX = dstX * srcW / dstW;
            int srcY = dstY * srcH / dstH;
            srcX = Math.Clamp(srcX, 0, srcW - 1);
            srcY = Math.Clamp(srcY, 0, srcH - 1);

            int idx = (srcY * srcW + srcX) * 4;
            if (idx + 3 >= rgba.Length)
                return Color.FromRgb(0, 0, 0);

            byte r = rgba[idx];
            byte g = rgba[idx + 1];
            byte b = rgba[idx + 2];
            byte a = rgba[idx + 3];

            return a < 128 ? Color.FromRgb(0, 0, 0) : Color.FromRgb(r, g, b);
        }
    }
}
