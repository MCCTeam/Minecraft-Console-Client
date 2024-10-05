using System;
using System.Collections.Generic;
using MinecraftClient.Inventory;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class TrimComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int TrimMaterialType { get; set; }
    public string AssetName { get; set; } = null!;
    public int Ingredient { get; set; }
    public float ItemModelIndex { get; set; }
    public int NumberOfOverrides { get; set; }
    public List<TrimAssetOverride>? Overrides { get; set; }
    public string Description { get; set; } = null!;
    public int TrimPatternType { get; set; }
    public string TrimPatternTypeAssetName { get; set; } = null!;
    public int TemplateItem { get; set; }
    public string TrimPatternTypeDescription { get; set; } = null!;
    public bool Decal { get; set; }
    public bool ShowInTooltip { get; set; }
    
    public override void Parse(Queue<byte> data)
    {
        TrimMaterialType = dataTypes.ReadNextVarInt(data);

        if (TrimMaterialType == 0)
        {
            AssetName = dataTypes.ReadNextString(data);
            Ingredient = dataTypes.ReadNextVarInt(data);
            ItemModelIndex = dataTypes.ReadNextFloat(data);
            NumberOfOverrides = dataTypes.ReadNextVarInt(data);

            if (NumberOfOverrides > 0)
            {
                Overrides = [];

                for (var i = 0; i < NumberOfOverrides; i++)
                    Overrides.Add(new TrimAssetOverride(dataTypes.ReadNextVarInt(data),
                        dataTypes.ReadNextString(data)));
            }

            Description = ChatParser.ParseText(dataTypes.ReadNextString(data));
        }

        TrimPatternType = dataTypes.ReadNextVarInt(data);

        if (TrimPatternType == 0)
        {
            TrimPatternTypeAssetName = dataTypes.ReadNextString(data);
            TemplateItem = dataTypes.ReadNextVarInt(data);
            TrimPatternTypeDescription = dataTypes.ReadNextString(data);
            Decal = dataTypes.ReadNextBool(data);
        }

        ShowInTooltip = dataTypes.ReadNextBool(data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();

        data.AddRange(DataTypes.GetVarInt(TrimMaterialType));

        if (TrimMaterialType == 0)
        {
            if (string.IsNullOrEmpty(AssetName) || string.IsNullOrEmpty(Description))
                throw new NullReferenceException("Can't serialize the TrimComponent because the Asset Name or Description are null!");
            
            data.AddRange(DataTypes.GetString(AssetName));
            data.AddRange(DataTypes.GetVarInt(Ingredient));
            data.AddRange(DataTypes.GetFloat(ItemModelIndex));
            data.AddRange(DataTypes.GetVarInt(NumberOfOverrides));
            if (NumberOfOverrides > 0)
            {
                if(NumberOfOverrides != Overrides?.Count)
                    throw new NullReferenceException("Can't serialize the TrimComponent because value of NumberOfOverrides and the size of Overrides don't match!");
                
                foreach (var (armorMaterialType, assetName) in Overrides)
                {
                    data.AddRange(DataTypes.GetVarInt(armorMaterialType));
                    data.AddRange(DataTypes.GetString(assetName));
                }
            }
            data.AddRange(DataTypes.GetString(Description));

            data.AddRange(DataTypes.GetVarInt(TrimPatternType));
            if (TrimPatternType == 0)
            {
                if (string.IsNullOrEmpty(TrimPatternTypeAssetName) || string.IsNullOrEmpty(TrimPatternTypeDescription))
                    throw new NullReferenceException("Can't serialize the TrimComponent because the TrimPatternTypeAssetName or TrimPatternTypeDescription are null!");
                
                data.AddRange(DataTypes.GetString(TrimPatternTypeAssetName));
                data.AddRange(DataTypes.GetVarInt(TemplateItem));
                data.AddRange(DataTypes.GetString(TrimPatternTypeDescription));
                data.AddRange(DataTypes.GetBool(Decal));
            }
            
            data.AddRange(DataTypes.GetBool(ShowInTooltip));
        }
        
        return new Queue<byte>(data);
    }
}