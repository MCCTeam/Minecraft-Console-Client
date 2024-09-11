using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Registries;

public class StructuredComponentsRegistry1206 : StructuredComponentRegistry
{
    public StructuredComponentsRegistry1206(SubComponentRegistry subComponentRegistry, DataTypes dataTypes) : base(
        subComponentRegistry, dataTypes)
    {
        RegisterComponent<CustomDataComponent1206>(0, "minecraft:custom_data");
        RegisterComponent<MaxStackSizeComponent1206>(1, "minecraft:max_stack_size");
        RegisterComponent<MaxDamageComponent1206>(2, "minecraft:max_damage");
        RegisterComponent<DamageComponent1206>(3, "minecraft:damage");
        RegisterComponent<UnbrekableComponent1206>(4, "minecraft:unbreakable");
        RegisterComponent<CustomNameComponent1206>(5, "minecraft:custom_name");
        RegisterComponent<ItemNameComponent1206>(6, "minecraft:item_name");
        RegisterComponent<LoreNameComponent1206>(7, "minecraft:lore");
        RegisterComponent<RarityComponent1206>(8, "minecraft:rarity");
        RegisterComponent<EnchantmentsComponent1206>(9, "minecraft:enchantments");
        RegisterComponent<CanPlaceOnComponent1206>(10, "minecraft:can_place_on");
        RegisterComponent<CanBreakComponent1206>(11, "minecraft:can_break");
        RegisterComponent<AttributeModifiersComponent1206>(12, "minecraft:attribute_modifiers");
        RegisterComponent<CustomModelDataComponent1206>(13, "minecraft:custom_model_data");
        RegisterComponent<HideAdditionalTooltipComponent1206>(14, "minecraft:hide_additional_tooltip");
        RegisterComponent<HideTooltipComponent1206>(15, "minecraft:hide_tooltip");
    }
}