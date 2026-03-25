using System;
using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_5;

/// <summary>
/// 1.21.5+ trim uses ArmorTrim.STREAM_CODEC:
/// Holder&lt;TrimMaterial&gt; + Holder&lt;TrimPattern&gt;.
/// The holder codec is ByteBufCodecs.holder(), so 0 means direct inline data and non-zero means registry reference.
/// </summary>
public class TrimComponent1215(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int MaterialHolderValue { get; set; }
    public DirectTrimMaterial1215? DirectMaterial { get; set; }
    public int PatternHolderValue { get; set; }
    public DirectTrimPattern1215? DirectPattern { get; set; }

    public override void Parse(Queue<byte> data)
    {
        MaterialHolderValue = DataTypes.ReadNextVarInt(data);
        if (MaterialHolderValue == 0)
            DirectMaterial = ParseDirectMaterial(data);

        PatternHolderValue = DataTypes.ReadNextVarInt(data);
        if (PatternHolderValue == 0)
            DirectPattern = ParseDirectPattern(data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();

        data.AddRange(DataTypes.GetVarInt(MaterialHolderValue));
        if (MaterialHolderValue == 0)
        {
            if (DirectMaterial is null)
                throw new ArgumentNullException(nameof(DirectMaterial), "Direct trim material payload is required when holder value is 0.");

            data.AddRange(DataTypes.GetString(DirectMaterial.BaseSuffix));
            data.AddRange(DataTypes.GetVarInt(DirectMaterial.OverrideSuffixes.Count));
            foreach (var (assetKey, suffix) in DirectMaterial.OverrideSuffixes)
            {
                data.AddRange(DataTypes.GetString(assetKey));
                data.AddRange(DataTypes.GetString(suffix));
            }

            data.AddRange(DataTypes.GetNbt(DirectMaterial.DescriptionNbt));
        }

        data.AddRange(DataTypes.GetVarInt(PatternHolderValue));
        if (PatternHolderValue == 0)
        {
            if (DirectPattern is null)
                throw new ArgumentNullException(nameof(DirectPattern), "Direct trim pattern payload is required when holder value is 0.");

            data.AddRange(DataTypes.GetString(DirectPattern.AssetId));
            data.AddRange(DataTypes.GetNbt(DirectPattern.DescriptionNbt));
            data.AddRange(DataTypes.GetBool(DirectPattern.Decal));
        }

        return new Queue<byte>(data);
    }

    private DirectTrimMaterial1215 ParseDirectMaterial(Queue<byte> data)
    {
        var baseSuffix = DataTypes.ReadNextString(data);
        var overrideCount = DataTypes.ReadNextVarInt(data);
        var overrideSuffixes = new Dictionary<string, string>(overrideCount, StringComparer.Ordinal);

        for (var i = 0; i < overrideCount; i++)
        {
            var assetKey = DataTypes.ReadNextString(data);
            var suffix = DataTypes.ReadNextString(data);
            overrideSuffixes[assetKey] = suffix;
        }

        var descriptionNbt = DataTypes.ReadNextNbt(data);
        var description = ChatParser.ParseText(descriptionNbt);
        return new DirectTrimMaterial1215(baseSuffix, overrideSuffixes, descriptionNbt, description);
    }

    private DirectTrimPattern1215 ParseDirectPattern(Queue<byte> data)
    {
        var assetId = DataTypes.ReadNextString(data);
        var descriptionNbt = DataTypes.ReadNextNbt(data);
        var description = ChatParser.ParseText(descriptionNbt);
        var decal = DataTypes.ReadNextBool(data);
        return new DirectTrimPattern1215(assetId, descriptionNbt, description, decal);
    }
}

public sealed record DirectTrimMaterial1215(
    string BaseSuffix,
    Dictionary<string, string> OverrideSuffixes,
    Dictionary<string, object> DescriptionNbt,
    string Description);

public sealed record DirectTrimPattern1215(
    string AssetId,
    Dictionary<string, object> DescriptionNbt,
    string Description,
    bool Decal);
