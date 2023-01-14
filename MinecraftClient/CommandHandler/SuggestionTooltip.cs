using Brigadier.NET;

namespace MinecraftClient.CommandHandler
{
    internal class SuggestionTooltip : IMessage
    {
        public SuggestionTooltip(string tooltip)
        {
            String = tooltip;
        }

        public string String { get; set; }
    }
}
