# MCC Version Adaptation Tools

Scripts for analyzing Minecraft version differences and generating MCC palette files.

Requires: Python 3.10+

## Data Sources

Two types of data can be used as input:

| Source | How to Get | Authoritative? |
|--------|-----------|---------------|
| Decompiled Java source | `MinecraftDecompiler.jar` → `MinecraftOfficial/<ver>-decompiled/` | Mostly (see caveat below) |
| Server data reports | `java -DbundlerMainClass=net.minecraft.data.Main -jar server.jar --reports` | **Yes** |

**Important since MC 1.21.9**: Some items and blocks are registered outside `Items.java`/`Blocks.java` field declarations (via block registration callbacks). In these cases, the decompiled source undercounts entries. **Always use server data reports** for item and block palettes when available.

### Decompiling a new MC version

```bash
# Server side (default) — also downloads server.jar into MinecraftOfficial/downloads/<ver>/
tools/decompile.sh --version 1.21.9

# Client side
tools/decompile.sh --version 1.21.9 --side CLIENT
```

The script auto-downloads `MinecraftDecompiler.jar` from GitHub releases if it doesn't exist.

### Generating server data reports

```bash
cd /tmp
java -DbundlerMainClass=net.minecraft.data.Main \
  -jar $MCC_SERVERS/<version>/server.jar \
  --reports --output /tmp/mc_reports
```

This generates:
- `/tmp/mc_reports/reports/registries.json` — all registries with protocol IDs
- `/tmp/mc_reports/reports/blocks.json` — all blocks with block state IDs
- `/tmp/mc_reports/reports/packets.json` — packet protocol definitions

## diff_registries.py — Compare registries between versions

Compares Items, EntityTypes, Blocks, DataComponents, and EntityDataSerializers between two MC versions. Reports whether each palette needs updating, lists added/removed entries, and shows ID shift statistics.

```bash
# Basic comparison (decompiled source only)
python3 tools/diff_registries.py 1.21.8 1.21.9

# With cross-validation against server registries.json (recommended)
python3 tools/diff_registries.py 1.21.8 1.21.9 --registry /tmp/mc_reports/reports/registries.json
```

The `--registry` flag enables cross-validation: compares the count and set of entries found in decompiled Java source against the server's authoritative registry. Any mismatches indicate that palette generation must use server data instead of Java source.

Output indicates for each registry:
- **IDENTICAL** → reuse existing palette
- **PALETTE UPDATE NEEDED** → create new palette file + update version routing
- **Count MISMATCH** (with --registry) → server has entries not in Java source

## gen_item_palette.py — Generate ItemPalette C# file

Two modes:

```bash
# Preferred: from server registries.json (accurate since 1.21.9)
python3 tools/gen_item_palette.py --from-registry /tmp/mc_reports/reports/registries.json 1219

# Legacy: from decompiled Items.java
python3 tools/gen_item_palette.py 1.21.1 121
```

Output: `MinecraftClient/Inventory/ItemPalettes/ItemPalette<suffix>.cs`

Validates each item name against `ItemType.cs` and warns about missing enum values. Add missing values to `ItemType.cs` in alphabetical order before compiling.

## gen_block_palette.py — Generate BlockPalette C# file

```bash
python3 tools/gen_block_palette.py /tmp/mc_reports/reports/blocks.json 1219
# → MinecraftClient/Mapping/BlockPalettes/Palette1219.cs
```

Generates a complete block palette with block state ID ranges from the server's `blocks.json`. Validates against `Material.cs` and warns about missing enum values.

## gen_entity_palette.py — Generate EntityPalette C# file

```bash
python3 tools/gen_entity_palette.py /tmp/mc_reports/reports/registries.json 1219
# → MinecraftClient/Mapping/EntityPalettes/EntityPalette1219.cs
```

Generates entity type palette from server's `registries.json`. Validates against `EntityType.cs` and warns about missing enum values.

## gen_entity_metadata_palette.py — Generate EntityMetadataPalette C# file

```bash
python3 tools/gen_entity_metadata_palette.py 1.21.9 1219
# → MinecraftClient/Mapping/EntityMetadataPalettes/EntityMetadataPalette1219.cs
```

Reads `EntityDataSerializers.java` static block registration order. Maps Java field names to MCC's `EntityMetaDataType` enum. If a new serializer type appears that isn't in the mapping table, it will warn you to update:
1. The script's `FIELD_TO_ENUM` dict
2. MCC's `EntityMetaDataType.cs` enum
3. `DataTypes.cs` ReadNextMetadata() read logic

## gen_command_argument_registry.py — Generate DeclareCommands registry arrays

```bash
python3 tools/gen_command_argument_registry.py 1.20.6 1.21.5 1.21.6
```

Reads `ArgumentTypeInfos.java`, skips the `SharedConstants.IS_RUNNING_IN_IDE` block, and prints C# array initializers for the runtime `COMMAND_ARGUMENT_TYPE` registry order. Use this when Mojang inserts new command argument types and the modern `DeclareCommands` parser needs updated ID routing.

## gen_block_shapes.py — Download & compact block collision shapes

Downloads block collision shapes from PrismarineJS `minecraft-data` and compacts them into a single JSON for MCC's physics engine.

```bash
# Auto-download for a specific MC version
python3 tools/gen_block_shapes.py 26.1
# → MinecraftClient/Physics/BlockShapeData.json

# From a local file (if network is slow)
python3 tools/gen_block_shapes.py --from-file /path/to/blockCollisionShapes.json
```

Output: `MinecraftClient/Physics/BlockShapeData.json` (embedded as a resource via `.csproj`).

Data source: `https://raw.githubusercontent.com/PrismarineJS/minecraft-data/master/data/pc/<version>/blockCollisionShapes.json`

Uses `curl` with resume (`-C -`) for reliable download over slow connections. Falls back to manual download if retries are exhausted.

## Recommended workflow

1. Generate server reports (Step 0)
2. Run `diff_registries.py --registry` to identify changes and validate source completeness
3. For each registry needing update:
   - Items: `gen_item_palette.py --from-registry`
   - Blocks: `gen_block_palette.py`
   - Entities: `gen_entity_palette.py`
   - Metadata: `gen_entity_metadata_palette.py`
4. Update block collision shapes: `gen_block_shapes.py`
5. Add any missing enum values to `ItemType.cs`, `Material.cs`, `EntityType.cs`, `EntityMetaDataType.cs`
6. Update version routing (see SKILL.md)
7. Build and test
