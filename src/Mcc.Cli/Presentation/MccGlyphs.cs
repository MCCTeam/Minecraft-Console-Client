using DMCBK.Core.Presentation;

namespace Mcc.Cli.Presentation;

/// <summary>Console glyph preference before terminal capability detection.</summary>
internal enum GlyphMode { Auto, Emoji, Ascii }

/// <summary>Terminal glyph vocabularies owned by MCC.</summary>
internal static class MccGlyphs
{
    public static GlyphSet Emoji { get; } = new()
    {
        IsEmoji = true,
        StatusWidth = 2,
        Ok = "✅",
        Fail = "❌",
        Blocked = "🚫",
        Pending = "⏳",
        Info = "ℹ️",
        Warn = "⚠️",
        Empty = "·",
        ChunkUnloaded = "🔳",
        ChunkLoading = "🟨",
        ChunkLoaded = "🟩",
        PluginEnabled = "✅",
        PluginDisabled = "🚫",
        Health = "❤",
        Food = "🍖",
        Experience = "⭐",
        AdvancementDone = "✅",
        AdvancementTodo = "⬜",
        CheckboxOn = "☑",
        CheckboxOff = "☐",
    };
}
