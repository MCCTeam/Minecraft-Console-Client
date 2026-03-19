# MCC Version Adaptation Tools

Scripts for analyzing Minecraft version differences and generating MCC palette files.

Requires: Python 3.10+, decompiled MC server source in `MinecraftOfficial/<version>-decompiled/`.

## Decompiling a new MC version

```bash
cd MinecraftOfficial
java -jar MinecraftDecompiler.jar --version 1.21.4 --side SERVER \
  --decompile --output 1.21.4-remapped.jar --decompiled-output 1.21.4-decompiled
```

## diff_registries.py — Compare registries between versions

Compares Items, EntityTypes, Blocks, DataComponents, and EntityDataSerializers between two MC versions. Reports whether each palette needs updating, lists added/removed entries, and shows ID shift statistics.

```bash
python3 tools/diff_registries.py 1.20.6 1.21.1
```

Output indicates for each registry:
- **IDENTICAL** → reuse existing palette
- **PALETTE UPDATE NEEDED** → create new palette file + update version routing

## gen_item_palette.py — Generate ItemPalette C# file

Reads `Items.java` field declaration order to generate a complete `ItemPaletteXXX.cs`.

```bash
python3 tools/gen_item_palette.py 1.21.1 121
# → MinecraftClient/Inventory/ItemPalettes/ItemPalette121.cs
```

Also validates each item name against `ItemType.cs` and warns about missing enum values.

## gen_entity_metadata_palette.py — Generate EntityMetadataPalette C# file

Reads `EntityDataSerializers.java` static block registration order to generate `EntityMetadataPaletteXXX.cs`.

```bash
python3 tools/gen_entity_metadata_palette.py 1.20.6 1206
# → MinecraftClient/Mapping/EntityMetadataPalettes/EntityMetadataPalette1206.cs
```

The script maps Java field names to MCC's `EntityMetaDataType` enum. If a new serializer type appears that isn't in the mapping table, it will warn you to update both the script's `FIELD_TO_ENUM` dict and MCC's `EntityMetaDataType.cs` enum.
