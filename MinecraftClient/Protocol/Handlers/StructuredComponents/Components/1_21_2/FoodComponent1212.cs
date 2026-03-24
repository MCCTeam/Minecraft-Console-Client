using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_2;

public class FoodComponent1212(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int Nutrition { get; set; }
    public float Saturation { get; set; }
    public bool CanAlwaysEat { get; set; }

    public override void Parse(Queue<byte> data)
    {
        Nutrition = DataTypes.ReadNextVarInt(data);
        Saturation = DataTypes.ReadNextFloat(data);
        CanAlwaysEat = DataTypes.ReadNextBool(data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(Nutrition));
        data.AddRange(DataTypes.GetFloat(Saturation));
        data.AddRange(DataTypes.GetBool(CanAlwaysEat));
        return new Queue<byte>(data);
    }
}
