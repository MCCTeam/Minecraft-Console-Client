# Plugin marketplace v2

Marketplace metadata is separate from plugin payloads. Git stores source, manifests, release declarations and historical catalogues. Release archives contain the selected source file or compiled assembly, private dependencies, manuals, localization and default settings. Binary history lives in release assets.

The runtime accepts only schema 2. Migrate older catalogues, mutable manifest flags and binary plugins that use the MCC SDK. User settings are separate from package contents.

## Catalogue files

`mcc-marketplace.toml` lists identities and relative catalogue locations:

```toml
schema-version = 2
id = "official"
name = "Official DMCBK Plugins"

[[plugins]]
id = "example-plugin"
description = "An example plugin."
tags = ["automation"]
releases = "catalog/example-plugin.toml"
```

Each catalogue keeps the complete release history:

```toml
schema-version = 2
id = "example-plugin"

[[releases]]
version = "2.0.0"
api-version = "1.0"
dmcbk = ">=0.1.0-preview.3 <0.2.0"
umpk = ">=0.9.0-beta.4 <0.10.0"
framework = "net10.0"
needs = ["commands"]
yanked = false
assets = [
  { kind = "compiled", target = "win-x64", url = "https://publisher.example/2.0.0/win-x64.zip", sha256 = "<64 hexadecimal characters>" },
  { kind = "compiled", target = "linux-x64", url = "https://publisher.example/2.0.0/linux-x64.zip", sha256 = "<64 hexadecimal characters>" },
  { kind = "source", target = "any", url = "https://publisher.example/2.0.0/source.zip", sha256 = "<64 hexadecimal characters>" },
]

[releases.requires]
shared-tools = "^2.1.0"

[releases.optional]
alerts = "^3.0.0"

[releases.hosts]
mcc = ">=2.0.0 <3.0.0"
```

The checksum placeholders above must be replaced by generated archive digests. Add another `[[releases]]` table for a new version. Payloads and compatibility metadata of a published release are immutable. Yank a release to remove it from new ordinary selection without deleting its history. The resolver can retain an already-installed yanked version that remains compatible. Yanking does not force an uninstall.

API compatibility requires the same major and at least the requested minor. DMCBK and UMPK ranges check actual library versions independently of application versions. Omitted or empty `hosts` permits any compatible application. A populated table restricts applications to those identities and version ranges. `needs` requires capabilities actually composed by the host, such as `commands`, `beacon` or `aspnetcore`.

A shared SemVer implementation checks version ranges during resolution and loading. It accepts exact versions, comparators, caret, tilde, wildcard and alternative ranges. Ordinary unrestricted requests exclude prereleases unless the request enables them. Exact prerelease requests remain exact. An explicit prerelease comparator can admit prereleases with the same major, minor and patch numbers. Required dependencies install transitively. Optional dependencies do not install automatically. They become available only when the installed version matches the declared range.

## Package manifest

Every archive has `plugin.toml` at its root:

```toml
schema-version = 2
id = "example-plugin"
version = "2.0.0"
kind = "compiled"
target = "win-x64"
entry = "ExamplePlugin.dll"
framework = "net10.0"
api-version = "1.0"
dmcbk = ">=0.1.0-preview.3 <0.2.0"
umpk = ">=0.9.0-beta.4 <0.10.0"
needs = ["commands"]
deps = ["lib/ExampleHelpers.dll"]
man = ["example-plugin"]

[requires]
shared-tools = "^2.1.0"

[optional]
alerts = "^3.0.0"

[hosts]
mcc = ">=2.0.0 <3.0.0"

[exports]
assemblies = ["ExamplePlugin.Contracts.dll"]
```

For a source asset, use `kind = "source"`, `target = "any"` and `entry = "ExamplePlugin.cs"`. Runtime compilation accepts the single entry source file and declared private assemblies. It does not run project builds or arbitrary NuGet restores. Put manuals in `man/en`, localization in the plugin's resource files, and default settings in the package's `defaults/settings.toml`.

Do not package DMCBK, UMPK or host framework assemblies. The loader shares provider contracts from `exports.assemblies` through required dependency contexts. Other dependencies stay private to a collectible plugin load context. Compiled packages can carry `.deps.json` and native dependencies for `AssemblyDependencyResolver`.

## Asset selection

Detection uses the operating system, process architecture and Linux libc. A 32-bit process on Windows x64 selects `win-x86`.

| System | Targets |
| --- | --- |
| Windows | `win-x86`, `win-x64`, `win-arm64` |
| Linux glibc | `linux-x64`, `linux-arm64`, `linux-arm` |
| Linux musl | `linux-musl-x64`, `linux-musl-arm64`, `linux-musl-arm` |
| macOS | `osx-x64`, `osx-arm64` |
| Portable managed | `any` |

