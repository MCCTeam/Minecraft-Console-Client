using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_5;

public class EitherHolderComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public bool IsHolder { get; set; }
    public int HolderId { get; set; }
    public string? ResourceKey { get; set; }

    public override void Parse(Queue<byte> data)
    {
        IsHolder = DataTypes.ReadNextBool(data);
        if (IsHolder)
        {
            HolderId = DataTypes.ReadNextVarInt(data);
            // For simple entity variants, holderId > 0 means registry ref (id = holderId - 1)
            // holderId == 0 means inline data; for most variants the inline is just the variant fields
            // We skip inline data since MCC doesn't use variant details
            if (HolderId == 0)
            {
                // Read inline variant data - varies by type, but most are simple
                // For chicken/variant specifically this might have additional fields
                // We'll consume what we can based on the pattern
                // TODO: If needed, specialize per variant type
            }
        }
        else
        {
            ResourceKey = DataTypes.ReadNextString(data);
        }
    }

    public override Queue<byte> Serialize()
    {
        var bytes = new List<byte>();
        bytes.AddRange(DataTypes.GetBool(IsHolder));
        if (IsHolder)
        {
            bytes.AddRange(DataTypes.GetVarInt(HolderId));
        }
        else
        {
            bytes.AddRange(DataTypes.GetString(ResourceKey ?? ""));
        }
        return new Queue<byte>(bytes);
    }
}
