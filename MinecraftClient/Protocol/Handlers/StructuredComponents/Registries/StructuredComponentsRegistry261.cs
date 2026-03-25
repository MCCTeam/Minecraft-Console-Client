using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_2;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_5;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_11;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Registries;

public class StructuredComponentsRegistry261 : StructuredComponentRegistry
{
    public StructuredComponentsRegistry261(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
        : base(dataTypes, itemPalette, subComponentRegistry)
    {
        RegisterComponent<CustomDataComponent>(0, "minecraft:custom_data");
        RegisterComponent<MaxStackSizeComponent>(1, "minecraft:max_stack_size");
        RegisterComponent<MaxDamageComponent>(2, "minecraft:max_damage");
        RegisterComponent<DamageComponent>(3, "minecraft:damage");
        RegisterComponent<EmptyComponent>(4, "minecraft:unbreakable");
        RegisterComponent<UseEffectsComponent>(5, "minecraft:use_effects");
        RegisterComponent<CustomNameComponent>(6, "minecraft:custom_name");
        RegisterComponent<PotionDurationScaleComponent>(7, "minecraft:minimum_attack_charge");
        RegisterComponent<RegistryEitherHolderComponent>(8, "minecraft:damage_type");
        RegisterComponent<ItemNameComponent>(9, "minecraft:item_name");
        RegisterComponent<ItemModelComponent>(10, "minecraft:item_model");
        RegisterComponent<LoreNameComponent1206>(11, "minecraft:lore");
        RegisterComponent<RarityComponent>(12, "minecraft:rarity");
        RegisterComponent<EnchantmentsComponent1215>(13, "minecraft:enchantments");
        RegisterComponent<CanPlaceOnComponent1215>(14, "minecraft:can_place_on");
        RegisterComponent<CanBreakComponent1215>(15, "minecraft:can_break");
        RegisterComponent<AttributeModifiersComponent>(16, "minecraft:attribute_modifiers");
        RegisterComponent<CustomModelDataComponent>(17, "minecraft:custom_model_data");
        RegisterComponent<TooltipDisplayComponent>(18, "minecraft:tooltip_display");
        RegisterComponent<RepairCostComponent>(19, "minecraft:repair_cost");
        RegisterComponent<CreativeSlotLockComponent>(20, "minecraft:creative_slot_lock");
        RegisterComponent<EnchantmentGlintOverrideComponent>(21, "minecraft:enchantment_glint_override");
        RegisterComponent<IntangibleProjectileComponent>(22, "minecraft:intangible_projectile");
        RegisterComponent<FoodComponent1212>(23, "minecraft:food");
        RegisterComponent<ConsumableComponent>(24, "minecraft:consumable");
        RegisterComponent<UseRemainderComponent>(25, "minecraft:use_remainder");
        RegisterComponent<UseCooldownComponent>(26, "minecraft:use_cooldown");
        RegisterComponent<DamageResistantComponent>(27, "minecraft:damage_resistant");
        RegisterComponent<ToolComponent>(28, "minecraft:tool");
        RegisterComponent<WeaponComponent>(29, "minecraft:weapon");
        RegisterComponent<AttackRangeComponent>(30, "minecraft:attack_range");
        RegisterComponent<EnchantableComponent>(31, "minecraft:enchantable");
        RegisterComponent<EquippableComponent>(32, "minecraft:equippable");
        RegisterComponent<RepairableComponent>(33, "minecraft:repairable");
        RegisterComponent<GliderComponent>(34, "minecraft:glider");
        RegisterComponent<TooltipStyleComponent>(35, "minecraft:tooltip_style");
        RegisterComponent<DeathProtectionComponent>(36, "minecraft:death_protection");
        RegisterComponent<BlocksAttacksComponent>(37, "minecraft:blocks_attacks");
        RegisterComponent<PiercingWeaponComponent>(38, "minecraft:piercing_weapon");
        RegisterComponent<KineticWeaponComponent>(39, "minecraft:kinetic_weapon");
        RegisterComponent<SwingAnimationComponent>(40, "minecraft:swing_animation");
        RegisterComponent<VarIntComponent>(41, "minecraft:additional_trade_cost");     // New in 26.1
        RegisterComponent<StoredEnchantmentsComponent1215>(42, "minecraft:stored_enchantments");
        RegisterComponent<VarIntComponent>(43, "minecraft:dye");                       // New in 26.1
        RegisterComponent<DyeColorComponent1215>(44, "minecraft:dyed_color");
        RegisterComponent<MapColorComponent>(45, "minecraft:map_color");
        RegisterComponent<MapIdComponent>(46, "minecraft:map_id");
        RegisterComponent<MapDecorationsComponent>(47, "minecraft:map_decorations");
        RegisterComponent<MapPostProcessingComponent>(48, "minecraft:map_post_processing");
        RegisterComponent<ChargedProjectilesComponent>(49, "minecraft:charged_projectiles");
        RegisterComponent<BundleContentsComponent>(50, "minecraft:bundle_contents");
        RegisterComponent<PotionContentsComponent>(51, "minecraft:potion_contents");
        RegisterComponent<PotionDurationScaleComponent>(52, "minecraft:potion_duration_scale");
        RegisterComponent<SuspiciousStewEffectsComponent>(53, "minecraft:suspicious_stew_effects");
        RegisterComponent<WritableBlookContentComponent>(54, "minecraft:writable_book_content");
        RegisterComponent<WrittenBlookContentComponent>(55, "minecraft:written_book_content");
        RegisterComponent<TrimComponent1215>(56, "minecraft:trim");
        RegisterComponent<DebugStickStateComponent>(57, "minecraft:debug_stick_state");
        RegisterComponent<EntityDataComponent>(58, "minecraft:entity_data");
        RegisterComponent<BucketEntityDataComponent>(59, "minecraft:bucket_entity_data");
        RegisterComponent<BlockEntityDataComponent>(60, "minecraft:block_entity_data");
        RegisterComponent<InstrumentComponent1215>(61, "minecraft:instrument");
        RegisterComponent<ProvidesTrimMaterialComponent>(62, "minecraft:provides_trim_material");
        RegisterComponent<OmniousBottleAmplifierComponent>(63, "minecraft:ominous_bottle_amplifier");
        RegisterComponent<JukeBoxPlayableComponent>(64, "minecraft:jukebox_playable");
        RegisterComponent<ProvidesBannerPatternsComponent>(65, "minecraft:provides_banner_patterns");
        RegisterComponent<RecipesComponent>(66, "minecraft:recipes");
        RegisterComponent<LodestoneTrackerComponent>(67, "minecraft:lodestone_tracker");
        RegisterComponent<FireworkExplosionComponent>(68, "minecraft:firework_explosion");
        RegisterComponent<FireworksComponent>(69, "minecraft:fireworks");
        RegisterComponent<ProfileComponent>(70, "minecraft:profile");
        RegisterComponent<NoteBlockSoundComponent>(71, "minecraft:note_block_sound");
        RegisterComponent<BannerPatternsComponent>(72, "minecraft:banner_patterns");
        RegisterComponent<BaseColorComponent>(73, "minecraft:base_color");
        RegisterComponent<PotDecorationsComponent>(74, "minecraft:pot_decorations");
        RegisterComponent<ContainerComponent>(75, "minecraft:container");
        RegisterComponent<BlockStateComponent>(76, "minecraft:block_state");
        RegisterComponent<BeesComponent>(77, "minecraft:bees");
        RegisterComponent<LockComponent>(78, "minecraft:lock");
        RegisterComponent<ContainerLootComponent>(79, "minecraft:container_loot");

        RegisterComponent<SoundEventHolderComponent>(80, "minecraft:break_sound");
        RegisterComponent<VarIntComponent>(81, "minecraft:villager/variant");
        RegisterComponent<VarIntComponent>(82, "minecraft:wolf/variant");
        RegisterComponent<VarIntComponent>(83, "minecraft:wolf/sound_variant");
        RegisterComponent<VarIntComponent>(84, "minecraft:wolf/collar");
        RegisterComponent<VarIntComponent>(85, "minecraft:fox/variant");
        RegisterComponent<VarIntComponent>(86, "minecraft:salmon/size");
        RegisterComponent<VarIntComponent>(87, "minecraft:parrot/variant");
        RegisterComponent<VarIntComponent>(88, "minecraft:tropical_fish/pattern");
        RegisterComponent<VarIntComponent>(89, "minecraft:tropical_fish/base_color");
        RegisterComponent<VarIntComponent>(90, "minecraft:tropical_fish/pattern_color");
        RegisterComponent<VarIntComponent>(91, "minecraft:mooshroom/variant");
        RegisterComponent<VarIntComponent>(92, "minecraft:rabbit/variant");
        RegisterComponent<VarIntComponent>(93, "minecraft:pig/variant");
        RegisterComponent<VarIntComponent>(94, "minecraft:pig/sound_variant");           // New in 26.1
        RegisterComponent<VarIntComponent>(95, "minecraft:cow/variant");
        RegisterComponent<VarIntComponent>(96, "minecraft:cow/sound_variant");           // New in 26.1
        RegisterComponent<EitherHolderComponent>(97, "minecraft:chicken/variant");
        RegisterComponent<VarIntComponent>(98, "minecraft:chicken/sound_variant");       // New in 26.1
        RegisterComponent<RegistryEitherHolderComponent>(99, "minecraft:zombie_nautilus/variant");
        RegisterComponent<VarIntComponent>(100, "minecraft:frog/variant");
        RegisterComponent<VarIntComponent>(101, "minecraft:horse/variant");
        RegisterComponent<PaintingVariantHolderComponent>(102, "minecraft:painting/variant");
        RegisterComponent<VarIntComponent>(103, "minecraft:llama/variant");
        RegisterComponent<VarIntComponent>(104, "minecraft:axolotl/variant");
        RegisterComponent<VarIntComponent>(105, "minecraft:cat/variant");
        RegisterComponent<VarIntComponent>(106, "minecraft:cat/sound_variant");          // New in 26.1
        RegisterComponent<VarIntComponent>(107, "minecraft:cat/collar");
        RegisterComponent<VarIntComponent>(108, "minecraft:sheep/color");
        RegisterComponent<VarIntComponent>(109, "minecraft:shulker/color");
    }
}