Selection prefers an exact compiled target, then compiled `any`. Source-only releases can select a matching source asset automatically. Mixed releases require explicit source fallback permission when no compiled asset matches. An explicit source preference selects source first. Unsupported targets produce an explanation rather than downloading every release.

Installation downloads one selected archive for each changed plugin in the required graph. The installer reuses unchanged installed selections. A portable managed plugin normally publishes one compiled `any` asset. Plugins with native dependencies publish only the targets they actually build and check.

## Planning and transactions

`MarketplaceInstaller.PlanInstallAsync`, `PlanUpdateAsync`, `PlanUninstallAsync`, `PlanPolicyAsync` and `PlanRollbackAsync` return an immutable `InstallPlan`. Its changes include exact versions, targets, hashes and affected dependents. `ApplyAsync` verifies the plan against the current lock while holding the installation-root lock. A stale plan must be recreated.

The resolver selects one version per identity, preserves compatible installed selections, respects pins and publisher bindings, and checks existing dependents. Conflicts, missing providers and required cycles fail before installation changes. Compatible optional providers can affect activation order without creating a cycle.

```text
plugins/
├── versions/<id>/<version>/<target>/<sha256>/
├── userdata/<id>/settings.toml
├── userdata/<id>/data/
├── cache/downloads/
├── cache/source/<id>/
├── transactions/
└── plugins.lock.toml
```

The generated lock has `schema-version`, a revision and `[[plugins]]` entries. Each entry records its marketplace, exact `release` and selected `asset`, enabled state, pin, update policy, and direct source/revision when applicable. It is the active graph, not a manifest edited in place. Do not hand-edit it during client operation.

The installer stages downloads and extraction. It checks digests, archive traversal, links, size limits and catalogue agreement. Source compilation runs before commitment. Affected plugins stop in reverse dependency order. Immutable packages and the lock commit before enabled plugins start in dependency order. Failed activation restores the previous graph. Startup recovers interrupted transactions before loading plugins. The installer never overwrites loaded DLLs.

Updates preserve settings and data. Defaults form an overlay. Cached rollback restores package selections and dependencies. Rollback cannot undo arbitrary effects from plugin code. Purging data is a separate explicit uninstall choice.

Plugin update policies are `inherit`, `off`, `manual` and `automatic`. Marketplace binding `auto-update` values are `off`, `check` and `apply`. These policies operate at different levels. Pins prevent version changes.

Automatic checks honor offline state and a 24-hour metadata cache. Startup checks use a short randomized delay. Automatic updates defer application until disconnection. They create a new plan before applying those updates. Manual refresh explicitly fetches metadata. Planning uses the current cached catalogues, or refreshes a catalogue when its cache is absent.

A plugin with `inherit` follows its marketplace binding. `check` reports available changes. `apply` permits automatic application after the session ends. A plugin with `manual` reports available changes for manual action. Direct installer application does not itself defer for a live session. The host must choose an appropriate time.

## Authoring and local development

