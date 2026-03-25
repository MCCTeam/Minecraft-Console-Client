using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class CustomModelDataComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public List<float> Floats { get; set; } = [];
    public List<bool> Flags { get; set; } = [];
    public List<string> Strings { get; set; } = [];
    public List<int> Colors { get; set; } = [];
    
    public override void Parse(Queue<byte> data)
    {
        Floats = ReadList(data, static (dataTypes, componentData) => dataTypes.ReadNextFloat(componentData));
        Flags = ReadList(data, static (dataTypes, componentData) => dataTypes.ReadNextBool(componentData));
        Strings = ReadList(data, static (dataTypes, componentData) => dataTypes.ReadNextString(componentData));
        Colors = ReadList(data, static (dataTypes, componentData) => dataTypes.ReadNextInt(componentData));
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        WriteList(data, Floats, static (dataTypes, value) => dataTypes.GetFloat(value));
        WriteList(data, Flags, static (dataTypes, value) => dataTypes.GetBool(value));
        WriteList(data, Strings, static (dataTypes, value) => dataTypes.GetString(value));
        WriteList(data, Colors, static (_, value) => DataTypes.GetInt(value));
        return new Queue<byte>(data);
    }

    private List<T> ReadList<T>(Queue<byte> data, ReadDelegate<T> read)
    {
        var count = DataTypes.ReadNextVarInt(data);
        var values = new List<T>(count);

        for (var i = 0; i < count; i++)
            values.Add(read(DataTypes, data));

        return values;
    }

    private void WriteList<T>(List<byte> data, List<T> values, WriteDelegate<T> write)
    {
        data.AddRange(DataTypes.GetVarInt(values.Count));
        foreach (var value in values)
            data.AddRange(write(DataTypes, value));
    }

    private delegate T ReadDelegate<out T>(DataTypes dataTypes, Queue<byte> data);
    private delegate byte[] WriteDelegate<in T>(DataTypes dataTypes, T value);
}
