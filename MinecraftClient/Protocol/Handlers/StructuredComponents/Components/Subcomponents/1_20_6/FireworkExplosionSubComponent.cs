using System;
using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;

public class FireworkExplosionSubComponent(DataTypes dataTypes, SubComponentRegistry subComponentRegistry) : SubComponent(dataTypes, subComponentRegistry)
{
    public int Shape { get; set; }
    public int NumberOfColors { get; set; }
    public List<int> Colors { get; set; } = [];
    public int NumberOfFadeColors { get; set; }
    public List<int> FadeColors { get; set; } = [];
    public bool HasTrail { get; set; }
    public bool HasTwinkle { get; set; }
    
    protected override void Parse(Queue<byte> data)
    {
        Shape = dataTypes.ReadNextVarInt(data);
        NumberOfColors = dataTypes.ReadNextVarInt(data);

        for (var i = 0; i < NumberOfColors; i++)
            Colors.Add(dataTypes.ReadNextInt(data));
        
        NumberOfFadeColors = dataTypes.ReadNextVarInt(data);

        for (var i = 0; i < NumberOfFadeColors; i++)
            FadeColors.Add(dataTypes.ReadNextInt(data));
        
        HasTrail = dataTypes.ReadNextBool(data);
        HasTwinkle = dataTypes.ReadNextBool(data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(Shape));

        data.AddRange(DataTypes.GetVarInt(NumberOfColors));
        if (NumberOfColors > 0)
        {
            if (NumberOfColors != Colors.Count)
                throw new Exception("Can't serialize FireworkExplosionComponent because NumberOfColors and the length of Colors list differ!");

            foreach (var color in Colors)
                data.AddRange(DataTypes.GetInt(color));
        }
        
        data.AddRange(DataTypes.GetVarInt(NumberOfFadeColors));
        if (NumberOfFadeColors > 0)
        {
            if (NumberOfFadeColors != FadeColors.Count)
                throw new Exception("Can't serialize FireworkExplosionComponent because NumberOfFadeColors and the length of FadeColors list differ!");

            foreach (var fadeColor in FadeColors)
                data.AddRange(DataTypes.GetInt(fadeColor));
        }
        
        data.AddRange(DataTypes.GetBool(HasTrail));
        data.AddRange(DataTypes.GetBool(HasTwinkle));
        return new Queue<byte>(data);
    }
}