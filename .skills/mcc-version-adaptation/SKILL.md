---
name: mcc-version-adaptation
description: Adapt MCC palettes and protocol handling for a new Minecraft version. Use when the user wants to add support for a new MC version, compare version registries, update item/entity/block/metadata palettes, or fix protocol mismatches between MC versions.
---

# MCC Version Adaptation

Systematic workflow for updating Minecraft Console Client to support a new Minecraft version, focusing on palette/registry changes and entity metadata.

## Prerequisites

- Decompiled server source for both the old and new MC versions in `$MCC_REPO/MinecraftOfficial/<version>-decompiled/`
- If missing, decompile and download server.jar:
  ```bash
  $MCC_REPO/tools/decompile.sh --version <ver>
  ```
  This auto-downloads `MinecraftDecompiler.jar` if needed, produces the decompiled source, and downloads `server.jar` into `$MCC_SERVERS/<ver>/`.
- A test server of the target version in `$MCC_SERVERS/<version>/` (see `mcc-dev-workflow` skill)

## Step 0: Generate Server Reports (CRITICAL since 1.21.9)

**Before** analyzing decompiled source, generate authoritative registry data from the server jar:

```bash
cd /tmp && java -DbundlerMainClass=net.minecraft.data.Main \
  -jar $MCC_SERVERS/<version>/server.jar \
  --reports --output /tmp/mc_reports
```

This produces `/tmp/mc_reports/reports/` containing:
- `registries.json` — all registries with **actual protocol_id** for each entry
- `blocks.json` — all blocks with **block state IDs**
- `packets.json` — packet protocol definitions

**Why this matters**: Since MC 1.21.9, some items and blocks are registered outside `Items.java`/`Blocks.java` field declarations (via block registration callbacks or other paths). The decompiled source alone will **miss** these entries. The server data generator is the only authoritative source for protocol IDs.

### Validation check
Compare server registry counts against decompiled source counts:
```bash
python3 -c "
import json
with open('/tmp/mc_reports/reports/registries.json') as f:
    data = json.load(f)
for reg in ['minecraft:item', 'minecraft:entity_type', 'minecraft:block']:
    print(f'{reg}: {len(data[reg][\"entries\"])} entries')
"
```

If server counts differ from decompiled Java source counts, the palette **must** be generated from server data, not from Java source.

## Step 1: Run Registry Diff

```bash
python3 $MCC_REPO/tools/diff_registries.py <old_ver> <new_ver>
```

This compares five registries and reports which need palette updates:

| Registry | MCC File | When to Update |
|----------|----------|----------------|
| Items.java | `ItemPalettes/ItemPaletteXXX.cs` | New/removed/reordered items |
| EntityType.java | `EntityPalettes/EntityPaletteXXX.cs` | New/removed/reordered entity types |
| Blocks.java | `BlockPalettes/BlockPaletteXXX.cs` | New/removed/reordered blocks |
| DataComponents.java | `StructuredComponents/StructuredComponentsRegistryXXX.cs` | New/reordered components |
| EntityDataSerializers.java | `EntityMetadataPalettes/EntityMetadataPaletteXXX.cs` | New/reordered serializer types |

**Important**: diff_registries.py compares decompiled Java source. If Step 0 revealed count mismatches, the diff output may undercount. Always cross-reference with server registries.json.

## Step 2: Generate Updated Palettes

For registries marked "PALETTE UPDATE NEEDED":

### Item Palette

**Preferred method** (accurate since 1.21.9):
```bash
python3 $MCC_REPO/tools/gen_item_palette.py --from-registry /tmp/mc_reports/reports/registries.json <suffix>
# e.g., gen_item_palette.py --from-registry /tmp/mc_reports/reports/registries.json 1219
```

**Legacy method** (works for versions where Items.java has all items):
```bash
python3 $MCC_REPO/tools/gen_item_palette.py <new_ver> <suffix>
# e.g., gen_item_palette.py 1.21.1 121
```

- If new items are reported missing from `ItemType.cs`, add them to the enum in alphabetical order.
- The script auto-generates the C# palette file.

### Block Palette

**Preferred method** (accurate since 1.21.9):
```bash
python3 $MCC_REPO/tools/gen_block_palette.py /tmp/mc_reports/reports/blocks.json <suffix>
# e.g., gen_block_palette.py /tmp/mc_reports/reports/blocks.json 1219
```

**Legacy method** (manual creation from decompiled Blocks.java): Follow the pattern of existing palette files, using `register("name", ...)` call order from the decompiled source. Only reliable when Blocks.java contains all blocks.

If new blocks are reported missing from `Material.cs`, add them to the enum in alphabetical order.

### Entity Palette

```bash
python3 $MCC_REPO/tools/gen_entity_palette.py /tmp/mc_reports/reports/registries.json <suffix>
# e.g., gen_entity_palette.py /tmp/mc_reports/reports/registries.json 1219
```

If new entity types are reported missing from `EntityType.cs`, add them to the enum in alphabetical order.

