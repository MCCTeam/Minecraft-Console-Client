using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_2;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_5;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_8;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_9;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Registries;

public class StructuredComponentsRegistry1215 : StructuredComponentRegistry
{
    public StructuredComponentsRegistry1215(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
        : base(dataTypes, itemPalette, subComponentRegistry)
    {
        var uses1218AttributeAndEquippableFormats = dataTypes.ProtocolVersion >= Protocol18Handler.MC_1_21_6_Version;
        var usesTypedBeesFormat = dataTypes.ProtocolVersion >= Protocol18Handler.MC_1_21_9_Version;

        RegisterComponent<CustomDataComponent>(0, "minecraft:custom_data");
        RegisterComponent<MaxStackSizeComponent>(1, "minecraft:max_stack_size");
        RegisterComponent<MaxDamageComponent>(2, "minecraft:max_damage");
        RegisterComponent<DamageComponent>(3, "minecraft:damage");
        RegisterComponent<EmptyComponent>(4, "minecraft:unbreakable"); // Changed from Unbreakable (Bool) to Unit (empty) in 1.21.5
        RegisterComponent<CustomNameComponent>(5, "minecraft:custom_name");
        RegisterComponent<ItemNameComponent>(6, "minecraft:item_name");
        RegisterComponent<ItemModelComponent>(7, "minecraft:item_model");
        RegisterComponent<LoreNameComponent1206>(8, "minecraft:lore");
        RegisterComponent<RarityComponent>(9, "minecraft:rarity");
        RegisterComponent<EnchantmentsComponent1215>(10, "minecraft:enchantments");
        RegisterComponent<CanPlaceOnComponent>(11, "minecraft:can_place_on");
        RegisterComponent<CanBreakComponent>(12, "minecraft:can_break");
        if (uses1218AttributeAndEquippableFormats)
            RegisterComponent<AttributeModifiersComponent1218>(13, "minecraft:attribute_modifiers");
        else
            RegisterComponent<AttributeModifiersComponent1215>(13, "minecraft:attribute_modifiers");
        RegisterComponent<CustomModelDataComponent>(14, "minecraft:custom_model_data");
        // 15: tooltip_display (NEW, replaces hide_additional_tooltip + hide_tooltip)
        RegisterComponent<TooltipDisplayComponent>(15, "minecraft:tooltip_display");
        RegisterComponent<RepairCostComponent>(16, "minecraft:repair_cost");
        RegisterComponent<CreativeSlotLockComponent>(17, "minecraft:creative_slot_lock");
        RegisterComponent<EnchantmentGlintOverrideComponent>(18, "minecraft:enchantment_glint_override");
        RegisterComponent<IntangibleProjectileComponent>(19, "minecraft:intangible_projectile");
        RegisterComponent<FoodComponent1212>(20, "minecraft:food");
        RegisterComponent<ConsumableComponent>(21, "minecraft:consumable");
        RegisterComponent<UseRemainderComponent>(22, "minecraft:use_remainder");
        RegisterComponent<UseCooldownComponent>(23, "minecraft:use_cooldown");
        RegisterComponent<DamageResistantComponent>(24, "minecraft:damage_resistant");
        RegisterComponent<ToolComponent1215>(25, "minecraft:tool");
        RegisterComponent<WeaponComponent>(26, "minecraft:weapon"); // NEW
        RegisterComponent<EnchantableComponent>(27, "minecraft:enchantable");
        if (uses1218AttributeAndEquippableFormats)
            RegisterComponent<EquippableComponent1218>(28, "minecraft:equippable");
        else
            RegisterComponent<EquippableComponent1215>(28, "minecraft:equippable");
        RegisterComponent<RepairableComponent>(29, "minecraft:repairable");
        RegisterComponent<GliderComponent>(30, "minecraft:glider");
        RegisterComponent<TooltipStyleComponent>(31, "minecraft:tooltip_style");
        RegisterComponent<DeathProtectionComponent>(32, "minecraft:death_protection");
        RegisterComponent<BlocksAttacksComponent>(33, "minecraft:blocks_attacks"); // NEW
        RegisterComponent<StoredEnchantmentsComponent1215>(34, "minecraft:stored_enchantments");
        RegisterComponent<DyeColorComponent>(35, "minecraft:dyed_color");
        RegisterComponent<MapColorComponent>(36, "minecraft:map_color");
        RegisterComponent<MapIdComponent>(37, "minecraft:map_id");
        RegisterComponent<MapDecorationsComponent>(38, "minecraft:map_decorations");
        RegisterComponent<MapPostProcessingComponent>(39, "minecraft:map_post_processing");
        RegisterComponent<ChargedProjectilesComponent>(40, "minecraft:charged_projectiles");
        RegisterComponent<BundleContentsComponent>(41, "minecraft:bundle_contents");
        RegisterComponent<PotionContentsComponent1212>(42, "minecraft:potion_contents");
        RegisterComponent<PotionDurationScaleComponent>(43, "minecraft:potion_duration_scale"); // NEW
        RegisterComponent<SuspiciousStewEffectsComponent>(44, "minecraft:suspicious_stew_effects");
        RegisterComponent<WritableBlookContentComponent>(45, "minecraft:writable_book_content");
        RegisterComponent<WrittenBlookContentComponent>(46, "minecraft:written_book_content");
        RegisterComponent<TrimComponent>(47, "minecraft:trim");
        RegisterComponent<DebugStickStateComponent>(48, "minecraft:debug_stick_state");
        RegisterComponent<EntityDataComponent>(49, "minecraft:entity_data");
        RegisterComponent<BucketEntityDataComponent>(50, "minecraft:bucket_entity_data");
        RegisterComponent<BlockEntityDataComponent>(51, "minecraft:block_entity_data");
        RegisterComponent<InstrumentComponent1215>(52, "minecraft:instrument"); // Changed to EitherHolder<Instrument> in 1.21.5
        RegisterComponent<ProvidesTrimMaterialComponent>(53, "minecraft:provides_trim_material"); // NEW
        RegisterComponent<OmniousBottleAmplifierComponent>(54, "minecraft:ominous_bottle_amplifier");
        RegisterComponent<JukeBoxPlayableComponent1215>(55, "minecraft:jukebox_playable");
        RegisterComponent<ProvidesBannerPatternsComponent>(56, "minecraft:provides_banner_patterns"); // NEW
        RegisterComponent<RecipesComponent>(57, "minecraft:recipes");
        RegisterComponent<LodestoneTrackerComponent>(58, "minecraft:lodestone_tracker");
        RegisterComponent<FireworkExplosionComponent>(59, "minecraft:firework_explosion");
        RegisterComponent<FireworksComponent>(60, "minecraft:fireworks");
        RegisterComponent<ProfileComponent>(61, "minecraft:profile");
        RegisterComponent<NoteBlockSoundComponent>(62, "minecraft:note_block_sound");
        RegisterComponent<BannerPatternsComponent>(63, "minecraft:banner_patterns");
        RegisterComponent<BaseColorComponent>(64, "minecraft:base_color");
        RegisterComponent<PotDecorationsComponent>(65, "minecraft:pot_decorations");
        RegisterComponent<ContainerComponent>(66, "minecraft:container");
        RegisterComponent<BlockStateComponent>(67, "minecraft:block_state");
        if (usesTypedBeesFormat)
            RegisterComponent<BeesComponent1219>(68, "minecraft:bees");
        else
            RegisterComponent<BeesComponent>(68, "minecraft:bees");
        RegisterComponent<LockComponent>(69, "minecraft:lock");
        RegisterComponent<ContainerLootComponent>(70, "minecraft:container_loot");

        // Entity variant components (NEW in 1.21.5)
        RegisterComponent<SoundEventHolderComponent>(71, "minecraft:break_sound");
        RegisterComponent<VarIntComponent>(72, "minecraft:villager/variant");
        RegisterComponent<VarIntComponent>(73, "minecraft:wolf/variant");
        RegisterComponent<VarIntComponent>(74, "minecraft:wolf/sound_variant");
        RegisterComponent<VarIntComponent>(75, "minecraft:wolf/collar"); // DyeColor as VarInt
        RegisterComponent<VarIntComponent>(76, "minecraft:fox/variant");
        RegisterComponent<VarIntComponent>(77, "minecraft:salmon/size");
        RegisterComponent<VarIntComponent>(78, "minecraft:parrot/variant");
        RegisterComponent<VarIntComponent>(79, "minecraft:tropical_fish/pattern");
        RegisterComponent<VarIntComponent>(80, "minecraft:tropical_fish/base_color"); // DyeColor
        RegisterComponent<VarIntComponent>(81, "minecraft:tropical_fish/pattern_color"); // DyeColor
        RegisterComponent<VarIntComponent>(82, "minecraft:mooshroom/variant");
        RegisterComponent<VarIntComponent>(83, "minecraft:rabbit/variant");
        RegisterComponent<VarIntComponent>(84, "minecraft:pig/variant");
        RegisterComponent<VarIntComponent>(85, "minecraft:cow/variant");
        RegisterComponent<EitherHolderComponent>(86, "minecraft:chicken/variant"); // EitherHolder<ChickenVariant>
        RegisterComponent<VarIntComponent>(87, "minecraft:frog/variant");
        RegisterComponent<VarIntComponent>(88, "minecraft:horse/variant");
        RegisterComponent<PaintingVariantHolderComponent>(89, "minecraft:painting/variant"); // Holder<PaintingVariant>
        RegisterComponent<VarIntComponent>(90, "minecraft:llama/variant");
        RegisterComponent<VarIntComponent>(91, "minecraft:axolotl/variant");
        RegisterComponent<VarIntComponent>(92, "minecraft:cat/variant");
        RegisterComponent<VarIntComponent>(93, "minecraft:cat/collar"); // DyeColor
        RegisterComponent<VarIntComponent>(94, "minecraft:sheep/color"); // DyeColor
        RegisterComponent<VarIntComponent>(95, "minecraft:shulker/color"); // DyeColor
    }
}
