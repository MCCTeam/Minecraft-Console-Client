using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Registries.Subcomponents;

public class SubComponentRegistry1206 : SubComponentRegistry
{
    public SubComponentRegistry1206(DataTypes dataTypes) : base(dataTypes)
    {
        RegisterSubComponent<BlockPredicateSubcomponent>(SubComponents.BlockPredicate);
        RegisterSubComponent<BlockSetSubcomponent>(SubComponents.BlockSet);
        RegisterSubComponent<PropertySubComponent>(SubComponents.Property);
        RegisterSubComponent<AttributeSubComponent>(SubComponents.Attribute);
        RegisterSubComponent<EffectSubComponent>(SubComponents.Effect);
        RegisterSubComponent<PotionEffectSubComponent>(SubComponents.PotionEffect);
        RegisterSubComponent<DetailsSubComponent>(SubComponents.Details);
        RegisterSubComponent<RuleSubComponent>(SubComponents.Rule);
        RegisterSubComponent<FireworkExplosionSubComponent>(SubComponents.FireworkExplosion);
    }
}