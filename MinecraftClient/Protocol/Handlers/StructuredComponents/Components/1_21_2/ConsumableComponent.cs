using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_21;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_2;

public class ConsumableComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public float ConsumeSeconds { get; set; }
    public int Animation { get; set; }
    public SoundEventSubComponent? Sound { get; set; }
    public bool HasConsumeParticles { get; set; }
    public List<ConsumeEffectData> Effects { get; set; } = new();

    public override void Parse(Queue<byte> data)
    {
        ConsumeSeconds = DataTypes.ReadNextFloat(data);
        Animation = DataTypes.ReadNextVarInt(data);
        Sound = (SoundEventSubComponent)SubComponentRegistry.ParseSubComponent(SubComponents.SoundEvent, data);
        HasConsumeParticles = DataTypes.ReadNextBool(data);

        var effectCount = DataTypes.ReadNextVarInt(data);
        for (var i = 0; i < effectCount; i++)
        {
            var effectTypeId = DataTypes.ReadNextVarInt(data);
            var effectData = ReadConsumeEffectPayload(effectTypeId, data);
            Effects.Add(new ConsumeEffectData(effectTypeId, effectData));
        }
    }

    private byte[] ReadConsumeEffectPayload(int effectTypeId, Queue<byte> data)
    {
        var payload = new List<byte>();
        switch (effectTypeId)
        {
            case 0: // apply_effects: List<MobEffectInstance> + probability(float)
                var effectCount = DataTypes.ReadNextVarInt(data);
                payload.AddRange(DataTypes.GetVarInt(effectCount));
                for (var i = 0; i < effectCount; i++)
                    payload.AddRange(ReadMobEffectInstance(data));
                payload.AddRange(DataTypes.GetFloat(DataTypes.ReadNextFloat(data)));
                break;
            case 1: // remove_effects: HolderSet<MobEffect>
                payload.AddRange(ReadHolderSet(data));
                break;
            case 2: // clear_all_effects: empty
                break;
            case 3: // teleport_randomly: float diameter
                payload.AddRange(DataTypes.GetFloat(DataTypes.ReadNextFloat(data)));
                break;
            case 4: // play_sound: Holder<SoundEvent>
                var sound = (SoundEventSubComponent)SubComponentRegistry.ParseSubComponent(SubComponents.SoundEvent, data);
                payload.AddRange(sound.Serialize());
                break;
        }
        return payload.ToArray();
    }

    private byte[] ReadMobEffectInstance(Queue<byte> data)
    {
        var result = new List<byte>();
        var effectId = DataTypes.ReadNextVarInt(data);
        result.AddRange(DataTypes.GetVarInt(effectId));
        result.AddRange(ReadMobEffectDetails(data));
        return result.ToArray();
    }

    private byte[] ReadMobEffectDetails(Queue<byte> data)
    {
        var result = new List<byte>();
        var amplifier = DataTypes.ReadNextVarInt(data);
        result.AddRange(DataTypes.GetVarInt(amplifier));
        var duration = DataTypes.ReadNextVarInt(data);
        result.AddRange(DataTypes.GetVarInt(duration));
        var ambient = DataTypes.ReadNextBool(data);
        result.AddRange(DataTypes.GetBool(ambient));
        var showParticles = DataTypes.ReadNextBool(data);
        result.AddRange(DataTypes.GetBool(showParticles));
        var showIcon = DataTypes.ReadNextBool(data);
        result.AddRange(DataTypes.GetBool(showIcon));
        var hasHiddenEffect = DataTypes.ReadNextBool(data);
        result.AddRange(DataTypes.GetBool(hasHiddenEffect));
        if (hasHiddenEffect)
            result.AddRange(ReadMobEffectDetails(data));
        return result.ToArray();
    }

    private byte[] ReadHolderSet(Queue<byte> data)
    {
        var result = new List<byte>();
        var type = DataTypes.ReadNextVarInt(data);
        result.AddRange(DataTypes.GetVarInt(type));
        if (type == 0)
        {
            var tagName = DataTypes.ReadNextString(data);
            result.AddRange(DataTypes.GetString(tagName));
        }
        else
        {
            for (var i = 0; i < type - 1; i++)
            {
                var id = DataTypes.ReadNextVarInt(data);
                result.AddRange(DataTypes.GetVarInt(id));
            }
        }
        return result.ToArray();
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetFloat(ConsumeSeconds));
        data.AddRange(DataTypes.GetVarInt(Animation));
        if (Sound is not null) data.AddRange(Sound.Serialize());
        data.AddRange(DataTypes.GetBool(HasConsumeParticles));
        data.AddRange(DataTypes.GetVarInt(Effects.Count));
        foreach (var effect in Effects)
        {
            data.AddRange(DataTypes.GetVarInt(effect.EffectTypeId));
            data.AddRange(effect.Payload);
        }
        return new Queue<byte>(data);
    }

    public record ConsumeEffectData(int EffectTypeId, byte[] Payload);
}
