using System;
using System.Collections.Generic;
using System.Linq;
using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.Inventory
{
    public class EnchantmentMapping
    {
        #pragma warning disable format // @formatter:off
        // 1.14 - 1.15.2
        private static Dictionary<short, Enchantments> enchantmentMappings114 = new()
        {
            //id    type 
            { 0,    Enchantments.Protection },
            { 1,    Enchantments.FireProtection },
            { 2,    Enchantments.FeatherFalling },
            { 3,    Enchantments.BlastProtection },
            { 4,    Enchantments.ProjectileProtection },
            { 5,    Enchantments.Respiration },
            { 6,    Enchantments.AquaAffinity },
            { 7,    Enchantments.Thorns },
            { 8,    Enchantments.DepthStrider },
            { 9,    Enchantments.FrostWalker },
            { 10,   Enchantments.BindingCurse },
            { 11,   Enchantments.Sharpness },
            { 12,   Enchantments.Smite },
            { 13,   Enchantments.BaneOfArthropods },
            { 14,   Enchantments.Knockback },
            { 15,   Enchantments.FireAspect },
            { 16,   Enchantments.Looting },
            { 17,   Enchantments.Sweeping },
            { 18,   Enchantments.Efficiency },
            { 19,   Enchantments.SilkTouch },
            { 20,   Enchantments.Unbreaking },
            { 21,   Enchantments.Fortune },
            { 22,   Enchantments.Power },
            { 23,   Enchantments.Punch },
            { 24,   Enchantments.Flame },
            { 25,   Enchantments.Infinity },
            { 26,   Enchantments.LuckOfTheSea },
            { 27,   Enchantments.Lure },
            { 28,   Enchantments.Loyalty },
            { 29,   Enchantments.Impaling },
            { 30,   Enchantments.Riptide },
            { 31,   Enchantments.Channeling },
            { 32,   Enchantments.Mending },
            { 33,   Enchantments.VanishingCurse }
        };

        // 1.16 - 1.18
        private static Dictionary<short, Enchantments> enchantmentMappings116 = new()
        {
            //id    type 
            { 0,    Enchantments.Protection },
            { 1,    Enchantments.FireProtection },
            { 2,    Enchantments.FeatherFalling },
            { 3,    Enchantments.BlastProtection },
            { 4,    Enchantments.ProjectileProtection },
            { 5,    Enchantments.Respiration },
            { 6,    Enchantments.AquaAffinity },
            { 7,    Enchantments.Thorns },
            { 8,    Enchantments.DepthStrider },
            { 9,    Enchantments.FrostWalker },
            { 10,   Enchantments.BindingCurse },
            { 11,   Enchantments.SoulSpeed },
            { 12,   Enchantments.Sharpness },
            { 13,   Enchantments.Smite },
            { 14,   Enchantments.BaneOfArthropods },
            { 15,   Enchantments.Knockback },
            { 16,   Enchantments.FireAspect },
            { 17,   Enchantments.Looting },
            { 18,   Enchantments.Sweeping },
            { 19,   Enchantments.Efficiency },
            { 20,   Enchantments.SilkTouch },
            { 21,   Enchantments.Unbreaking },
            { 22,   Enchantments.Fortune },
            { 23,   Enchantments.Power },
            { 24,   Enchantments.Punch },
            { 25,   Enchantments.Flame },
            { 26,   Enchantments.Infinity },
            { 27,   Enchantments.LuckOfTheSea },
            { 28,   Enchantments.Lure },
            { 29,   Enchantments.Loyalty },
            { 30,   Enchantments.Impaling },
            { 31,   Enchantments.Riptide },
            { 32,   Enchantments.Channeling },
            { 33,   Enchantments.Multishot },
            { 34,   Enchantments.QuickCharge },
            { 35,   Enchantments.Piercing },
            { 36,   Enchantments.Mending },
            { 37,   Enchantments.VanishingCurse }
        };

        // 1.19 - 1.20.4
        private static Dictionary<short, Enchantments> enchantmentMappings119 = new()
        {
            //id    type 
            { 0,    Enchantments.Protection },
            { 1,    Enchantments.FireProtection },
            { 2,    Enchantments.FeatherFalling },
            { 3,    Enchantments.BlastProtection },
            { 4,    Enchantments.ProjectileProtection },
            { 5,    Enchantments.Respiration },
            { 6,    Enchantments.AquaAffinity },
            { 7,    Enchantments.Thorns },
            { 8,    Enchantments.DepthStrider },
            { 9,    Enchantments.FrostWalker },
            { 10,   Enchantments.BindingCurse },
            { 11,   Enchantments.SoulSpeed },
            { 12,   Enchantments.SwiftSneak },
            { 13,   Enchantments.Sharpness },
            { 14,   Enchantments.Smite },
            { 15,   Enchantments.BaneOfArthropods },
            { 16,   Enchantments.Knockback },
            { 17,   Enchantments.FireAspect },
            { 18,   Enchantments.Looting },
            { 19,   Enchantments.Sweeping },
            { 20,   Enchantments.Efficiency },
            { 21,   Enchantments.SilkTouch },
            { 22,   Enchantments.Unbreaking },
            { 23,   Enchantments.Fortune },
            { 24,   Enchantments.Power },
            { 25,   Enchantments.Punch },
            { 26,   Enchantments.Flame },
            { 27,   Enchantments.Infinity },
            { 28,   Enchantments.LuckOfTheSea },
            { 29,   Enchantments.Lure },
            { 30,   Enchantments.Loyalty },
            { 31,   Enchantments.Impaling },
            { 32,   Enchantments.Riptide },
            { 33,   Enchantments.Channeling },
            { 34,   Enchantments.Multishot },
            { 35,   Enchantments.QuickCharge },
            { 36,   Enchantments.Piercing },
            { 37,   Enchantments.Mending },
            { 38,   Enchantments.VanishingCurse }
        };
        
        // 1.20.6 - 1.21.10
        private static Dictionary<short, Enchantments> enchantmentMappings1206 = new()
        {
            //id    type 
            { 0,    Enchantments.Protection },
            { 1,    Enchantments.FireProtection },
            { 2,    Enchantments.FeatherFalling },
            { 3,    Enchantments.BlastProtection },
            { 4,    Enchantments.ProjectileProtection },
            { 5,    Enchantments.Respiration },
            { 6,    Enchantments.AquaAffinity },
            { 7,    Enchantments.Thorns },
            { 8,    Enchantments.DepthStrider },
            { 9,    Enchantments.FrostWalker },
            { 10,   Enchantments.BindingCurse },
            { 11,   Enchantments.SoulSpeed },
            { 12,   Enchantments.SwiftSneak },
            { 13,   Enchantments.Sharpness },
            { 14,   Enchantments.Smite },
            { 15,   Enchantments.BaneOfArthropods },
            { 16,   Enchantments.Knockback },
            { 17,   Enchantments.FireAspect },
            { 18,   Enchantments.Looting },
            { 19,   Enchantments.Sweeping },
            { 20,   Enchantments.Efficiency },
            { 21,   Enchantments.SilkTouch },
            { 22,   Enchantments.Unbreaking },
            { 23,   Enchantments.Fortune },
            { 24,   Enchantments.Power },
            { 25,   Enchantments.Punch },
            { 26,   Enchantments.Flame },
            { 27,   Enchantments.Infinity },
            { 28,   Enchantments.LuckOfTheSea },
            { 29,   Enchantments.Lure },
            { 30,   Enchantments.Loyalty },
            { 31,   Enchantments.Impaling },
            { 32,   Enchantments.Riptide },
            { 33,   Enchantments.Channeling },
            { 34,   Enchantments.Multishot },
            { 35,   Enchantments.QuickCharge },
            { 36,   Enchantments.Piercing },
            { 37,   Enchantments.Density },
            { 38,   Enchantments.Breach },
            { 39,   Enchantments.WindBurst },
            { 40,   Enchantments.Mending },
            { 41,   Enchantments.VanishingCurse }
        };

        // 1.21.11+
        private static Dictionary<short, Enchantments> enchantmentMappings12111 = new()
        {
            //id    type 
            { 0,    Enchantments.Protection },
            { 1,    Enchantments.FireProtection },
            { 2,    Enchantments.FeatherFalling },
            { 3,    Enchantments.BlastProtection },
            { 4,    Enchantments.ProjectileProtection },
            { 5,    Enchantments.Respiration },
            { 6,    Enchantments.AquaAffinity },
            { 7,    Enchantments.Thorns },
            { 8,    Enchantments.DepthStrider },
            { 9,    Enchantments.FrostWalker },
            { 10,   Enchantments.BindingCurse },
            { 11,   Enchantments.SoulSpeed },
            { 12,   Enchantments.SwiftSneak },
            { 13,   Enchantments.Sharpness },
            { 14,   Enchantments.Smite },
            { 15,   Enchantments.BaneOfArthropods },
            { 16,   Enchantments.Knockback },
            { 17,   Enchantments.FireAspect },
            { 18,   Enchantments.Looting },
            { 19,   Enchantments.Sweeping },
            { 20,   Enchantments.Efficiency },
            { 21,   Enchantments.SilkTouch },
            { 22,   Enchantments.Unbreaking },
            { 23,   Enchantments.Fortune },
            { 24,   Enchantments.Power },
            { 25,   Enchantments.Punch },
            { 26,   Enchantments.Flame },
            { 27,   Enchantments.Infinity },
            { 28,   Enchantments.LuckOfTheSea },
            { 29,   Enchantments.Lure },
            { 30,   Enchantments.Loyalty },
            { 31,   Enchantments.Impaling },
            { 32,   Enchantments.Riptide },
            { 33,   Enchantments.Channeling },
            { 34,   Enchantments.Multishot },
            { 35,   Enchantments.QuickCharge },
            { 36,   Enchantments.Piercing },
            { 37,   Enchantments.Density },
            { 38,   Enchantments.Breach },
            { 39,   Enchantments.WindBurst },
            { 40,   Enchantments.Lunge },
            { 41,   Enchantments.Mending },
            { 42,   Enchantments.VanishingCurse }
        };
#pragma warning restore format // @formatter:on

        public static Enchantments GetEnchantmentById(int protocolVersion, short id)
        {
            if (protocolVersion < Protocol18Handler.MC_1_14_Version)
                throw new Exception("Enchantments mappings are not implemented bellow 1.14");

            var map = GetMapForProtocolVersion(protocolVersion);

            if (!map.TryGetValue(id, out var value))
                throw new Exception($"Got an Unknown Enchantment ID {id}, please update the Mappings!");

            return value;
        }

        private static Dictionary<Enchantments, short>? reverseDynamicEnchantmentMappings;
        private static readonly Dictionary<Enchantments, short> reverseEnchantmentMappings114 = CreateReverseMap(enchantmentMappings114);
        private static readonly Dictionary<Enchantments, short> reverseEnchantmentMappings116 = CreateReverseMap(enchantmentMappings116);
        private static readonly Dictionary<Enchantments, short> reverseEnchantmentMappings119 = CreateReverseMap(enchantmentMappings119);
        private static readonly Dictionary<Enchantments, short> reverseEnchantmentMappings1206 = CreateReverseMap(enchantmentMappings1206);
        private static readonly Dictionary<Enchantments, short> reverseEnchantmentMappings12111 = CreateReverseMap(enchantmentMappings12111);
        private static Dictionary<int, Enchantments>? dynamicEnchantmentIdMap;

        private static readonly Dictionary<string, Enchantments> nameToEnchantment = new()
        {
            { "protection", Enchantments.Protection },
            { "fire_protection", Enchantments.FireProtection },
            { "feather_falling", Enchantments.FeatherFalling },
            { "blast_protection", Enchantments.BlastProtection },
            { "projectile_protection", Enchantments.ProjectileProtection },
            { "respiration", Enchantments.Respiration },
            { "aqua_affinity", Enchantments.AquaAffinity },
            { "thorns", Enchantments.Thorns },
            { "depth_strider", Enchantments.DepthStrider },
            { "frost_walker", Enchantments.FrostWalker },
            { "binding_curse", Enchantments.BindingCurse },
            { "soul_speed", Enchantments.SoulSpeed },
            { "swift_sneak", Enchantments.SwiftSneak },
            { "sharpness", Enchantments.Sharpness },
            { "smite", Enchantments.Smite },
            { "bane_of_arthropods", Enchantments.BaneOfArthropods },
            { "knockback", Enchantments.Knockback },
            { "fire_aspect", Enchantments.FireAspect },
            { "looting", Enchantments.Looting },
            { "sweeping_edge", Enchantments.Sweeping },
            { "efficiency", Enchantments.Efficiency },
            { "silk_touch", Enchantments.SilkTouch },
            { "unbreaking", Enchantments.Unbreaking },
            { "fortune", Enchantments.Fortune },
            { "power", Enchantments.Power },
            { "punch", Enchantments.Punch },
            { "flame", Enchantments.Flame },
            { "infinity", Enchantments.Infinity },
            { "luck_of_the_sea", Enchantments.LuckOfTheSea },
            { "lure", Enchantments.Lure },
            { "loyalty", Enchantments.Loyalty },
            { "lunge", Enchantments.Lunge },
            { "impaling", Enchantments.Impaling },
            { "riptide", Enchantments.Riptide },
            { "channeling", Enchantments.Channeling },
            { "multishot", Enchantments.Multishot },
            { "quick_charge", Enchantments.QuickCharge },
            { "piercing", Enchantments.Piercing },
            { "density", Enchantments.Density },
            { "breach", Enchantments.Breach },
            { "wind_burst", Enchantments.WindBurst },
            { "mending", Enchantments.Mending },
            { "vanishing_curse", Enchantments.VanishingCurse },
        };

        /// <summary>
        /// Set the dynamic enchantment ID map from server RegistryData.
        /// Called during configuration phase when receiving minecraft:enchantment registry.
        /// </summary>
        public static void SetDynamicEnchantmentIdMap(Dictionary<int, string> idMap)
        {
            dynamicEnchantmentIdMap = new();
            foreach (var kvp in idMap)
            {
                var name = kvp.Value.StartsWith("minecraft:") ? kvp.Value.Substring("minecraft:".Length) : kvp.Value;
                if (nameToEnchantment.TryGetValue(name, out var enchantment))
                    dynamicEnchantmentIdMap[kvp.Key] = enchantment;
            }
            reverseDynamicEnchantmentMappings = null;
        }

        public static Enchantments GetEnchantmentByRegistryId1206(int protocolVersion, int id)
        {
            if (dynamicEnchantmentIdMap is not null && dynamicEnchantmentIdMap.TryGetValue(id, out var dynValue))
                return dynValue;
            if (GetMapForProtocolVersion(protocolVersion).TryGetValue((short)id, out var value))
                return value;
            return (Enchantments)(-1);
        }

        public static int GetRegistryId1206ByEnchantment(int protocolVersion, Enchantments enchantment)
        {
            if (dynamicEnchantmentIdMap is not null)
            {
                if (reverseDynamicEnchantmentMappings is null)
                {
                    reverseDynamicEnchantmentMappings = new();
                    foreach (var kvp in dynamicEnchantmentIdMap)
                        reverseDynamicEnchantmentMappings[kvp.Value] = (short)kvp.Key;
                }

                return reverseDynamicEnchantmentMappings.TryGetValue(enchantment, out var dynamicId) ? dynamicId : -1;
            }

            var reverseMap = GetReverseMapForProtocolVersion(protocolVersion);
            return reverseMap.TryGetValue(enchantment, out var id) ? id : -1;
        }

        public static string GetEnchantmentName(Enchantments enchantment)
        {
            var translation = ChatParser.TranslateString("enchantment.minecraft." + enchantment.ToString().ToUnderscoreCase());
            return string.IsNullOrEmpty(translation) ? $"Unknown Enchantment with ID: {(short)enchantment} (Probably not named in the code yet)" : translation;
        }

        public static string ConvertLevelToRomanNumbers(int num)
        {
            var result = string.Empty;
            var romanNumbers = new Dictionary<string, int>
            {
                {"M", 1000},
                {"CM", 900},
                {"D", 500},
                {"CD", 400},
                {"C", 100},
                {"XC", 90},
                {"L", 50},
                {"XL", 40},
                {"X", 10},
                {"IX", 9},
                {"V", 5},
                {"IV", 4},
                {"I", 1}
            };

            foreach (var pair in romanNumbers)
            {
                result += string.Join(string.Empty, Enumerable.Repeat(pair.Key, num / pair.Value));
                num %= pair.Value;
            }

            return result;
        }

        private static Dictionary<short, Enchantments> GetMapForProtocolVersion(int protocolVersion)
        {
            return protocolVersion switch
            {
                >= Protocol18Handler.MC_1_14_Version and < Protocol18Handler.MC_1_16_Version => enchantmentMappings114,
                >= Protocol18Handler.MC_1_16_Version and < Protocol18Handler.MC_1_19_Version => enchantmentMappings116,
                >= Protocol18Handler.MC_1_19_Version and < Protocol18Handler.MC_1_20_6_Version => enchantmentMappings119,
                >= Protocol18Handler.MC_1_20_6_Version and < Protocol18Handler.MC_1_21_11_Version => enchantmentMappings1206,
                >= Protocol18Handler.MC_1_21_11_Version => enchantmentMappings12111,
                _ => enchantmentMappings119
            };
        }

        private static Dictionary<Enchantments, short> GetReverseMapForProtocolVersion(int protocolVersion)
        {
            return protocolVersion switch
            {
                >= Protocol18Handler.MC_1_14_Version and < Protocol18Handler.MC_1_16_Version => reverseEnchantmentMappings114,
                >= Protocol18Handler.MC_1_16_Version and < Protocol18Handler.MC_1_19_Version => reverseEnchantmentMappings116,
                >= Protocol18Handler.MC_1_19_Version and < Protocol18Handler.MC_1_20_6_Version => reverseEnchantmentMappings119,
                >= Protocol18Handler.MC_1_20_6_Version and < Protocol18Handler.MC_1_21_11_Version => reverseEnchantmentMappings1206,
                >= Protocol18Handler.MC_1_21_11_Version => reverseEnchantmentMappings12111,
                _ => reverseEnchantmentMappings119
            };
        }

        private static Dictionary<Enchantments, short> CreateReverseMap(Dictionary<short, Enchantments> map)
        {
            Dictionary<Enchantments, short> reverseMap = new();
            foreach (var kvp in map)
                reverseMap[kvp.Value] = kvp.Key;

            return reverseMap;
        }
    }
}
