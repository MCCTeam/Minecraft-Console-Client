#!/usr/bin/env python3
"""
Generate MinimapEntityCategories.json from decompiled Minecraft source.

Parses EntityType.java to extract each entity's MobCategory assignment,
then maps them to MCC minimap categories (hostile/passive/neutral/non_living).

Minecraft's MobCategory values:
  MONSTER        -> hostile (with neutral overrides for conditionally hostile mobs)
  CREATURE       -> passive (with neutral overrides for conditionally hostile mobs)
  AMBIENT        -> passive
  AXOLOTLS       -> passive
  WATER_CREATURE -> passive
  WATER_AMBIENT  -> passive
  UNDERGROUND_WATER_CREATURE -> passive
  MISC           -> non_living

Some mobs classified as MONSTER or CREATURE are actually "neutral" -- they
only attack when provoked. These are listed in NEUTRAL_OVERRIDES below and
should be updated when new conditionally-hostile mobs are added.

Usage:
    python3 tools/gen_entity_category_map.py <decompiled_root>

Example:
    python3 tools/gen_entity_category_map.py MinecraftOfficial/26.1-rc-2-decompiled
"""

import json
import re
import sys
from pathlib import Path

OUTPUT_PATH = (Path(__file__).resolve().parent.parent
               / "MinecraftClient" / "Tui" / "MinimapEntityCategories.json")
ENTITY_TYPE_CS = (Path(__file__).resolve().parent.parent
                  / "MinecraftClient" / "Mapping" / "EntityType.cs")


def mc_name_to_csharp(mc_name: str) -> str:
    name = mc_name.removeprefix("minecraft:")
    return "".join(word.capitalize() for word in name.split("_"))


# Mobs that Minecraft classifies as MONSTER or CREATURE but behave as
# "neutral" -- they only attack when provoked. This list is maintained
# manually because there is no machine-readable flag in the game data.
NEUTRAL_OVERRIDES = {
    "bee", "dolphin", "goat", "iron_golem", "llama", "panda",
    "polar_bear", "snow_golem", "trader_llama", "wolf",
    "zombified_piglin", "enderman", "spider", "cave_spider",
    "copper_golem",
}

# Entities whose MobCategory in the game code doesn't match how they
# should appear on the minimap. For example, Villager and WanderingTrader
# are MISC in MC code (for spawning reasons) but should be passive on the map.
# ZombieHorse is MONSTER but is a rideable passive mob in practice.
PASSIVE_OVERRIDES = {
    "villager", "wandering_trader", "zombie_horse",
}

# Player has its own category in MCC -- extracted from MISC to "player".
PLAYER_OVERRIDES = {"player"}

MC_TO_MCC = {
    "MONSTER": "hostile",
    "CREATURE": "passive",
    "AMBIENT": "passive",
    "AXOLOTLS": "passive",
    "WATER_CREATURE": "passive",
    "WATER_AMBIENT": "passive",
    "UNDERGROUND_WATER_CREATURE": "passive",
    "MISC": "non_living",
}


def extract_entity_categories(entity_type_java: Path) -> list[tuple[str, str, str]]:
    """Extract (entity_id, field_name, MobCategory) from EntityType.java.

    Returns list of (entity_id, FIELD_NAME, MobCategory_name).
    """
    text = entity_type_java.read_text()
    results = []

    field_pat = re.compile(
        r'public\s+static\s+final\s+EntityType<[^>]+>\s+(\w+)\s*=\s*register\s*\(')

    pos = 0
    while pos < len(text):
        m = field_pat.search(text, pos)
        if not m:
            break

        field_name = m.group(1)
        paren_start = m.end() - 1
        depth = 1
        i = paren_start + 1
        while i < len(text) and depth > 0:
            if text[i] == '(':
                depth += 1
            elif text[i] == ')':
                depth -= 1
            i += 1

        body = text[paren_start:i]

        name_match = re.search(r'"(\w+)"', body)
        entity_id = name_match.group(1) if name_match else field_name.lower()

        cat_match = re.search(r'MobCategory\.(\w+)', body)
        mob_cat = cat_match.group(1) if cat_match else "MISC"

        results.append((entity_id, field_name, mob_cat))
        pos = i

    return results


def load_known_entity_types() -> set[str]:
    known = set()
    if ENTITY_TYPE_CS.exists():
        with open(ENTITY_TYPE_CS) as f:
            for line in f:
                m = re.match(r'\s+(\w+),?\s*$', line)
                if m:
                    known.add(m.group(1))
    return known


def main():
    if len(sys.argv) != 2:
        print(__doc__)
        sys.exit(1)

    root = Path(sys.argv[1])
    entity_type_java = root / "net/minecraft/world/entity/EntityType.java"

    if not entity_type_java.exists():
        print(f"Error: {entity_type_java} not found")
        sys.exit(1)

    print("Parsing EntityType.java...")
    entities = extract_entity_categories(entity_type_java)
    print(f"  Found {len(entities)} entity type declarations")

    known_types = load_known_entity_types()

    hostile = []
    passive = []
    neutral = []
    non_living = []

    for entity_id, field_name, mob_cat in entities:
        cs_name = mc_name_to_csharp(entity_id)

        if known_types and cs_name not in known_types:
            continue

        if entity_id in PLAYER_OVERRIDES:
            continue
        elif entity_id in NEUTRAL_OVERRIDES:
            neutral.append(cs_name)
        elif entity_id in PASSIVE_OVERRIDES:
            passive.append(cs_name)
        elif mob_cat in MC_TO_MCC:
            cat = MC_TO_MCC[mob_cat]
            if cat == "hostile":
                hostile.append(cs_name)
            elif cat == "passive":
                passive.append(cs_name)
            elif cat == "non_living":
                non_living.append(cs_name)
            else:
                non_living.append(cs_name)
        else:
            non_living.append(cs_name)

    output = {
        "version": root.name.replace("-decompiled", "").replace("-client", ""),
        "hostile": sorted(hostile),
        "passive": sorted(passive),
        "neutral": sorted(neutral),
        "non_living": sorted(non_living),
    }

    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    with open(OUTPUT_PATH, 'w') as f:
        json.dump(output, f, indent=2)

    print(f"\nGenerated {OUTPUT_PATH}")
    print(f"  hostile:    {len(hostile)}")
    print(f"  passive:    {len(passive)}")
    print(f"  neutral:    {len(neutral)}")
    print(f"  non_living: {len(non_living)}")
    print(f"  total:      {len(hostile) + len(passive) + len(neutral) + len(non_living)}")


if __name__ == "__main__":
    main()
