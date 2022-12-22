using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftClient.Mapping
{
    public record MapData
    {
        public int MapId { init; get; }

        public byte Scale { init; get; }

        public bool TrackingPosition { init; get; }

        public bool Locked { init; get; }

        public List<MapIcon> Icons { init; get; } = new();

        public byte ColumnsUpdated { init; get; }

        public byte RowsUpdated { init; get; }

        public byte MapCoulmnX { init; get; }

        public byte MapRowZ { init; get; }

        public byte[]? Colors { init; get; }
    }
}
