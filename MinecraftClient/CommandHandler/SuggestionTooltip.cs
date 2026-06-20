using Brigadier.NET;

namespace MinecraftClient.CommandHandler
{
    internal class SuggestionTooltip(string tooltip) : IMessage
    {
        public string String { get; set; } = tooltip;
    }
}
