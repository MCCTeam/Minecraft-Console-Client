using System;
using System.Collections.Generic;
using System.Linq;
using MinecraftClient.Inventory;
using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

namespace MinecraftClient.Mapping
{
    /// <summary>
    /// Computes dig duration in ticks for survival-style block breaking.
    /// Version-aware across 1.8-1.21.11+, using tool speed, enchantments, effects, and attributes.
    /// </summary>
    public static class MiningCalculator
    {
        /// <summary>
        /// Compute the number of ticks required to break a block in survival mode.
        /// Returns 0 for instant-break blocks, -1 for unbreakable blocks.
        /// </summary>
        /// <param name="blockMaterial">The block material to break</param>
        /// <param name="heldItem">The item in the player's main hand (null for empty hand)</param>
        /// <param name="helmetItem">The item in the player's helmet slot (null if empty, used for Aqua Affinity)</param>
        /// <param name="effects">Currently active player effects</param>
        /// <param name="playerAttributes">Cached player attribute values (from OnEntityProperties)</param>
        /// <param name="isUnderwater">Whether the player's eyes are submerged in water</param>
        /// <param name="isOnGround">Whether the player is on the ground</param>
        /// <param name="protocolVersion">The Minecraft protocol version</param>
        /// <returns>Ticks to break the block, 0 for instant, -1 for unbreakable</returns>
        public static int ComputeDigTicks(
            Material blockMaterial,
            Item? heldItem,
            Item? helmetItem,
            Dictionary<Effects, EffectData> effects,
            Dictionary<string, double> playerAttributes,
            bool isUnderwater,
            bool isOnGround,
            int protocolVersion)
        {
            float hardness = BlockHardness.GetHardness(blockMaterial);

            if (hardness < 0)
                return -1; // Unbreakable

            if (hardness == 0)
                return 0; // Instant break

            float destroySpeed = GetDestroySpeed(
                blockMaterial, heldItem, helmetItem, effects, playerAttributes,
                isUnderwater, isOnGround, protocolVersion);

            bool correctTool = HasCorrectToolForDrops(blockMaterial, heldItem, protocolVersion);
            int divisor = correctTool ? 30 : 100;

            float destroyProgress = destroySpeed / hardness / divisor;

            if (destroyProgress >= 1.0f)
                return 0; // Instant break

            return (int)MathF.Ceiling(1.0f / destroyProgress);
        }

        /// <summary>
        /// Compute the player's destroy speed for a given block, following vanilla formulas.
        /// </summary>
        private static float GetDestroySpeed(
            Material blockMaterial,
            Item? heldItem,
            Item? helmetItem,
            Dictionary<Effects, EffectData> effects,
            Dictionary<string, double> playerAttributes,
            bool isUnderwater,
            bool isOnGround,
            int protocolVersion)
        {
            float speed = GetToolSpeed(blockMaterial, heldItem, protocolVersion);

            if (protocolVersion >= Protocol18Handler.MC_1_21_11_Version)
            {
                // 1.21.11+: Efficiency is delivered via the MINING_EFFICIENCY attribute
                if (speed > 1.0f && playerAttributes.TryGetValue("player.mining_efficiency", out double miningEff))
                    speed += (float)miningEff;
            }
            else
            {
                // Pre-1.21.11: Efficiency enchantment adds level^2 + 1
                int effLevel = GetEnchantmentLevel(heldItem, Enchantments.Efficiency, protocolVersion);
                if (speed > 1.0f && effLevel > 0)
                    speed += effLevel * effLevel + 1;
            }

            // Haste effect: multiply by 1 + 0.2 * (amplifier + 1)
            if (effects.TryGetValue(Effects.Haste, out var hasteData))
                speed *= 1.0f + (hasteData.Amplifier + 1) * 0.2f;

            // Conduit Power also grants dig speed equivalent when in water
            if (effects.TryGetValue(Effects.ConduitPower, out var conduitData))
                speed *= 1.0f + (conduitData.Amplifier + 1) * 0.2f;

            // Mining Fatigue
            if (effects.TryGetValue(Effects.MiningFatigue, out var fatigueData))
            {
                float multiplier = fatigueData.Amplifier switch
                {
                    0 => 0.3f,
                    1 => 0.09f,
                    2 => 0.0027f,
                    _ => 8.1E-4f
                };
                speed *= multiplier;
            }

            // Attribute multipliers for modern versions
            if (protocolVersion >= Protocol18Handler.MC_1_20_6_Version)
            {
                // BLOCK_BREAK_SPEED attribute (default 1.0)
                if (playerAttributes.TryGetValue("player.block_break_speed", out double bbs))
                    speed *= (float)bbs;
            }

            // Underwater penalty
            if (isUnderwater)
            {
                if (protocolVersion >= Protocol18Handler.MC_1_21_11_Version)
                {
                    // 1.21.11+: Uses SUBMERGED_MINING_SPEED attribute (default 0.2)
                    double submergedSpeed = 0.2;
                    if (playerAttributes.TryGetValue("player.submerged_mining_speed", out double sms))
                        submergedSpeed = sms;
                    speed *= (float)submergedSpeed;
                }
                else
                {
                    // Pre-1.21.11: /5 unless Aqua Affinity
                    bool hasAquaAffinity = GetEnchantmentLevel(helmetItem, Enchantments.AquaAffinity, protocolVersion) > 0;
                    if (!hasAquaAffinity)
                        speed /= 5.0f;
                }
            }

            // Airborne penalty
            if (!isOnGround)
                speed /= 5.0f;

            return speed;
        }