Use the templates and `PluginPack` in [Marketplace](https://github.com/MCCTeam/Marketplace). Build against pinned NuGet packages with no MCC, DMCBK or UMPK checkout. For unpublished preview development, supply an explicit local package feed and use an isolated NuGet cache whenever replacing packages at the same preview version.

Local folder, archive, direct URL and Git imports use the same manifest checks and immutable installation layout. Git imports record the selected commit. These paths are development sources. Marketplace dependencies still need explicit source bindings.

The host must supply accessible compilation reference assemblies for source plugins. Standard MCC distributions keep managed assemblies accessible. Single-file or embedded hosts must explicitly provide reference assets.

## Review an installation plan

1. Add a publisher binding to the registry file.
2. Create a plan for the requested plugin version.
3. Display every change to the user.
4. Apply the plan after the user accepts the changes.

```toml
schema-version = 2

[[marketplaces]]
id = "official"
source = "https://publisher.example/mcc-marketplace.toml"
auto-update = "off"
```

The following code assumes the host composed Marketplace and Plugins as shown in [hosting](https://github.com/MCCTeam/DMCBK/blob/master/docs/hosting.md).

```csharp
using DMCBK.Marketplace;

MarketplaceService market = client.GetModule<MarketplaceService>();
InstallPlan plan = await market.PlanInstallAsync(
    new ResolutionRequest("example-plugin", "official", ExactVersion: "2.0.0"));

foreach (PluginChange change in plan.Changes)
    Console.WriteLine($"{change.Id}: {change.PreviousVersion} -> {change.Version} ({change.Target})");

// Call this only after your host receives approval for the displayed plan.
PluginOperationResult applied = await market.ApplyAsync(plan);
```

The example URL and checksums are placeholders. Replace them with real release metadata before installation. `ApplyAsync` checks the current lock again. A changed lock invalidates the plan, so the host must request a new plan.

## Understand the three metadata layers

The index answers: which plugins does this publisher offer? The release catalogue answers: which versions and assets exist? The package manifest answers: what is inside this archive?

The installation lock records the local selection. It also records user policy, such as enabled state and pins. It does not replace the publisher's catalogue.

| File | Writer | Reader | Changes when |
| --- | --- | --- | --- |
| Marketplace index | Publisher | Catalogue client | A plugin identity appears or disappears |
| Release catalogue | Publisher | Dependency resolver | The publisher publishes or yanks a release |
| `plugin.toml` | Plugin author or packager | Installer and loader | The author builds a new package version |
| `plugins.lock.toml` | Installer | Installer and runtime | Local selection or policy changes |

Keep the manifest identity, version and compatibility fields consistent with the selected catalogue release. The installer rejects disagreement before activation.

## Choose source, compiled or mixed releases

A source package contains one entry C# file. It suits small plugins and hosts that provide runtime compilation references. A compiled package contains the plugin DLL and its private dependencies. It suits larger projects or plugins with native dependencies.

A mixed release publishes both kinds under the same plugin version. Each archive has its own matching manifest. All assets for that version share the same compatibility and dependency declarations.

Pure C# code usually uses compiled `any`. That means the plugin payload is portable managed code. It does not mean a native dependency becomes portable.

Publishing six platform archives does not make installation download all six. Selection chooses one suitable archive for each changed plugin. Required plugin dependencies can add further downloads.

## Select a version deliberately

| Request | Meaning |
| --- | --- |
| `ExactVersion: "2.0.0"` | Select precisely version 2.0.0 or fail |
| `Range: ">=2.0.0 <3.0.0"` | Allow compatible 2.x releases |
| `Range: "^2.1.0"` | Allow releases from 2.1.0 up to the next major boundary |
| `Range: "~2.1.0"` | Allow releases from 2.1.0 below 2.2.0 |
| `Range: "*"` | Allow compatible versions under the prerelease policy |

These examples describe ordinary positive major versions. Caret ranges below 1.0 have narrower boundaries. Use the shared `SemVerRange` parser rather than implementing version comparison yourself.

An install can preserve an installed version that already satisfies the request. An update asks the resolver to prefer newer versions for the requested identity. Compatibility, dependent constraints and pins still apply.

Exact requests do not silently move to another version. Normal selection excludes yanked releases. A prerelease needs the request's prerelease policy, an exact prerelease request or a matching explicit prerelease comparator. A plugin version alone does not establish compatibility with the host.

## Configure source selection in a request

The following fragments create requests. They do not download or install anything.

```csharp
using DMCBK.Marketplace;

var exact = new ResolutionRequest("example-plugin", "official", ExactVersion: "2.0.0");
var compatible = new ResolutionRequest("example-plugin", "official", Range: ">=2.0.0 <3.0.0");
var source = exact with { Assets = new AssetPolicy(PreferSource: true) };
var fallback = exact with { Assets = new AssetPolicy(AllowSourceFallback: true) };
Console.WriteLine(source.Assets);
Console.WriteLine(fallback.Assets);
```

`PreferSource` requires a suitable source asset. It does not silently select a binary when source is absent. `AllowSourceFallback` permits source only after compiled selection fails. Source-only releases already permit automatic source selection.

## Compose a host marketplace

Use the same absolute plugin root for Plugins and Marketplace. The registry is a separate host-selected file. Attach Commands before Plugins. Attach Marketplace after Plugins so its runtime factory can obtain `PluginHost`.

```csharp
using DMCBK.Core;
using DMCBK.Marketplace;
using DMCBK.PluginSdk;

string pluginRoot = Path.GetFullPath("client-data/plugins");
string registry = Path.GetFullPath("client-data/marketplaces.toml");
await using Client client = new ClientBuilder()
    .UseServer("localhost")
    .UseUsername("GuideBot")
    .UseCommands()
    .UsePlugins(new PluginOptions(pluginRoot))
    .UseMarketplace(new MarketplaceOptions(pluginRoot, registry)
    {
        Runtime = current => current.GetModule<PluginHost>()
    })
    .Build();

MarketplaceService market = client.GetModule<MarketplaceService>();
Console.WriteLine(market.IncludePrerelease);
```

This constructs the service without contacting a publisher. Use `MarketplaceAddAsync` to validate and add a publisher binding. Check the returned action result before planning an installation.

Convenience installation methods use the service's confirmation callback. Its default answer is refusal. Explicit planning and `ApplyAsync` let your interface present a review screen itself. `ApplyAsync` is the application step and does not ask for approval again.

## Build release archives with shared models

`PluginPackageBuilder.Pack` creates an archive, a SHA-256 sidecar and a catalogue fragment. It uses deterministic file order and archive timestamps. It rejects a different payload at the same output identity.

A source pack uses the author folder as both manifest and payload folder. A compiled pack uses the author folder for resources and a separate build output folder for the DLLs. Keep the compiled payload outside `bin` and `obj` subdirectories inside the selected payload root.

```csharp
using DMCBK.Marketplace;

string authorFolder = Path.GetFullPath("example-plugin");
string outputFolder = Path.GetFullPath("release-assets");
PackedPlugin packed = PluginPackageBuilder.Pack(
    authorFolder, authorFolder, outputFolder,
    "source", "any", new Uri("https://publisher.example/releases/2.0.0/"));
Console.WriteLine(packed.ArchivePath);
Console.WriteLine(packed.Release.Assets[0].Sha256);
```

This fragment requires an existing valid source manifest and entry file. The URL is the future published asset location. Packaging does not upload files. The complete runnable [marketplace sample](https://github.com/MCCTeam/DMCBK/blob/master/samples/MarketplaceGuide/README.md) creates its own valid input and checks installation.

The packager copies `lang`, `man` and `defaults` resources. It also converts an author-folder `settings.toml` into packaged `defaults/settings.toml`. The runtime default overlay reads that packaged location.

Combine all unpublished assets for one version with `ReleaseCatalogueBuilder.Compose`. Add the completed release to existing history with `ReleaseCatalogueBuilder.Publish`. Compose the full asset list before first publication. Adding an asset to an already published version changes its immutable release identity.

## Publish in a safe order

1. Build every declared target.
2. Execute checks on the supported targets where available.
3. Pack each asset.
4. Compose the catalogue fragments.
5. Review dependencies and compatibility fields.
6. Upload the immutable archives and checksums.
7. Check each uploaded archive against its checksum.
8. Publish the catalogue entry.
9. Test installation from the published metadata.

Record whether a target only built or also executed. A Linux cross-build does not prove Windows native loading works.

## Work through a dependency conflict

Suppose `map-tools` requires `shared-tools = "^2.0.0"`. Another installed plugin requires `shared-tools = "^3.0.0"`. One active version must satisfy both consumers.

The resolver rejects this graph before downloading or unloading. Installing both versions in separate immutable directories does not permit both to be active under one plugin identity. Choose compatible consumer releases or remove the conflicting consumer deliberately.

Optional dependencies behave differently. They do not trigger installation. A missing or incompatible optional provider remains unavailable. The consumer must support that condition in its code.

## Recovery and failure handling

Keep the lock, transaction history and immutable package directories together. Copying only DLLs loses the installation graph and rollback history.

Cancellation before commitment preserves the previous selection. An activation failure triggers restoration of the previous graph. Source compilation happens before commitment, so a compiler error cannot become the active package.

A previous package graph cannot reverse server commands, network requests or plugin-written application data. A plugin must design its own durable data migration when its storage format changes.

| Error situation | Next action |
| --- | --- |
| Hash mismatch | Obtain the correct immutable archive and catalogue digest |
| No matching target | Select a supported asset or explicitly permit source fallback |
| Missing capability | Attach the required module or select another release |
| Pinned provider blocks an update | Review the pin and dependent ranges before changing policy |
| Stale plan | Create a new plan and display its changes again |
| Invalid archive path or link | Correct the publisher's package rather than disabling validation |
| Source compiler error | Check the entry source and explicit compilation references |
| Activation failure | Inspect the individual plugin status and transaction result |

## Run a complete local installation check

The [MarketplaceGuide sample](https://github.com/MCCTeam/DMCBK/blob/master/samples/MarketplaceGuide/README.md) packs a plugin, plans an exact version, downloads one archive through a local HTTP handler and runs a real plugin callback. It also checks the lock hash, pinning and uninstall.

It needs no account, external publisher or Minecraft server. Its in-memory protocol session checks the modeled lifecycle. Use a live private server for additional game behavior.


Adapted from the [DMCBK marketplace specification](https://github.com/MCCTeam/DMCBK/blob/master/docs/marketplace-v2.md). [Use marketplaces in MCC](using.md) or [create a marketplace](creating.md).
