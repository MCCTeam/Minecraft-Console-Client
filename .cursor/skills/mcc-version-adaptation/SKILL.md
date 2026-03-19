---
name: mcc-version-adaptation
description: Adapt MCC palettes and protocol handling for a new Minecraft version. Use when the user wants to add support for a new MC version, compare version registries, update item/entity/block/metadata palettes, or fix protocol mismatches between MC versions.
---

# MCC Version Adaptation

Systematic workflow for updating Minecraft Console Client to support a new Minecraft version, focusing on palette/registry changes and entity metadata.

## Prerequisites

- Decompiled server source for both the old and new MC versions in `$MCC_REPO/MinecraftOfficial/<version>-decompiled/`
- If missing, decompile first:
  ```bash
  cd $MCC_REPO/MinecraftOfficial
  java -jar MinecraftDecompiler.jar --version <ver> --side SERVER \
    --decompile --output <ver>-remapped.jar --decompiled-output <ver>-decompiled
  ```

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

## Step 2: Generate Updated Palettes

For registries marked "PALETTE UPDATE NEEDED":

### Item Palette
```bash
python3 $MCC_REPO/tools/gen_item_palette.py <new_ver> <suffix>
# e.g., gen_item_palette.py 1.21.1 121
```
- If new items are reported missing from `ItemType.cs`, add them to the enum in alphabetical order.
- The script auto-generates the C# palette file.

### Entity Metadata Palette
```bash
python3 $MCC_REPO/tools/gen_entity_metadata_palette.py <new_ver> <suffix>
# e.g., gen_entity_metadata_palette.py 1.20.6 1206
```
- If new serializer types appear as UNMAPPED, add them to both:
  1. The script's `FIELD_TO_ENUM` dictionary
  2. MCC's `EntityMetaDataType.cs` enum
  3. `DataTypes.cs` read logic (add a `case` to consume the correct bytes)

### Entity/Block Palettes
No generator script yet — these change rarely. When needed, manually create by following the pattern of existing palette files, using `register("name", ...)` call order from the decompiled source.

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

Pattern: add a new `>= MC_X_Y_Z_Version => new XxxPaletteXYZ()` case.

## Step 4: Check Variant Encoding Changes

For entity types that use variant serializers (Cat, Wolf, Frog, Painting), check if the codec changed between versions by inspecting:

- `EntityDataSerializers.java` — look at how each `*_VARIANT` field is constructed
- Key codecs:
  - `ByteBufCodecs.holderRegistry()` → wire format: `VarInt(registry_id)`
  - `ByteBufCodecs.holder()` → wire format: `VarInt(id+1)` for registered, `VarInt(0) + inline_data` for direct
- If codec changed, update `DataTypes.cs` entity metadata reading logic accordingly.

## Step 5: Handle New EntityDataSerializer Types

When new serializer types are added (detected in Step 1):

1. Add enum value to `EntityMetaDataType.cs` with XML doc comment
2. Add read logic in `DataTypes.cs` `ReadNextMetadata()`:
   - Determine byte consumption from the decompiled codec
   - Examples: VarInt read, list of particles, etc.
3. Create the new palette file (Step 2)
4. Update palette routing (Step 3)

## Step 6: Compile and Verify

```bash
dotnet build $MCC_REPO/MinecraftClient.sln -c Release
```

Then connect to a test server of the target version (see `mcc-dev-workflow` skill) and verify:
- Successful connection
- `/give` new items → check inventory
- Summon entities (especially variant types) → no metadata parse errors
- Particle effects → no crashes

## Key Source Files Reference

| Decompiled Java Source | Purpose |
|----------------------|---------|
| `world/item/Items.java` | Item registry (field declaration order = ID) |
| `world/entity/EntityType.java` | Entity type registry (`register()` call order = ID) |
| `world/level/block/Blocks.java` | Block registry (`register()` call order = ID) |
| `core/component/DataComponents.java` | Data component registry |
| `network/syncher/EntityDataSerializers.java` | Entity metadata type registry (static block order = ID) |

## Common Pitfalls

- **ID order matters**: IDs are determined by declaration/registration order, not alphabetical. Always use the decompiled source as ground truth.
- **Cross-version jumps**: When MCC skips versions (e.g., 1.20.4→1.20.6), registries from ALL intermediate versions may have changed. Always diff against the actual last-supported version, not the latest palette.
- **EntityMetadata type shifts**: A single new serializer type shifts all subsequent IDs, causing widespread metadata parse failures. Symptoms: entity rendering glitches, disconnections, or silent data corruption.
- **CUT_STANDSTONE_SLAB**: This is an intentional typo in Minecraft source (should be SANDSTONE). MCC's `ItemType.cs` uses `CutSandstoneSlab` — the gen script handles this via the OVERRIDES dict.

## Reusable Scripts

All scripts are in `$MCC_REPO/tools/`. See `tools/README.md` for detailed usage.