        /// <summary>
        /// Get the base tool mining speed for a block.
        /// For 1.20.6+ with ToolComponent, uses structured component data.
        /// For older versions, uses hardcoded tool speed tables.
        /// </summary>
        private static float GetToolSpeed(Material blockMaterial, Item? heldItem, int protocolVersion)
        {
            if (heldItem is null)
                return 1.0f;

            // Modern path: use ToolComponent from structured components
            if (protocolVersion >= Protocol18Handler.MC_1_20_6_Version)
            {
                var toolComp = heldItem.Components?.OfType<ToolComponent>().FirstOrDefault();
                if (toolComp is not null)
                {
                    // Check rules for matching blocks
                    foreach (var rule in toolComp.Rules)
                    {
                        if (rule.HasSpeed && MatchesBlockSet(rule.Blocks, blockMaterial))
                            return rule.Speed;
                    }
                    return toolComp.DefaultMiningSpeed;
                }
            }

            // Legacy path: hardcoded tool speed tables
            return GetLegacyToolSpeed(heldItem.Type, blockMaterial);
        }

        /// <summary>
        /// Check whether the tool provides correct drops for a block.
        /// </summary>
        private static bool HasCorrectToolForDrops(Material blockMaterial, Item? heldItem, int protocolVersion)
        {
            if (!BlockHardness.RequiresCorrectTool(blockMaterial))
                return true;

            if (heldItem is null)
                return false;

            // Modern path: check ToolComponent rules
            if (protocolVersion >= Protocol18Handler.MC_1_20_6_Version)
            {
                var toolComp = heldItem.Components?.OfType<ToolComponent>().FirstOrDefault();
                if (toolComp is not null)
                {
                    foreach (var rule in toolComp.Rules)
                    {
                        if (rule.HasCorrectDropForBlocks && rule.CorrectDropForBlocks
                            && MatchesBlockSet(rule.Blocks, blockMaterial))
                            return true;
                    }
                }
                return false;
            }

            // Legacy path: check if Material2Tool recommends this tool type
            return IsCorrectToolLegacy(heldItem.Type, blockMaterial);
        }

        /// <summary>
        /// Match a block material against a ToolComponent BlockSetSubcomponent.
        /// </summary>
        private static bool MatchesBlockSet(
            Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6.BlockSetSubcomponent blockSet,
            Material blockMaterial)
        {
            if (blockSet.BlockIds is not null)
            {
                // Check against explicit block state IDs
                foreach (int blockId in blockSet.BlockIds)
                {
                    if (Block.Palette.FromId(blockId) == blockMaterial)
                        return true;
                }
            }

            if (blockSet.TagName is not null)
            {
                // Match against tag name (e.g., "minecraft:mineable/pickaxe")
                return MatchesBlockTag(blockSet.TagName, blockMaterial);
            }

            return false;
        }

        /// <summary>
        /// Approximate block tag matching using Material2Tool categories.
        /// Tags like "minecraft:mineable/pickaxe" map to the appropriate tool categories.
        /// </summary>
        private static bool MatchesBlockTag(string tagName, Material blockMaterial)
        {
            // Normalize tag name
            string tag = tagName.Replace("minecraft:", "");

            ItemType[] tools = Material2Tool.GetCorrectToolForBlock(blockMaterial);
            if (tools.Length == 0)
                return false;

            ItemType firstTool = tools[0];
            return tag switch
            {
                "mineable/pickaxe" => IsPickaxe(firstTool),
                "mineable/axe" => IsAxe(firstTool),
                "mineable/shovel" => IsShovel(firstTool),
                "mineable/hoe" => IsHoe(firstTool),
                _ => false
            };
        }

