using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Mapping
{
    public class MapIcon
    {
        public MapIconType Type { set; get; }
        public byte X { set; get; }
        public byte Z { set; get; }
        public byte Direction { set; get; }
        public string? DisplayName { set; get; } = null;
    }
}
