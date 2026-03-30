using System;
using System.Collections.Generic;

namespace MinecraftClient
{
    internal static class LegacyAchievementCatalog
    {
        public static IReadOnlyList<string> Ids { get; } =
        [
            "achievement.openInventory",
            "achievement.mineWood",
            "achievement.buildWorkBench",
            "achievement.buildPickaxe",
            "achievement.buildFurnace",
            "achievement.acquireIron",
            "achievement.buildHoe",
            "achievement.makeBread",
            "achievement.bakeCake",
            "achievement.buildBetterPickaxe",
            "achievement.cookFish",
            "achievement.onARail",
            "achievement.buildSword",
            "achievement.killEnemy",
            "achievement.killCow",
            "achievement.flyPig",
            "achievement.snipeSkeleton",
            "achievement.diamonds",
            "achievement.diamondsToYou",
            "achievement.portal",
            "achievement.ghast",
            "achievement.blazeRod",
            "achievement.potion",
            "achievement.theEnd",
            "achievement.theEnd2",
            "achievement.enchantments",
            "achievement.overkill",
            "achievement.bookcase",
            "achievement.breedCow",
            "achievement.spawnWither",
            "achievement.killWither",
            "achievement.fullBeacon",
            "achievement.exploreAllBiomes",
            "achievement.overpowered"
        ];

        private static readonly HashSet<string> s_idSet = new(Ids, StringComparer.Ordinal);

        public static bool Contains(string id)
        {
            return s_idSet.Contains(id);
        }
    }
}