### Entity Metadata Palette
```bash
python3 $MCC_REPO/tools/gen_entity_metadata_palette.py <new_ver> <suffix>
# e.g., gen_entity_metadata_palette.py 1.20.6 1206
```
- If new serializer types appear as UNMAPPED, add them to both:
  1. The script's `FIELD_TO_ENUM` dictionary
  2. MCC's `EntityMetaDataType.cs` enum
  3. `DataTypes.cs` read logic (add a `case` to consume the correct bytes)

### DataComponents / StructuredComponents
Compare `DataComponents.java` registration order. If new components appear, update `StructuredComponentsRegistryXXX.cs`. For new component types, implement corresponding reader in `StructuredComponents/Components/`.

## Step 3: Update Version Routing

After creating palette files, update version selection logic:

| Palette Type | Routing Location |
|-------------|-----------------|
| Item | `Protocol18.cs` → `itemPalette` switch expression |
| Entity | `Protocol18.cs` → `entityPalette` switch expression |
| Block | `Protocol18.cs` → `blockPalette` initialization |
| EntityMetadata | `EntityMetadataPalette.cs` → `GetPalette()` switch |
| DataComponents | `StructuredComponentsRegistry.cs` → factory/routing |
| Packet | `PacketType18Handler.cs` → `GetTypeHandler()` switch |

Pattern: add a new `>= MC_X_Y_Z_Version => new XxxPaletteXYZ()` case.

Also update:
- `Protocol18.cs`: add `MC_X_Y_Z_Version = <protocol_number>` constant
- `Protocol18.cs`: update all `> MC_prev_Version` upper-bound checks to `> MC_X_Y_Z_Version`
- `ProtocolHandler.cs`: add version string → protocol mapping, protocol → version mapping, add to supported list
- `Program.cs`: update `MCHighestVersion`

## Step 4: Check Packet Changes

Compare `GameProtocols.java` and `ConfigurationProtocols.java` between versions.

Common patterns:
- **New clientbound packets inserted mid-list**: All subsequent packet IDs shift. Requires a new `PacketPalette` class.
- **New packets appended at end**: Only need to add new enum values and entries in the palette.
- **Packet renames** (same slot): Update MCC's packet type enum name but no ID change.

When packet changes are detected:
1. Add new packet type enum values to `PacketTypesIn.cs`, `PacketTypesOut.cs`, `ConfigurationPacketTypesIn.cs`, `ConfigurationPacketTypesOut.cs`
2. Create new `PacketPaletteXXX.cs` based on the previous one, adjusting IDs
3. Update `PacketType18Handler.cs` routing

## Step 5: Check Variant Encoding Changes

For entity types that use variant serializers (Cat, Wolf, Frog, Painting), check if the codec changed between versions by inspecting:

- `EntityDataSerializers.java` — look at how each `*_VARIANT` field is constructed
- Key codecs:
  - `ByteBufCodecs.holderRegistry()` → wire format: `VarInt(registry_id)`
  - `ByteBufCodecs.holder()` → wire format: `VarInt(id+1)` for registered, `VarInt(0) + inline_data` for direct
- If codec changed, update `DataTypes.cs` entity metadata reading logic accordingly.

## Step 6: Handle New EntityDataSerializer Types

When new serializer types are added (detected in Step 1):

1. Add enum value to `EntityMetaDataType.cs` with XML doc comment
2. Add read logic in `DataTypes.cs` `ReadNextMetadata()`:
   - Determine byte consumption from the decompiled codec
   - Simple enum types (like CopperGolemState, WeatheringCopperState): `ReadNextVarInt(cache)`
   - Composite types (like ResolvableProfile): analyze the STREAM_CODEC chain in decompiled source
3. Create the new palette file (Step 2)
4. Update palette routing (Step 3)

## Step 7: Check SpawnEntity / Other Packet Format Changes

Compare key packet codec classes between versions. Known changes:
- **1.21.9+**: `SpawnEntity` velocity fields changed from `short / 8000.0` to `LpVec3` format (VarLong-packed fixed-point). Gate reading in `DataTypes.ReadNextEntity()` by version.

When in doubt, compare the relevant packet class (e.g. `ClientboundAddEntityPacket.java`) between versions.

## Step 8: Update Block Collision Shapes (Physics Engine)

MCC's physics engine uses block collision shape data from PrismarineJS `minecraft-data` to perform accurate AABB collision detection (stored in `MinecraftClient/Physics/BlockShapeData.json`, embedded as a resource).

When a new MC version introduces new blocks or changes block shapes, update this data:

```bash
# Download and compact collision shapes for the target version
python3 $MCC_REPO/tools/gen_block_shapes.py <version>
# e.g. python3 tools/gen_block_shapes.py 1.21.11
```

