# Create a marketplace

This tutorial publishes a plugin catalogue with immutable release archives. Start with the [Session Journal plugin tutorial](../plugins/development/tutorial/index.md) if you still need a plugin.

A Git repository keeps source, manifests, and release history. Download assets contain the code that MCC loads. A GitHub Release or another stable HTTPS host can store those archives.

## Chapter 1: Prepare the repository

Use this layout:

```text
my-plugins/
├── src/
│   └── SessionJournal/
│       ├── SessionJournal.cs
│       ├── SessionJournal.csproj
│       ├── plugin.toml
│       ├── defaults/settings.toml
│       ├── lang/en.toml
│       └── man/en/session-journal.md
├── tests/
├── tools/PackRelease/
├── marketplace/
│   ├── mcc-marketplace.toml
│   └── catalog/session-journal.toml
└── release-assets/                  # Local output, ignore in Git
```

1. Create the source folder.
2. Copy your tested plugin files into it.
3. Add `release-assets/`, `bin/`, and `obj/` to `.gitignore`.
4. Pin DMCBK and UMPK package versions in the author projects.
5. Build the plugin without sibling source references.
6. Execute its source and compiled loading tests.

Plugin authors need NuGet packages, not MCC source or a submodule. The [Marketplace repository](https://github.com/MCCTeam/Marketplace) demonstrates packaging with the published library versions used by MCC. Pin and test your own dependencies before publishing.

## Chapter 2: Define the release contract

The following source manifest belongs at the package root. The Session Journal entry from the tutorial requires Commands and Beacon.

```toml
schema-version = 2
id = "session-journal"
version = "1.0.0"
kind = "source"
target = "any"
entry = "SessionJournal.cs"
framework = "net10.0"
api-version = "1.0"
dmcbk = ">=0.1.0-preview.3 <0.2.0"
umpk = ">=0.9.0-beta.4 <0.10.0"
needs = ["commands", "beacon"]
man = ["session-journal"]
```

For the compiled asset, set `kind = "compiled"` and `entry = "SessionJournal.dll"`. Keep the same plugin version, dependencies, and compatibility fields. `any` is suitable for this portable managed example.

A native dependency requires a tested archive for each supported operating system and process architecture. Use exact targets such as `win-x64` or `osx-arm64`. Do not declare targets that you did not build.

Add required dependencies under `[requires]`. Add optional providers under `[optional]`. Use `[exports]` only for public contract assemblies. See [dependency loading](../plugins/development/dependencies-and-loading.md) for shared type identity.

## Chapter 3: Pack with the shared models

Create a small packaging tool. Run these commands from the repository root:

```sh
dotnet new console --framework net10.0 --output tools/PackRelease
dotnet add tools/PackRelease package DMCBK.Marketplace --version 0.1.0-preview.7
```

Replace `tools/PackRelease/Program.cs` with:

```csharp
using DMCBK.Marketplace;

if (args.Length != 6)
{
    Console.Error.WriteLine("Usage: <author> <payload> <output> <kind> <target> <asset-base-url>");
    return 2;
}

PackedPlugin packed = PluginPackageBuilder.Pack(
    Path.GetFullPath(args[0]),
    Path.GetFullPath(args[1]),
    Path.GetFullPath(args[2]),
    args[3],
    args[4],
    new Uri(args[5]));
Console.WriteLine(packed.ArchivePath);
Console.WriteLine(packed.Release.Assets[0].Sha256);
return 0;
```

For a source asset, the author and payload directories are the same:

```sh
dotnet run --project tools/PackRelease --   src/SessionJournal src/SessionJournal release-assets source any   https://publisher.example/releases/session-journal/1.0.0/
```

The URL is a placeholder for your final archive directory. Packaging does not upload files. Replace it with your real stable release URL before generating publication metadata.

For a compiled asset, build the DLL and prepare a separate payload folder. Include the entry DLL, its `.deps.json`, and private dependencies. Do not include DMCBK, UMPK, or host framework assemblies.

The shared packager copies `lang`, `man`, and `defaults` from the author folder. It converts an author `settings.toml` into packaged `defaults/settings.toml`. It writes a deterministic archive, checksum sidecar, and catalogue fragment.

Do not regenerate different bytes under an already published plugin version. A changed payload needs a new version.

## Chapter 4: Compose the release catalogue

One catalogue contains all releases for one plugin. A release can contain both source and compiled assets.

```toml
schema-version = 2
id = "session-journal"

[[releases]]
version = "1.0.0"
api-version = "1.0"
dmcbk = ">=0.1.0-preview.3 <0.2.0"
umpk = ">=0.9.0-beta.4 <0.10.0"
framework = "net10.0"
needs = ["commands", "beacon"]
yanked = false
assets = [
  { kind = "source", target = "any", url = "https://publisher.example/releases/session-journal/1.0.0/session-journal-source.zip", sha256 = "REPLACE_WITH_GENERATED_64_CHARACTER_DIGEST" },
  { kind = "compiled", target = "any", url = "https://publisher.example/releases/session-journal/1.0.0/session-journal-any.zip", sha256 = "REPLACE_WITH_GENERATED_64_CHARACTER_DIGEST" },
]
```

This illustrates the format. It is not an installable catalogue until you replace both URLs and digests with generated values.

Use `ReleaseCatalogueBuilder.Compose` to combine unpublished asset fragments for one version. Use `ReleaseCatalogueBuilder.Publish` to append the release to existing history. The official `PluginPack catalogue <fragment-directory> <catalogue-directory>` command wraps these models.

Compose every asset before first publication. Adding another asset later changes the immutable release identity. Publish another plugin version instead.

Keep existing releases in the catalogue. Set `yanked = true` to exclude a bad release from ordinary new selection. Yanking does not automatically uninstall an existing compatible selection.

## Chapter 5: Add the marketplace index

Create `marketplace/mcc-marketplace.toml`:

```toml
schema-version = 2
id = "my-community"
name = "My community plugins"

[[plugins]]
id = "session-journal"
description = "Counts successful sessions and exposes a journal command."
tags = ["utility", "beacon"]
releases = "catalog/session-journal.toml"
```

The `releases` path is relative to the index. The index describes identities, not one current binary per identity.

An installation registry is different. MCC writes publisher bindings in its configurations `marketplaces.toml`. The installed `plugins.lock.toml` records local active selections. Do not publish either file as a substitute for your catalogue.

## Chapter 6: Publish in order

1. Build each declared target.
2. Execute the available target tests.
3. Pack every release asset.
4. Compose the catalogue fragments.
5. Check dependency and compatibility fields.
6. Upload the archives and checksum sidecars.
7. Download each archive through its final URL.
8. Check that its SHA-256 matches the catalogue.
9. Publish the release catalogue and index.
10. Test installation through MCC.

A successful cross-build does not prove native library loading on another operating system. Record which targets only built and which also executed.

Do not publish catalogue entries that point at a future release. Users should never encounter an asset that you intend to upload later.

## Chapter 7: Test the published marketplace

Start MCC with a clean temporary runtime directory. Keep credentials out of the repository. Enter these commands, using your real index URL:

```text
/plugins marketplace add https://publisher.example/marketplace/mcc-marketplace.toml as tutorial
/plugins search session-journal in tutorial
/plugins install session-journal@tutorial 1.0.0
/plugins enable session-journal
/plugins info session-journal
/plugins deps session-journal
```

Check the selected version, target, digest, and activation state. Connect to a controlled server and test the plugin callback. Then test source selection with `--source` in a separate clean installation.

For a mixed release, test unsupported compiled targets with and without `--source-fallback`. The first request must explain the missing target. The second request may select compatible source.

Test dependency conflicts, bad checksums, and missing assets with disposable test metadata. Keep those fixtures separate from the published catalogue.

## Chapter 8: Maintain release history

1. Increase the plugin version when code or payload changes.
2. Test new compatibility ranges.
3. Pack new archives.
4. Upload and check those archives.
5. Append the new catalogue release.
6. Keep older catalogue entries and assets available.

Changing user defaults does not overwrite existing settings. Design storage migrations if the new plugin changes its saved data. Cached package rollback cannot reverse an arbitrary migration.

[Format reference](format.md) · [Plugin authoring tutorial](../plugins/development/tutorial/index.md) · [Use marketplaces](using.md)
