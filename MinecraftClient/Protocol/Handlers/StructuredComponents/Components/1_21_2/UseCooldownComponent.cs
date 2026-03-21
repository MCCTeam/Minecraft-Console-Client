using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_2;

public class UseCooldownComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public float Seconds { get; set; }
    public bool HasCooldownGroup { get; set; }
    public string? CooldownGroup { get; set; }

    public override void Parse(Queue<byte> data)
    {
        Seconds = dataTypes.ReadNextFloat(data);
        HasCooldownGroup = dataTypes.ReadNextBool(data);
        if (HasCooldownGroup)
            CooldownGroup = dataTypes.ReadNextString(data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetFloat(Seconds));
        data.AddRange(DataTypes.GetBool(HasCooldownGroup));
        if (HasCooldownGroup && CooldownGroup != null)
            data.AddRange(DataTypes.GetString(CooldownGroup));
        return new Queue<byte>(data);
    }
}
