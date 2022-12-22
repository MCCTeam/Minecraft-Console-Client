using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftClient.Protocol
{
    public record TitlePacket
    {
        public int Action { init; get; }

        public string TitleText { init; get; } = string.Empty;

        public string SubtitleText { init; get; } = string.Empty;

        public string ActionbarText { init; get; } = string.Empty;

        public int Stay { init; get; }

        public int FadeIn { init; get; }

        public int FadeOut { init; get; }

        public string JsonText { init; get; } = string.Empty;
    }
}
