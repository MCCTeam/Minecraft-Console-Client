using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_5;

public class ProvidesBannerPatternsComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public string TagKey { get; set; } = string.Empty;

    public override void Parse(Queue<byte> data)
    {
        TagKey = DataTypes.ReadNextString(data); // ResourceLocation
    }

    public override Queue<byte> Serialize()
    {
        var bytes = new List<byte>();
        bytes.AddRange(DataTypes.GetString(TagKey));
        return new Queue<byte>(bytes);
    }
}
