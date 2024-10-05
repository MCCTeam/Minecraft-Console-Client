using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class LodestoneTrackerComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public bool HasGlobalPosition { get; set; }
    public string Dimension { get; set; } = null!;
    public Location Position { get; set; }
    public bool Tracked { get; set; }
    
    public override void Parse(Queue<byte> data)
    {
        HasGlobalPosition = dataTypes.ReadNextBool(data);

        if (HasGlobalPosition)
        {
            Dimension = dataTypes.ReadNextString(data);
            Position = dataTypes.ReadNextLocation(data);
        }
        
        Tracked = dataTypes.ReadNextBool(data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetBool(HasGlobalPosition));

        if (HasGlobalPosition)
        {
            data.AddRange(DataTypes.GetString(Dimension));
            data.AddRange(DataTypes.GetLocation(Position));
        }
        
        data.AddRange(DataTypes.GetBool(Tracked));
        return new Queue<byte>(data);
    }
}