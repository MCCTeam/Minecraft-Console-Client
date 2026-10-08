# Documentation review and checks

The MCC 2.0 documentation received a council review after its first draft. Three independent agents used six reader perspectives.

These were simulated perspectives, not interviews with real readers. Ages identify the requested review viewpoints. They do not support demographic conclusions.

## Reader perspectives

| Perspective | Main questions |
| --- | --- |
| First-time terminal user, age 15 | Where do I type this command? What should happen next? |
| English as a second language, age 35 | Are terms defined? Does each instruction have one clear action? |
| Plugin developer, age 30 | Do contracts, dependencies, compilation and lifecycle examples agree with the source? |
| Retired server administrator and beginner programmer, age 65 | Are prerequisites, paths, saved data and permissions explicit? |
| Technical writer and translator, age 45 | Can the page be translated without changing commands or placeholders? |
| Experienced server operator, age 45 | Are connection, authentication and destructive-action instructions reproducible? |

## Changes from the review

- Added missing DMCBK checkout steps for optional plugin verification.
- Added `/` prefixes to interactive plugin examples and explained when bare input becomes server chat.
- Separated custom-host C# setup from normal MCC plugin loading.
- Corrected PowerShell executable paths and input-file directory creation.
- Corrected the timing of Microsoft authentication and the singular `realm` configuration value.
- Added platform-specific checksum commands and a beginner glossary.
- Fixed command tables that lost syntax at pipe characters.
- Corrected Beacon one-shot command names and installer launcher argument order.
- Added translated fallback routes and downloadable example files for each active locale.
- Added release-build prerequisites and Docker permission diagnostics.
- Removed an unsupported categorical claim about Discord direct messages.

## Validation evidence

| Check | Result |
| --- | --- |
| CLI test suite | All 910 tests pass on Linux and the Windows VM, including translated resources and English fallback. |
| Command reference | All 47 active commands have pages. 133 library and host examples parse without execution. |
| Configuration examples | 22 portable TOML fragments load without warnings. |
| Beacon examples | 24 `.bcn` files and 50 fenced examples lint without errors. Eleven representative files execute offline. |
| Plugin tutorial | Source counter, journal lifecycle, incremental tutorial stages and package generation pass with NuGet packages. |
| Installer tests | Ten offline tests pass. A real Linux publish/package/install rehearsal also passes help, Beacon lint and Beacon run. |
| Windows PowerShell | All 15 native helper checks pass in PowerShell 5.1 with a compiler-DLL wrapper for the VM's CET limitation. Debug build-and-run also passes. |
| PowerShell installer | Native RID detection and local release-fixture installation pass. Checks cover one selected asset, configuration preservation, duplicate versions, checksums, archive paths, and launcher arguments. |
| Docker | Image build, non-root execution, volume writes and packaged translated resources pass. |
| Documentation | Local links, coverage checks and the VuePress build pass. A locale fixture checks English fallback and example downloads. |

Windows x64 and x86 self-contained test executables pass help and offline Beacon execution. These test artifacts use `CETCompat=false` for the old VM. Normal publish settings retain the default CET behavior, which fails at startup on this unpatched Windows installation. No Windows security policy was changed.

Offline checks do not prove server acceptance of game actions. External bridge credentials, official-plugin gameplay, interactive TUI behavior, and native macOS execution need additional testing.

Documentation must follow the installed package version and actual code. Update these checks when those versions change.