If network is slow or unreliable, download the file manually and convert:
```bash
# Manual download
curl -L -o /tmp/bcs.json \
  "https://raw.githubusercontent.com/PrismarineJS/minecraft-data/master/data/pc/<version>/blockCollisionShapes.json"

# Then compact from local file
python3 $MCC_REPO/tools/gen_block_shapes.py --from-file /tmp/bcs.json
```

Output: `MinecraftClient/Physics/BlockShapeData.json` (embedded via `MinecraftClient.csproj`)

The JSON maps block names (snake_case) → collision shape IDs → AABB coordinates. At runtime, `BlockShapes.cs` maps MCC's block state IDs to these AABBs using the block palette.

**When to update**: Whenever new blocks are added that have non-trivial collision shapes (e.g., new slab variants, stairs, fences). If only items or entities changed, this step can be skipped.

**Data source**: PrismarineJS `minecraft-data` repo, path: `data/pc/<version>/blockCollisionShapes.json`. Version availability can be checked via `data/dataPaths.json`.

## Step 9: Compile and Verify

```bash
dotnet build $MCC_REPO/MinecraftClient.sln -c Release
```

Then connect to a test server of the target version (see `mcc-dev-workflow` skill) and verify:
- Successful connection
- `/give` new items → check inventory for correct identification
- `/give` existing items (diamond_sword, etc.) → verify no ID shift
- Summon new entities → check type and health
- Summon variant entities (wolf, cat, frog) → no metadata parse errors
- Place new blocks → `dig` reports correct block type
- Teleport to distant chunks → terrain loads without errors
- Chat commands work normally

**Always verify basic existing items first** (e.g. diamond_sword) to catch palette ID shift bugs early. If an existing item shows as the wrong type, the palette is using wrong protocol IDs.

## Key Source Files Reference

| Decompiled Java Source | Purpose |
|----------------------|---------|
| `world/item/Items.java` | Item registry (field declaration order ≈ ID, **but not always since 1.21.9**) |
| `world/entity/EntityType.java` | Entity type registry (`register()` call order = ID) |
| `world/level/block/Blocks.java` | Block registry (`register()` call order ≈ ID, **but not always since 1.21.9**) |
| `core/component/DataComponents.java` | Data component registry |
| `network/syncher/EntityDataSerializers.java` | Entity metadata type registry (static block order = ID) |
| `network/protocol/game/GameProtocols.java` | Play packet registration order (= packet IDs) |
| `network/protocol/configuration/ConfigurationProtocols.java` | Config packet registration order |

| Server Data Generator Output | Purpose |
|-----|---------|
| `registries.json` | **Authoritative** protocol_id for all registries |
| `blocks.json` | **Authoritative** block state IDs |
| `packets.json` | Packet protocol definitions |

## Common Pitfalls

- **Source field order ≠ runtime registry ID (since 1.21.9)**: Some items/blocks are registered via callbacks (e.g., block items registered by `Blocks.java` during block registration) rather than in `Items.java` field declarations. Always validate palette counts against server `registries.json`. If counts differ, **use server data generator output instead of decompiled source**.
- **ID order matters**: IDs are determined by registration order, not alphabetical. Always use server data generator as ground truth.
- **Cross-version jumps**: When MCC skips versions (e.g., 1.20.4→1.20.6), registries from ALL intermediate versions may have changed. Always diff against the actual last-supported version, not the latest palette.
- **EntityMetadata type shifts**: A single new serializer type shifts all subsequent IDs, causing widespread metadata parse failures. Symptoms: entity rendering glitches, disconnections, or silent data corruption.
- **CUT_STANDSTONE_SLAB**: This is an intentional typo in Minecraft source (should be SANDSTONE). MCC's `ItemType.cs` uses `CutSandstoneSlab` — the gen script handles this via the OVERRIDES dict.
- **Item/block renames across versions**: Some items/blocks get renamed (e.g., `DRY_SHORT_GRASS` → `SHORT_DRY_GRASS`, `CHAIN` → `IRON_CHAIN`). Keep old enum values for backward compatibility with older palettes, and add new ones for the new version.
- **Packet ID cascading shifts**: Even one inserted mid-list clientbound packet shifts ALL subsequent IDs. Always create a new PacketPalette for protocol changes.
- **Test existing items first**: After palette changes, always verify existing items (diamond_sword, stone, etc.) before testing new ones. If they show as wrong items, the palette has a systemic ID offset bug.

## Reusable Scripts

All scripts are in `$MCC_REPO/tools/`. See `tools/README.md` for detailed usage.

| Script | Purpose | Input |
|--------|---------|-------|
| `diff_registries.py` | Compare registries between versions | Decompiled source |
| `gen_item_palette.py` | Generate ItemPalette C# | Decompiled source OR registries.json |
| `gen_block_palette.py` | Generate BlockPalette C# | blocks.json |
| `gen_entity_palette.py` | Generate EntityPalette C# | registries.json |
| `gen_entity_metadata_palette.py` | Generate EntityMetadataPalette C# | Decompiled source |
| `gen_block_shapes.py` | Download & compact block collision shapes | PrismarineJS minecraft-data |
