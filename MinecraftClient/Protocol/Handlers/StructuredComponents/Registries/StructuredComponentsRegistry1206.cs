using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Registries;

public class StructuredComponentsRegistry1206 : StructuredComponentRegistry
{
    public StructuredComponentsRegistry1206(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
        : base(dataTypes, itemPalette, subComponentRegistry)
    {
        RegisterComponent<CustomDataComponent>(0, "minecraft:custom_data");
        RegisterComponent<MaxStackSizeComponent>(1, "minecraft:max_stack_size");
        RegisterComponent<MaxDamageComponent>(2, "minecraft:max_damage");
        RegisterComponent<DamageComponent>(3, "minecraft:damage");
        RegisterComponent<UnbrekableComponent1206>(4, "minecraft:unbreakable");
        RegisterComponent<CustomNameComponent>(5, "minecraft:custom_name");
        RegisterComponent<ItemNameComponent>(6, "minecraft:item_name");
        RegisterComponent<LoreNameComponent1206>(7, "minecraft:lore");
        RegisterComponent<RarityComponent>(8, "minecraft:rarity");
        RegisterComponent<EnchantmentsComponent>(9, "minecraft:enchantments");
        RegisterComponent<CanPlaceOnComponent>(10, "minecraft:can_place_on");
        RegisterComponent<CanBreakComponent>(11, "minecraft:can_break");
        RegisterComponent<AttributeModifiersComponent>(12, "minecraft:attribute_modifiers");
        RegisterComponent<CustomModelDataComponent>(13, "minecraft:custom_model_data");
        RegisterComponent<HideAdditionalTooltipComponent>(14, "minecraft:hide_additional_tooltip");
        RegisterComponent<HideTooltipComponent>(15, "minecraft:hide_tooltip");
        RegisterComponent<HideTooltipComponent>(16, "minecraft:repair_cost");
        RegisterComponent<CreativeSlotLockComponent>(17, "minecraft:creative_slot_lock");
        RegisterComponent<EnchantmentGlintOverrideComponent>(18, "minecraft:enchantment_glint_override");
        RegisterComponent<IntangibleProjectileComponent>(19, "minecraft:intangible_projectile");
        RegisterComponent<FoodComponentComponent>(20, "minecraft:food");
        RegisterComponent<FireResistantComponent>(21, "minecraft:fire_resistant");
        RegisterComponent<ToolComponent>(22, "minecraft:tool");
        RegisterComponent<StoredEnchantmentsComponent>(23, "minecraft:stored_enchantments");
        RegisterComponent<DyeColorComponent>(24, "minecraft:dyed_color");
        RegisterComponent<MapColorComponent>(25, "minecraft:map_color");
        RegisterComponent<MapIdComponent>(26, "minecraft:map_id");
        RegisterComponent<MapDecorationsComponent>(27, "minecraft:map_decorations");
        RegisterComponent<MapPostProcessingComponent>(28, "minecraft:map_post_processing");
        RegisterComponent<ChargedProjectilesComponent>(29, "minecraft:charged_projectiles");
        RegisterComponent<BundleContentsComponent>(30, "minecraft:bundle_contents");
        RegisterComponent<PotionContentsComponent>(31, "minecraft:potion_contents");
        RegisterComponent<SuspiciousStewEffectsComponent>(32, "minecraft:suspicious_stew_effects");
        RegisterComponent<WritableBlookContentComponent>(33, "minecraft:writable_book_content");
        RegisterComponent<WrittenBlookContentComponent>(34, "minecraft:written_book_content");
        RegisterComponent<TrimComponent>(35, "minecraft:trim");
        RegisterComponent<DebugStickStateComponent>(36, "minecraft:debug_stick_state");
        RegisterComponent<EntityDataComponent>(37, "minecraft:entity_data");
        RegisterComponent<BucketEntityDataComponent>(38, "minecraft:bucket_entity_data");
        RegisterComponent<BlockEntityDataComponent>(39, "minecraft:block_entity_data");
        RegisterComponent<InstrumentComponent>(40, "minecraft:instrument");
        RegisterComponent<OmniousBottleAmplifierComponent>(41, "minecraft:ominous_bottle_amplifier");
        RegisterComponent<RecipesComponent>(42, "minecraft:recipes");
        RegisterComponent<LodestoneTrackerComponent>(43, "minecraft:lodestone_tracker");
        RegisterComponent<FireworkExplosionComponent>(44, "minecraft:firework_explosion");
        RegisterComponent<FireworksComponent>(45, "minecraft:fireworks");
        RegisterComponent<ProfileComponent>(46, "minecraft:profile");
        RegisterComponent<NoteBlockSoundComponent>(47, "minecraft:note_block_sound");
        RegisterComponent<BannerPatternsComponent>(48, "minecraft:banner_patterns");
        RegisterComponent<BaseColorComponent>(49, "minecraft:base_color");
        RegisterComponent<PotDecorationsComponent>(50, "minecraft:pot_decorations");
        RegisterComponent<ContainerComponent>(51, "minecraft:container");
        RegisterComponent<BlockStateComponent>(52, "minecraft:block_state");
        RegisterComponent<BeesComponent>(53, "minecraft:bees");
        RegisterComponent<LockComponent>(54, "minecraft:lock");
        RegisterComponent<ContainerLootComponent>(55, "minecraft:container_loot");
    }
}