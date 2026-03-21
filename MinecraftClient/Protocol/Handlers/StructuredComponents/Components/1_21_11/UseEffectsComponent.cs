using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_11;

public class UseEffectsComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public bool CanSprint { get; set; }
    public bool InteractVibrations { get; set; }
    public float SpeedMultiplier { get; set; }

    public override void Parse(Queue<byte> data)
    {
        CanSprint = dataTypes.ReadNextBool(data);
        InteractVibrations = dataTypes.ReadNextBool(data);
        SpeedMultiplier = dataTypes.ReadNextFloat(data);
    }

    public override Queue<byte> Serialize()
    {
        var bytes = new List<byte>();
        bytes.AddRange(DataTypes.GetBool(CanSprint));
        bytes.AddRange(DataTypes.GetBool(InteractVibrations));
        bytes.AddRange(DataTypes.GetFloat(SpeedMultiplier));
        return new Queue<byte>(bytes);
    }
}
