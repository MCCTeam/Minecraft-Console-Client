using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace MinecraftClient.Tui
{
    /// <summary>
    /// Parses Minecraft § color codes and produces Avalonia Inlines for rich text display.
    /// </summary>
    public static class McColorParser
    {
        private static readonly Dictionary<char, IBrush> ColorMap = new()
        {
            { '0', new SolidColorBrush(Color.FromRgb(0, 0, 0)) },
            { '1', new SolidColorBrush(Color.FromRgb(0, 0, 170)) },
            { '2', new SolidColorBrush(Color.FromRgb(0, 170, 0)) },
            { '3', new SolidColorBrush(Color.FromRgb(0, 170, 170)) },
            { '4', new SolidColorBrush(Color.FromRgb(170, 0, 0)) },
            { '5', new SolidColorBrush(Color.FromRgb(170, 0, 170)) },
            { '6', new SolidColorBrush(Color.FromRgb(255, 170, 0)) },
            { '7', new SolidColorBrush(Color.FromRgb(170, 170, 170)) },
            { '8', new SolidColorBrush(Color.FromRgb(85, 85, 85)) },
            { '9', new SolidColorBrush(Color.FromRgb(85, 85, 255)) },
            { 'a', new SolidColorBrush(Color.FromRgb(85, 255, 85)) },
            { 'b', new SolidColorBrush(Color.FromRgb(85, 255, 255)) },
            { 'c', new SolidColorBrush(Color.FromRgb(255, 85, 85)) },
            { 'd', new SolidColorBrush(Color.FromRgb(255, 85, 255)) },
            { 'e', new SolidColorBrush(Color.FromRgb(255, 255, 85)) },
            { 'f', Brushes.White },
        };

        public static TextBlock CreateColoredTextBlock(string text, TextWrapping wrapping = TextWrapping.Wrap)
        {
            var tb = new TextBlock
            {
                TextWrapping = wrapping,
                Padding = new Avalonia.Thickness(0),
                Margin = new Avalonia.Thickness(0),
            };

            if (string.IsNullOrEmpty(text) || !text.Contains('§'))
            {
                tb.Text = text ?? "";
                tb.Foreground = Brushes.White;
                return tb;
            }

            IBrush currentColor = Brushes.White;
            bool bold = false;
            bool italic = false;
            int start = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '§' && i + 1 < text.Length)
                {
                    if (i > start)
                        AddRun(tb, text[start..i], currentColor, bold, italic);

                    char code = char.ToLower(text[i + 1]);

                    if (ColorMap.TryGetValue(code, out var brush))
                    {
                        currentColor = brush;
                        bold = false;
                        italic = false;
                    }
                    else
                    {
                        switch (code)
                        {
                            case 'l': bold = true; break;
                            case 'o': italic = true; break;
                            case 'r':
                                currentColor = Brushes.White;
                                bold = false;
                                italic = false;
                                break;
                        }
                    }

                    i++;
                    start = i + 1;
                }
            }

            if (start < text.Length)
                AddRun(tb, text[start..], currentColor, bold, italic);

            if (tb.Inlines?.Count == 0)
            {
                tb.Text = "";
                tb.Foreground = Brushes.White;
            }

            return tb;
        }

        private static void AddRun(TextBlock tb, string text, IBrush color, bool bold, bool italic)
        {
            if (text.Length == 0) return;

            tb.Inlines ??= new InlineCollection();
            tb.Inlines.Add(new Run(text)
            {
                Foreground = color,
                FontWeight = bold ? FontWeight.Bold : FontWeight.Normal,
                FontStyle = italic ? FontStyle.Italic : FontStyle.Normal,
            });
        }
    }
}
