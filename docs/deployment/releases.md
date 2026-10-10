# Create a release package

The MCC installers need a complete archive. A single executable does not contain the managed assemblies required by source plugins.

This page describes local packaging and the early-access release workflow. Obtain the owner's authorization before publishing.

You need the pinned [.NET SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), [Python](https://www.python.org/downloads/) 3.10 or later, and [Node.js with npm](https://nodejs.org/en/download).

## Build one target

1. Open the repository terminal.
2. Initialize ConsoleInteractive.
3. Load the build helpers.
4. Publish your target into a new output folder.

```bash
git submodule update --init ConsoleInteractive
source tools/mcc-env.sh
mcc-publish --rid linux-x64 -- -o /tmp/mcc-publish/linux-x64
```

Use a fresh output folder for each target. Do not place runtime configuration, tokens, scripts or installed plugin data in it.

## Create an archive

```bash
python3 tools/package-release.py /tmp/mcc-publish/linux-x64 --tag v2.0.0-preview.1 --rid linux-x64 --output artifacts/releases
```

The tool verifies the executable, managed assembly, dependency manifest and runtime configuration. It keeps every published file in the archive.

Release assets use these names:

```text
Mcc-v2.0.0-preview.1-linux-x64.tar.gz
Mcc-v2.0.0-preview.1-osx-arm64.tar.gz
Mcc-v2.0.0-preview.1-win-x64.zip
SHA256SUMS
```

Archive files sit at the archive root. They do not use an additional parent folder. Unix archives contain `Mcc.Cli`; Windows archives contain `Mcc.Cli.exe`.

The checksum file covers each archive in the output folder. The tool refuses to replace an existing archive. A published payload must remain immutable.

## Target identifiers

| Platform | RIDs |
| --- | --- |
| Windows | `win-x86`, `win-x64`, `win-arm64` |
| Linux with glibc | `linux-x64`, `linux-arm64`, `linux-arm` |
| Linux with musl | `linux-musl-x64`, `linux-musl-arm64`, `linux-musl-arm` |
| macOS | `osx-x64`, `osx-arm64` |

Publish only targets that you can validate. Cross-building does not prove that a binary runs on its target system.

## Publish an early-access build through CI

The `build-and-release.yml` workflow on `feat/mcc-2.0` builds all 11 targets in the table. It runs the CLI tests and extracted-archive startup checks on seven native targets. Linux ARM32 and the musl targets receive cross-build checks. The workflow keeps complete publish directories, validates every archive checksum and publishes a prerelease only after all build and documentation jobs pass.

Add release notes at `docs/deployment/early-access-<number>.md`. Update the expected CLI test count in the workflow when tests are intentionally added or removed. After publication is authorized, dispatch the registered workflow with the branch and build number:

```sh
gh workflow run build-and-release.yml --ref feat/mcc-2.0 -f build_number=12
```

Build 12 uses tag `v2.0.0-early-access.12` and title `MCC 2.0 Early Access Build 12`. The executable records the build number and source commit in its version metadata. The default installers select stable releases, so specify the preview tag when installing this build.

The publisher creates an annotated tag for the validated commit, stages a draft release, verifies the uploaded asset digests and then makes the prerelease public. It does not replace existing assets or move an existing tag. If publication fails, rerun the failed publication job to reuse its original build artifacts. Changed source or release bytes require a new build number.

## Check before publication

1. Run `mcc-test`.
2. Run `python3 tools/test_installers.py`.
3. Run `python3 tools/check_docs.py`.
4. Install site dependencies with `npm --prefix docs ci`.
5. Build the documentation with `npm --prefix docs run docs:build`.
6. Extract each archive in an empty directory.
7. Run its help command on the target platform.
8. Check classic and TUI startup on supported terminals.
9. Verify that translated resource assemblies remain in their culture directories.

After the owner approves publication, upload the archives and their shared `SHA256SUMS` file to the matching GitHub release tag.

The default installers select the latest stable GitHub release. Users need an explicit version for a preview tag. The new installer refuses old MCC 1.x releases.
