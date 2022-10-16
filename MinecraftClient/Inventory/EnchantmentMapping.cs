using System;
using System.Collections.Generic;
using System.Linq;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Inventory
{
    public class EnchantmentMapping
    {
        #pragma warning disable format // @formatter:off
        // 1.14 - 1.15.2
        private static Dictionary<short, Enchantment> enchantmentMappings114 = new Dictionary<short, Enchantment>()
        {
            //id    type 
            { 0,    Enchantment.Protection },
            { 1,    Enchantment.FireProtection },
            { 2,    Enchantment.FeatherFalling },
            { 3,    Enchantment.BlastProtection },
            { 4,    Enchantment.ProjectileProtection },
            { 5,    Enchantment.Respiration },
            { 6,    Enchantment.AquaAffinity },
            { 7,    Enchantment.Thorns },
            { 8,    Enchantment.DepthStrieder },
            { 9,    Enchantment.FrostWalker },
            { 10,   Enchantment.BindingCurse },
            { 11,   Enchantment.Sharpness },
            { 12,   Enchantment.Smite },
            { 13,   Enchantment.BaneOfArthropods },
            { 14,   Enchantment.Knockback },
            { 15,   Enchantment.FireAspect },
            { 16,   Enchantment.Looting },
            { 17,   Enchantment.Sweeping },
            { 18,   Enchantment.Efficency },
            { 19,   Enchantment.SilkTouch },
            { 20,   Enchantment.Unbreaking },
            { 21,   Enchantment.Fortune },
            { 22,   Enchantment.Power },
            { 23,   Enchantment.Punch },
            { 24,   Enchantment.Flame },
            { 25,   Enchantment.Infinity },
            { 26,   Enchantment.LuckOfTheSea },
            { 27,   Enchantment.Lure },
            { 28,   Enchantment.Loyality },
            { 29,   Enchantment.Impaling },
            { 30,   Enchantment.Riptide },
            { 31,   Enchantment.Channeling },
            { 32,   Enchantment.Mending },
            { 33,   Enchantment.VanishingCurse }
        };

        // 1.16 - 1.18
        private static Dictionary<short, Enchantment> enchantmentMappings116 = new Dictionary<short, Enchantment>()
        {
            //id    type 
            { 0,    Enchantment.Protection },
            { 1,    Enchantment.FireProtection },
            { 2,    Enchantment.FeatherFalling },
            { 3,    Enchantment.BlastProtection },
            { 4,    Enchantment.ProjectileProtection },
            { 5,    Enchantment.Respiration },
            { 6,    Enchantment.AquaAffinity },
            { 7,    Enchantment.Thorns },
            { 8,    Enchantment.DepthStrieder },
            { 9,    Enchantment.FrostWalker },
            { 10,   Enchantment.BindingCurse },
            { 11,   Enchantment.SoulSpeed },
            { 12,   Enchantment.Sharpness },
            { 13,   Enchantment.Smite },
            { 14,   Enchantment.BaneOfArthropods },
            { 15,   Enchantment.Knockback },
            { 16,   Enchantment.FireAspect },
            { 17,   Enchantment.Looting },
            { 18,   Enchantment.Sweeping },
            { 19,   Enchantment.Efficency },
            { 20,   Enchantment.SilkTouch },
            { 21,   Enchantment.Unbreaking },
            { 22,   Enchantment.Fortune },
            { 23,   Enchantment.Power },
            { 24,   Enchantment.Punch },
            { 25,   Enchantment.Flame },
            { 26,   Enchantment.Infinity },
            { 27,   Enchantment.LuckOfTheSea },
            { 28,   Enchantment.Lure },
            { 29,   Enchantment.Loyality },
            { 30,   Enchantment.Impaling },
            { 31,   Enchantment.Riptide },
            { 32,   Enchantment.Channeling },
            { 33,   Enchantment.Multishot },
            { 34,   Enchantment.QuickCharge },
            { 35,   Enchantment.Piercing },
            { 36,   Enchantment.Mending },
            { 37,   Enchantment.VanishingCurse }
        };

        // 1.19+
        private static Dictionary<short, Enchantment> enchantmentMappings = new Dictionary<short, Enchantment>()
        {
            //id    type 
            { 0,    Enchantment.Protection },
            { 1,    Enchantment.FireProtection },
            { 2,    Enchantment.FeatherFalling },
            { 3,    Enchantment.BlastProtection },
            { 4,    Enchantment.ProjectileProtection },
            { 5,    Enchantment.Respiration },
            { 6,    Enchantment.AquaAffinity },
            { 7,    Enchantment.Thorns },
            { 8,    Enchantment.DepthStrieder },
            { 9,    Enchantment.FrostWalker },
            { 10,   Enchantment.BindingCurse },
            { 11,   Enchantment.SoulSpeed },
            { 12,   Enchantment.SwiftSneak },
            { 13,   Enchantment.Sharpness },
            { 14,   Enchantment.Smite },
            { 15,   Enchantment.BaneOfArthropods },
            { 16,   Enchantment.Knockback },
            { 17,   Enchantment.FireAspect },
            { 18,   Enchantment.Looting },
            { 19,   Enchantment.Sweeping },
            { 20,   Enchantment.Efficency },
            { 21,   Enchantment.SilkTouch },
            { 22,   Enchantment.Unbreaking },
            { 23,   Enchantment.Fortune },
            { 24,   Enchantment.Power },
            { 25,   Enchantment.Punch },
            { 26,   Enchantment.Flame },
            { 27,   Enchantment.Infinity },
            { 28,   Enchantment.LuckOfTheSea },
            { 29,   Enchantment.Lure },
            { 30,   Enchantment.Loyality },
            { 31,   Enchantment.Impaling },
            { 32,   Enchantment.Riptide },
            { 33,   Enchantment.Channeling },
            { 34,   Enchantment.Multishot },
            { 35,   Enchantment.QuickCharge },
            { 36,   Enchantment.Piercing },
            { 37,   Enchantment.Mending },
            { 38,   Enchantment.VanishingCurse }
        };

        private static Dictionary<Enchantment, string> enchantmentNames = new Dictionary<Enchantment, string>()
        {
            //type          
            { Enchantment.Protection,           "Enchantment.Protection"           },
            { Enchantment.FireProtection,       "Enchantment.FireProtection"       },
            { Enchantment.FeatherFalling,       "Enchantment.FeatherFalling"       },
            { Enchantment.BlastProtection,      "Enchantment.BlastProtection"      },
            { Enchantment.ProjectileProtection, "Enchantment.ProjectileProtection" },
            { Enchantment.Respiration,          "Enchantment.Respiration"          },
            { Enchantment.AquaAffinity,         "Enchantment.AquaAffinity"         },
            { Enchantment.Thorns,               "Enchantment.Thorns"               },
            { Enchantment.DepthStrieder,        "Enchantment.DepthStrieder"        },
            { Enchantment.FrostWalker,          "Enchantment.FrostWalker"          },
            { Enchantment.BindingCurse,         "Enchantment.BindingCurse"         },
            { Enchantment.SoulSpeed,            "Enchantment.SoulSpeed"            },
            { Enchantment.SwiftSneak,           "Enchantment.SwiftSneak"           },
            { Enchantment.Sharpness,            "Enchantment.Sharpness"            },
            { Enchantment.Smite,                "Enchantment.Smite"                },
            { Enchantment.BaneOfArthropods,     "Enchantment.BaneOfArthropods"     },
            { Enchantment.Knockback,            "Enchantment.Knockback"            },
            { Enchantment.FireAspect,           "Enchantment.FireAspect"           },
            { Enchantment.Looting,              "Enchantment.Looting"              },
            { Enchantment.Sweeping,             "Enchantment.Sweeping"             },
            { Enchantment.Efficency,            "Enchantment.Efficency"            },
            { Enchantment.SilkTouch,            "Enchantment.SilkTouch"            },
            { Enchantment.Unbreaking,           "Enchantment.Unbreaking"           },
            { Enchantment.Fortune,              "Enchantment.Fortune"              },
            { Enchantment.Power,                "Enchantment.Power"                },
            { Enchantment.Punch,                "Enchantment.Punch"                },
            { Enchantment.Flame,                "Enchantment.Flame"                },
            { Enchantment.Infinity,             "Enchantment.Infinity"             },
            { Enchantment.LuckOfTheSea,         "Enchantment.LuckOfTheSea"         },
            { Enchantment.Lure,                 "Enchantment.Lure"                 },
            { Enchantment.Loyality,             "Enchantment.Loyality"             },
            { Enchantment.Impaling,             "Enchantment.Impaling"             },
            { Enchantment.Riptide,              "Enchantment.Riptide"              },
            { Enchantment.Channeling,           "Enchantment.Channeling"           },
            { Enchantment.Multishot,            "Enchantment.Multishot"            },
            { Enchantment.QuickCharge,          "Enchantment.QuickCharge"          },
            { Enchantment.Piercing,             "Enchantment.Piercing"             },
            { Enchantment.Mending,              "Enchantment.Mending"              },
            { Enchantment.VanishingCurse,       "Enchantment.VanishingCurse"       }
        };
#pragma warning restore format // @formatter:on

        public static Enchantment GetEnchantmentById(int protocolVersion, short id)
        {
            if (protocolVersion < Protocol18Handler.MC_1_14_Version)
                throw new Exception("Enchantments mappings are not implemented bellow 1.14");

            Dictionary<short, Enchantment> map = enchantmentMappings;

            if (protocolVersion >= Protocol18Handler.MC_1_14_Version && protocolVersion < Protocol18Handler.MC_1_16_Version)
                map = enchantmentMappings114;
            else if (protocolVersion >= Protocol18Handler.MC_1_16_Version && protocolVersion < Protocol18Handler.MC_1_19_Version)
                map = enchantmentMappings116;

            if (!map.ContainsKey(id))
                throw new Exception("Got an Unknown Enchantment ID '" + id + "', please update the Mappings!");

            return map[id];
        }

        public static string GetEnchantmentName(Enchantment enchantment)
        {
            if (!enchantmentNames.ContainsKey(enchantment))
                return "Unknown Enchantment with ID: " + ((short)enchantment) + " (Probably not named in the code yet)";

            return Translations.TryGet(enchantmentNames[enchantment]);
        }

        public static string ConvertLevelToRomanNumbers(int num)
        {
            string result = string.Empty;
            Dictionary<string, int> romanNumbers = new Dictionary<string, int>
            {
                {"M", 1000 },
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
    }
}
