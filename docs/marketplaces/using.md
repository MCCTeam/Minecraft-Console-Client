# Use a marketplace in MCC

Start with a publisher you trust. A plugin runs with ordinary access to the MCC process, files, and network. A marketplace checksum checks that an archive matches the publisher metadata. It does not prove that the plugin code is safe.

## Use the official marketplace

MCC registers the [MCC Marketplace](https://github.com/MCCTeam/Marketplace) as `official` when your configuration has no marketplace registry. Enter:

```text
/plugins search fishing in official
/plugins install auto-fishing@official
/plugins info auto-fishing
```

Read the [plugin manual](../plugins/official/auto-fishing.md) before enabling its behavior. Automatic updates start off, and startup does not download or install plugins. Use `/plugins marketplace refresh official` to fetch current metadata. If you removed `official`, MCC preserves that choice. Existing custom or empty registries stay as you configured them.

To add it again, enter:

```text
/plugins marketplace add https://raw.githubusercontent.com/MCCTeam/Marketplace/master/marketplace/mcc-marketplace.toml as official
```

## Add another publisher

1. Obtain the publisher schema-2 index URL.
2. Enter `/plugins marketplace add <index-url> as community`.
3. Enter `/plugins marketplace list`.
4. Check that `community` appears.
5. Enter `/plugins search fishing in community`.
6. Read the compatibility details for the result.

Replace `<index-url>` with the real URL. A local folder containing `mcc-marketplace.toml` also works. Repository imports must expose a valid index where the marketplace importer expects it. Prefer a direct index URL when a repository stores the index in a subdirectory.

The official repository stores its index at `marketplace/mcc-marketplace.toml`. Use the direct index URL shown above.

## Install a particular release

```text
/plugins install auto-fishing@community 2.0.0
/plugins info auto-fishing
/plugins enable auto-fishing
```

This is an example for a publisher that actually offers AutoFishing 2.0.0. The `@community` suffix binds the package to that marketplace. It avoids silently choosing another publisher that uses the same plugin ID.

Omit the version to ask for an ordinary compatible selection. Enter an exact version to require that version. Quote a range that contains spaces:

```text
/plugins install example-plugin@community ">=2.0.0 <3.0.0"
```

The resolver may keep an installed version that already satisfies the request. An update request prefers a newer compatible selection. Pins and installed dependent ranges still apply.

## Select source or compiled code

| Request | Selection |
| --- | --- |
| No flag | Exact compiled target, then compiled `any` |
| Source-only release | Suitable source asset without an extra flag |
| `--source` | Require a suitable source asset |
| `--source-fallback` | Permit source after compiled selection fails in a mixed release |
| `--prerelease` | Permit prereleases under the shared range rules |

```text
/plugins install example-plugin@community 2.0.0 --source
/plugins install example-plugin@community 2.0.0 --source-fallback
```

A mixed release contains source and compiled assets for one plugin version. A source entry is one C# file. MCC compiles it with its explicit SDK references. It does not restore arbitrary NuGet dependencies or build a project file.

Windows selects `win-x86`, `win-x64`, or `win-arm64` from the running process. Linux also distinguishes glibc and musl. macOS selects `osx-x64` or `osx-arm64`. See the [target table](format.md#asset-selection) for all supported identifiers.

The installer downloads one archive per changed plugin. Required plugin dependencies can add more archives. It does not download every historical release or every architecture.

## Understand dependency changes

Required dependencies install transitively. Optional dependencies do not install automatically. A missing or incompatible optional provider remains unavailable.

One plugin ID has one active version. Two consumers that require incompatible provider versions cause resolution to fail before downloading or unloading anything. Add compatible consumer releases or remove the conflicting consumer deliberately.

Cross-market dependencies need an explicit existing source binding. MCC does not silently search unrelated publishers or change publisher identity.

## Control automatic updates

```text
/plugins marketplace auto-update community off
/plugins marketplace auto-update community check
/plugins marketplace auto-update community apply
```

| Marketplace policy | Behavior |
| --- | --- |
| `off` | Do not schedule automatic checks or application |
| `check` | Check and report available compatible updates |
| `apply` | Permit automatic application after the live session ends |

Pins block version changes. Per-plugin policy can override inherited behavior. The installation lock records that policy. The CLI exposes marketplace automatic policy and pins. Do not invent an unimplemented per-plugin policy command.

Automatic checks use cached metadata and a short randomized startup delay. Manual refresh fetches metadata explicitly:

```text
/plugins marketplace refresh community
/plugins outdated
/plugins update all
```

Review a manual plan before accepting it. A direct application API does not defer itself for a live session. MCC automatic update scheduling handles that deferral.

## Recover from a failed operation

The installer stages files, verifies hashes and manifests, and validates or compiles source before it changes active selection. It does not overwrite loaded DLLs.

An activation failure restores the previous package graph. Startup recovers interrupted transactions before loading plugins. Settings and data remain outside immutable packages.

Use `/plugins rollback <transaction>` for a cached prior graph. Preserve the lock, transaction history, and immutable version directories together. Rollback does not undo chat, server commands, or plugin data migrations.

| Failure | Action |
| --- | --- |
| Asset returns 404 | Ask the publisher to upload or correct the release asset |
| Digest differs | Obtain the correct immutable asset and catalogue digest |
| No supported target | Select a supported release or explicitly permit source |
| Dependency conflict | Check `/plugins deps <id>` and the declared ranges |
| Pinned version blocks changes | Review the pin before removing it |
| Source compilation fails | Correct the entry code or required assembly references |
| Activation fails | Read individual plugin status and logs |

Remove a marketplace with `/plugins marketplace remove community`. Its installed plugins remain. Their publisher binding still matters for future updates, so restore the same source when needed.

[Manage plugins](../plugins/managing.md) · [Create a marketplace](creating.md) · [Schema and API reference](format.md)