        /// <summary>
        /// Get the enchantment level from an item, supporting both legacy NBT and modern structured components.
        /// </summary>
        public static int GetEnchantmentLevel(Item? item, Enchantments enchantment, int protocolVersion)
        {
            if (item is null)
                return 0;

            // Modern path: structured components (1.20.6+)
            var enchList = item.EnchantmentList;
            if (enchList is not null)
            {
                var ench = enchList.FirstOrDefault(e => e.Type == enchantment);
                if (ench is not null)
                    return ench.Level;
            }

            // Legacy path: NBT data
            if (item.NBT is not null &&
                item.NBT.TryGetValue("Enchantments", out object? enchantments))
            {
                try
                {
                    string enchNameLower = GetEnchantmentResourceName(enchantment);
                    foreach (Dictionary<string, object> enchEntry in (object[])enchantments)
                    {
                        string id = ((string)enchEntry["id"]).ToLowerInvariant();
                        if (id == enchNameLower || id == "minecraft:" + enchNameLower)
                            return (short)enchEntry["lvl"];
                    }
                }
                catch
                {
                    // NBT parsing failure - return 0
                }
            }

            return 0;
        }

        /// <summary>
        /// Map Enchantments enum to Minecraft resource name (e.g., "efficiency").
        /// </summary>
        private static string GetEnchantmentResourceName(Enchantments enchantment)
        {
            return enchantment switch
            {
                Enchantments.AquaAffinity => "aqua_affinity",
                Enchantments.BaneOfArthropods => "bane_of_arthropods",
                Enchantments.BlastProtection => "blast_protection",
                Enchantments.Efficiency => "efficiency",
                Enchantments.FeatherFalling => "feather_falling",
                Enchantments.FireAspect => "fire_aspect",
                Enchantments.FireProtection => "fire_protection",
                Enchantments.FrostWalker => "frost_walker",
                Enchantments.LuckOfTheSea => "luck_of_the_sea",
                Enchantments.ProjectileProtection => "projectile_protection",
                Enchantments.QuickCharge => "quick_charge",
                Enchantments.SilkTouch => "silk_touch",
                Enchantments.SoulSpeed => "soul_speed",
                Enchantments.SwiftSneak => "swift_sneak",
                Enchantments.VanishingCurse => "vanishing_curse",
                Enchantments.BindingCurse => "binding_curse",
                Enchantments.WindBurst => "wind_burst",
                _ => enchantment.ToString().ToUnderscoreCase()
            };
        }

        #region Legacy Tool Speed Tables

        /// <summary>
        /// Legacy tool speed for pre-1.20.6 versions using hardcoded values.
        /// </summary>
        private static float GetLegacyToolSpeed(ItemType toolType, Material blockMaterial)
        {
            ItemType[] recommended = Material2Tool.GetCorrectToolForBlock(blockMaterial);
            if (recommended.Length == 0)
                return 1.0f;

            // Check if the held tool matches the recommended tool category
            ToolCategory heldCategory = GetToolCategory(toolType);
            ToolCategory neededCategory = GetToolCategory(recommended[0]);

            if (heldCategory == ToolCategory.None || heldCategory != neededCategory)
            {
                // Special cases: sword on cobweb, shears on specific blocks
                if (toolType is ItemType.Shears && IsShearable(blockMaterial))
                    return 1.5f;
                if (IsSword(toolType) && blockMaterial == Material.Cobweb)
                    return 15.0f;
                return 1.0f;
            }

            return GetBaseToolSpeed(toolType);
        }

        private static float GetBaseToolSpeed(ItemType toolType)
        {
            return toolType switch
            {
                // Wooden tools
                ItemType.WoodenPickaxe or ItemType.WoodenAxe or ItemType.WoodenShovel or
                ItemType.WoodenSword or ItemType.WoodenHoe => 2.0f,

                // Stone tools
                ItemType.StonePickaxe or ItemType.StoneAxe or ItemType.StoneShovel or
                ItemType.StoneSword or ItemType.StoneHoe => 4.0f,

                // Iron tools
                ItemType.IronPickaxe or ItemType.IronAxe or ItemType.IronShovel or
                ItemType.IronSword or ItemType.IronHoe => 6.0f,

                // Diamond tools
                ItemType.DiamondPickaxe or ItemType.DiamondAxe or ItemType.DiamondShovel or
                ItemType.DiamondSword or ItemType.DiamondHoe => 8.0f,

                // Netherite tools
                ItemType.NetheritePickaxe or ItemType.NetheriteAxe or ItemType.NetheriteShovel or
                ItemType.NetheriteSword or ItemType.NetheriteHoe => 9.0f,

                // Golden tools
                ItemType.GoldenPickaxe or ItemType.GoldenAxe or ItemType.GoldenShovel or
                ItemType.GoldenSword or ItemType.GoldenHoe => 12.0f,

                // Shears
                ItemType.Shears => 2.0f,

                _ => 1.0f
            };
        }

