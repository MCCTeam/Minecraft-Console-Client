# Troubleshooting

Start with the first error in the MCC log. Later errors can be consequences of the same failed connection or configuration.

## Check these facts first

1. Read the MCC version from the startup banner.
2. Check the exact server address and port.
3. Check the selected account type.
4. Check the configuration folder passed at startup.
5. Read configuration warnings.
6. Read `/debug state` when the prompt is available.

For a bug report, record the operating system, Minecraft version, interface mode, exact command, expected result, and actual result.

## Connection and account problems

| Symptom | Check | Action |
| --- | --- | --- |
| Connection refused | Server process and port | Start the server or correct the port |
| Timeout | Address, firewall, proxy, and DNS | Check the endpoint from the same machine |
| Version resolution failed | Server ping or proxy response | Select a supported explicit version with `-v` |
| Online login rejected | Account kind and account entitlement | Use Microsoft authentication for an online server |
| First-run prompt unavailable | Redirected input or file input | Complete account setup in an interactive run |
| Whitelist or ban kick | Server's reason | Ask the server operator or use an allowed account |
| Repeated reconnect | Reconnect policy | Disable retry on kick or limit attempts |

An offline account cannot authenticate to a normal online server. A cached Microsoft token is not guaranteed to remain valid forever.

## Configuration and path problems

| Symptom | Explanation | Action |
| --- | --- | --- |
| Different account or server | `Active` selects another entry | Check both saved files |
| Setting appears ignored | Unknown key, fallback, or override | Read warnings and check command-line options |
| Configuration appears elsewhere | Relative paths use the working directory | Use an absolute configuration path |
| Scripts are missing | Scripts are beside the configuration folder | Check `../scripts`, not `configurations/scripts` |
| Console mode did not change | Host settings load at startup | Restart MCC |
| Game feature remains unavailable | Session composition did not change | Enable requirements and reconnect |

The loader can continue with defaults after a TOML error. Correct the error before relying on the result.

## Command and game problems

A server response of "unknown command" can mean MCC did not recognize the local name and forwarded it. Use `/help <command>` to check syntax.

Use `//name` or `/send /name` when a server command shares an MCC name. Use `/exit` to quit in the default interactive slash mode.

For inventory or world errors, check enabled features and received data. Reach restrictions, server permissions, unavailable chunks, and closed containers can all prevent an action.

A successful send is not proof of a server-side change. Inspect the resulting snapshot or server response.

## Terminal problems

For broken colors, set `ConsoleColorMode = "disable"`. For missing symbols, set `Glyphs = "ascii"`. Both keys belong in `console.toml [General]`.

Use classic mode when stdin or output is redirected. Use a real terminal for the TUI. Enlarge a narrow terminal if a view uses separate list/detail navigation.

## Script and plugin problems

For Beacon, lint the file first. Check the required feature and jail capability. Use `/scripts list` and `/scripts run <id> --trace`. Read the [Beacon guide](../beacon/index.md).

For plugins, run:

```text
/plugins doctor
/plugins info example-plugin
/plugins deps example-plugin
/plugins validate
```

Replace `example-plugin` with a discovered plugin ID. Check its state, version constraints, dependencies, asset target, entry file, and settings. An old binary plugin needs rebuilding against the new SDK. Schema-1 marketplaces and manifests are not supported.

Source compilation needs the host's managed reference assemblies. Keep the full MCC distribution together. Read [plugins](../plugins/index.md) and [marketplaces](../marketplaces/index.md) for package-specific errors.

## Gather evidence

[Session diagnostics](diagnostics.md) explains the generated bundle, privacy review, and packet-recording limits. For a deeper fault description, set `MCC_CLI_FAULT_DETAIL=1` for the failing run.

Stop with `/exit` so MCC can finish the bundle. A forcibly killed process can leave an unpacked session folder.
