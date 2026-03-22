using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_2;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Registries;

public class StructuredComponentsRegistry1212 : StructuredComponentRegistry
{
    public StructuredComponentsRegistry1212(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
        : base(dataTypes, itemPalette, subComponentRegistry)
    {
        RegisterComponent<CustomDataComponent>(0, "minecraft:custom_data");
        RegisterComponent<MaxStackSizeComponent>(1, "minecraft:max_stack_size");
        RegisterComponent<MaxDamageComponent>(2, "minecraft:max_damage");
        RegisterComponent<DamageComponent>(3, "minecraft:damage");
        RegisterComponent<UnbrekableComponent1206>(4, "minecraft:unbreakable");
        RegisterComponent<CustomNameComponent>(5, "minecraft:custom_name");
        RegisterComponent<ItemNameComponent>(6, "minecraft:item_name");
        RegisterComponent<ItemModelComponent>(7, "minecraft:item_model");
        RegisterComponent<LoreNameComponent1206>(8, "minecraft:lore");
        RegisterComponent<RarityComponent>(9, "minecraft:rarity");
        RegisterComponent<EnchantmentsComponent>(10, "minecraft:enchantments");
        RegisterComponent<CanPlaceOnComponent>(11, "minecraft:can_place_on");
        RegisterComponent<CanBreakComponent>(12, "minecraft:can_break");
        RegisterComponent<AttributeModifiersComponent>(13, "minecraft:attribute_modifiers");
        RegisterComponent<CustomModelDataComponent>(14, "minecraft:custom_model_data");
        RegisterComponent<HideAdditionalTooltipComponent>(15, "minecraft:hide_additional_tooltip");
        RegisterComponent<HideTooltipComponent>(16, "minecraft:hide_tooltip");
        RegisterComponent<RepairCostComponent>(17, "minecraft:repair_cost");
        RegisterComponent<CreativeSlotLockComponent>(18, "minecraft:creative_slot_lock");
        RegisterComponent<EnchantmentGlintOverrideComponent>(19, "minecraft:enchantment_glint_override");
        RegisterComponent<IntangibleProjectileComponent>(20, "minecraft:intangible_projectile");
        RegisterComponent<FoodComponent1212>(21, "minecraft:food");
        RegisterComponent<ConsumableComponent>(22, "minecraft:consumable");
        RegisterComponent<UseRemainderComponent>(23, "minecraft:use_remainder");
        RegisterComponent<UseCooldownComponent>(24, "minecraft:use_cooldown");
        RegisterComponent<DamageResistantComponent>(25, "minecraft:damage_resistant");
        RegisterComponent<ToolComponent>(26, "minecraft:tool");
        RegisterComponent<EnchantableComponent>(27, "minecraft:enchantable");
        RegisterComponent<EquippableComponent>(28, "minecraft:equippable");
        RegisterComponent<RepairableComponent>(29, "minecraft:repairable");
        RegisterComponent<GliderComponent>(30, "minecraft:glider");
        RegisterComponent<TooltipStyleComponent>(31, "minecraft:tooltip_style");
        RegisterComponent<DeathProtectionComponent>(32, "minecraft:death_protection");
        RegisterComponent<StoredEnchantmentsComponent>(33, "minecraft:stored_enchantments");
        RegisterComponent<DyeColorComponent>(34, "minecraft:dyed_color");
        RegisterComponent<MapColorComponent>(35, "minecraft:map_color");
        RegisterComponent<MapIdComponent>(36, "minecraft:map_id");
        RegisterComponent<MapDecorationsComponent>(37, "minecraft:map_decorations");
        RegisterComponent<MapPostProcessingComponent>(38, "minecraft:map_post_processing");
        RegisterComponent<ChargedProjectilesComponent>(39, "minecraft:charged_projectiles");
        RegisterComponent<BundleContentsComponent>(40, "minecraft:bundle_contents");
        RegisterComponent<PotionContentsComponent>(41, "minecraft:potion_contents");
        RegisterComponent<SuspiciousStewEffectsComponent>(42, "minecraft:suspicious_stew_effects");
        RegisterComponent<WritableBlookContentComponent>(43, "minecraft:writable_book_content");
        RegisterComponent<WrittenBlookContentComponent>(44, "minecraft:written_book_content");
        RegisterComponent<TrimComponent>(45, "minecraft:trim");
        RegisterComponent<DebugStickStateComponent>(46, "minecraft:debug_stick_state");
        RegisterComponent<EntityDataComponent>(47, "minecraft:entity_data");
        RegisterComponent<BucketEntityDataComponent>(48, "minecraft:bucket_entity_data");
        RegisterComponent<BlockEntityDataComponent>(49, "minecraft:block_entity_data");
        RegisterComponent<InstrumentComponent>(50, "minecraft:instrument");
        RegisterComponent<OmniousBottleAmplifierComponent>(51, "minecraft:ominous_bottle_amplifier");
        RegisterComponent<JukeBoxPlayableComponent>(52, "minecraft:jukebox_playable");
        RegisterComponent<RecipesComponent>(53, "minecraft:recipes");
        RegisterComponent<LodestoneTrackerComponent>(54, "minecraft:lodestone_tracker");
        RegisterComponent<FireworkExplosionComponent>(55, "minecraft:firework_explosion");
        RegisterComponent<FireworksComponent>(56, "minecraft:fireworks");
        RegisterComponent<ProfileComponent>(57, "minecraft:profile");
        RegisterComponent<NoteBlockSoundComponent>(58, "minecraft:note_block_sound");
        RegisterComponent<BannerPatternsComponent>(59, "minecraft:banner_patterns");
        RegisterComponent<BaseColorComponent>(60, "minecraft:base_color");
        RegisterComponent<PotDecorationsComponent>(61, "minecraft:pot_decorations");
        RegisterComponent<ContainerComponent>(62, "minecraft:container");
        RegisterComponent<BlockStateComponent>(63, "minecraft:block_state");
        RegisterComponent<BeesComponent>(64, "minecraft:bees");
        RegisterComponent<LockComponent>(65, "minecraft:lock");
        RegisterComponent<ContainerLootComponent>(66, "minecraft:container_loot");
    }
}
