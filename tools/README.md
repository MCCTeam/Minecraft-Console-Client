# MCC 2.0 tools

| Tool | Purpose |
| --- | --- |
| `mcc-env.sh` | Build, test, publish and run the current CLI. |
| [windows/](windows/README.md) | Native PowerShell build, test, run, publish and clean helpers. |
| `localization/generate_strings.py` | Generate typed MCC resource accessors. |
| `localization/prepare_docs.py` | Prepare translated routes and downloadable examples. |
| `check_docs.py` | Check local documentation links and coverage. |
| `install_mcc.py` | Canonical Unix installer implementation. |
| `test_installers.py` | Offline platform, checksum and archive tests. |
| `package-release.py` | Create complete release archives and checksums. |
| `validate-marketplace.py` | Run the CLI marketplace validator. |
| `umpkcap.py` | Inspect packet captures produced by the host. |
| `start-server.sh`, `mc-rcon.sh` | Optional local Minecraft server helpers. |

See the main README for build prerequisites. Runtime files belong outside the source checkout. Do not use retired MCC 1.x launch or palette-generation tools.

The public `install.sh` contains a copy of `install_mcc.py`. Keep them synchronized. `test_installers.py` checks that they match.
