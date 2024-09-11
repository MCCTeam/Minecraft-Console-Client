using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Registries;

public class StructuredComponentsRegistry1206 : StructuredComponentRegistry
{
    public StructuredComponentsRegistry1206(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
        : base(dataTypes, itemPalette, subComponentRegistry)
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
        RegisterComponent<HideTooltipComponent1206>(16, "minecraft:repair_cost");
        RegisterComponent<CreativeSlotLockComponent1206>(17, "minecraft:creative_slot_lock");
        RegisterComponent<EnchantmentGlintOverrideComponent1206>(18, "minecraft:enchantment_glint_override");
        RegisterComponent<IntangibleProjectileComponent1206>(19, "minecraft:intangible_projectile");
        RegisterComponent<FoodComponentComponent1206>(20, "minecraft:food");
        RegisterComponent<FireResistantComponent1206>(21, "minecraft:fire_resistant");
        RegisterComponent<ToolComponent1206>(22, "minecraft:tool");
        RegisterComponent<StoredEnchantmentsComponent1206>(23, "minecraft:stored_enchantments");
        RegisterComponent<DyeColorComponent1206>(24, "minecraft:dyed_color");
        RegisterComponent<MapColorComponent1206>(25, "minecraft:map_color");
        RegisterComponent<MapIdComponent1206>(26, "minecraft:map_id");
        RegisterComponent<MapDecorationsComponent1206>(27, "minecraft:map_decorations");
        RegisterComponent<MapPostProcessingComponent1206>(28, "minecraft:map_post_processing");
        RegisterComponent<ChargedProjectilesComponent1206>(29, "minecraft:charged_projectiles");
        RegisterComponent<BundleContentsComponent1206>(30, "minecraft:bundle_contents");
        RegisterComponent<PotionContentsComponentComponent1206>(31, "minecraft:potion_contents");
        
    }
}