        /// <summary>
        /// Check if the held tool is the correct tool for drops in legacy versions.
        /// Uses Material2Tool's recommendations to determine correctness.
        /// </summary>
        private static bool IsCorrectToolLegacy(ItemType toolType, Material blockMaterial)
        {
            ItemType[] recommended = Material2Tool.GetCorrectToolForBlock(blockMaterial);
            if (recommended.Length == 0)
                return false;

            ToolCategory heldCategory = GetToolCategory(toolType);
            ToolCategory neededCategory = GetToolCategory(recommended[0]);

            if (heldCategory == ToolCategory.None || heldCategory != neededCategory)
                return false;

            // Check tool tier requirement
            int heldTier = GetToolTier(toolType);
            int requiredTier = GetRequiredTier(blockMaterial, recommended);

            return heldTier >= requiredTier;
        }

        /// <summary>
        /// Get the minimum tool tier required for a block based on Material2Tool's recommendation ordering.
        /// </summary>
        private static int GetRequiredTier(Material blockMaterial, ItemType[] recommended)
        {
            if (recommended.Length == 0)
                return 0;

            // Material2Tool lists tools from highest to lowest tier.
            // The last tool in the array is the minimum required tier.
            return GetToolTier(recommended[^1]);
        }

        private enum ToolCategory
        {
            None,
            Pickaxe,
            Axe,
            Shovel,
            Hoe,
            Sword,
            Shears
        }

        private static ToolCategory GetToolCategory(ItemType item)
        {
            if (IsPickaxe(item)) return ToolCategory.Pickaxe;
            if (IsAxe(item)) return ToolCategory.Axe;
            if (IsShovel(item)) return ToolCategory.Shovel;
            if (IsHoe(item)) return ToolCategory.Hoe;
            if (IsSword(item)) return ToolCategory.Sword;
            if (item == ItemType.Shears) return ToolCategory.Shears;
            return ToolCategory.None;
        }

        private static int GetToolTier(ItemType item)
        {
            string name = item.ToString();
            if (name.StartsWith("Wooden")) return 0;
            if (name.StartsWith("Golden")) return 0;
            if (name.StartsWith("Stone")) return 1;
            if (name.StartsWith("Iron")) return 2;
            if (name.StartsWith("Diamond")) return 3;
            if (name.StartsWith("Netherite")) return 4;
            return 0;
        }

        private static bool IsPickaxe(ItemType item) =>
            item is ItemType.WoodenPickaxe or ItemType.StonePickaxe or ItemType.IronPickaxe
                 or ItemType.GoldenPickaxe or ItemType.DiamondPickaxe or ItemType.NetheritePickaxe;

        private static bool IsAxe(ItemType item) =>
            item is ItemType.WoodenAxe or ItemType.StoneAxe or ItemType.IronAxe
                 or ItemType.GoldenAxe or ItemType.DiamondAxe or ItemType.NetheriteAxe;

        private static bool IsShovel(ItemType item) =>
            item is ItemType.WoodenShovel or ItemType.StoneShovel or ItemType.IronShovel
                 or ItemType.GoldenShovel or ItemType.DiamondShovel or ItemType.NetheriteShovel;

        private static bool IsHoe(ItemType item) =>
            item is ItemType.WoodenHoe or ItemType.StoneHoe or ItemType.IronHoe
                 or ItemType.GoldenHoe or ItemType.DiamondHoe or ItemType.NetheriteHoe;

        private static bool IsSword(ItemType item) =>
            item is ItemType.WoodenSword or ItemType.StoneSword or ItemType.IronSword
                 or ItemType.GoldenSword or ItemType.DiamondSword or ItemType.NetheriteSword;

        private static bool IsShearable(Material block) =>
            block is Material.Cobweb or Material.OakLeaves or Material.SpruceLeaves
                  or Material.BirchLeaves or Material.JungleLeaves or Material.AcaciaLeaves
                  or Material.DarkOakLeaves or Material.CherryLeaves or Material.MangroveLeaves
                  or Material.AzaleaLeaves or Material.FloweringAzaleaLeaves
                  or Material.WhiteWool or Material.OrangeWool or Material.MagentaWool
                  or Material.LightBlueWool or Material.YellowWool or Material.LimeWool
                  or Material.PinkWool or Material.GrayWool or Material.LightGrayWool
                  or Material.CyanWool or Material.PurpleWool or Material.BlueWool
                  or Material.BrownWool or Material.GreenWool or Material.RedWool
                  or Material.BlackWool or Material.Vine;

        #endregion
    }
}
