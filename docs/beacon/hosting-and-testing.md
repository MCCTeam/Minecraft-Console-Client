# MCC operations and testing

MCC runs one Beacon runtime per client. All scripts in that runtime share a scheduler, shared values, and chat allowance.

## Select the correct data directory

`--configurations /data/configurations` selects the configuration directory. Beacon files then belong in `/data/scripts`.

```text
mcc-data/
├── configurations/
│   ├── beacon.toml
│   └── beacon/
│       ├── welcome.settings.toml
│       └── welcome.toml
└── scripts/
    ├── welcome.bcn
    ├── lib/
    │   └── math.bcn
    └── data/
        └── notes.txt
```

| Path | Purpose | Owner |
| --- | --- | --- |
| `scripts/*.bcn` | Source discovered by MCC. | Script author |
| `scripts/lib/*.bcn` | Imported helper libraries. | Script author |
| `configurations/beacon.toml` | Network allowlist. | MCC user |
| `configurations/beacon/<id>.settings.toml` | Editable setting overrides. | MCC user |
| `configurations/beacon/<id>.toml` | Saved runtime values. | Beacon script |
| `scripts/data/` | Restricted file area shared by scripts. | Beacon scripts |

Do not store account passwords or tokens in these files. Use MCC's authentication configuration for login.

## Operate scripts in classic mode

1. Run `/scripts list` to inspect running IDs.
2. Run `/scripts lint <id>` before loading a new file.
3. Run `/scripts run <id>` to load it.
4. Check local output and diagnostics.
5. Run `/scripts stop <id>` when its work is complete.

[The command reference](commands.md) documents every option and template.

## Operate scripts in TUI mode

Run `/scripts ui` to open the script manager. The manager can inspect source, create files, run scripts, edit settings, and show diagnostics.

The same DMCBK runtime handles classic and TUI operations. A UI change does not give a script additional server permissions.

Keep a copy of important source before editing. Reload resets globals. Resetting or removing saved files can remove progress.

## Choose a test level

| Level | Tool | What it proves | What it does not prove |
| --- | --- | --- | --- |
| Syntax | `Mcc.Cli lint` | Source and declarations pass static checks. | Handler results and server behavior. |
| Logic | `Mcc.Cli run` | Top-level calculations and due timers execute offline. | Minecraft events or packet delivery. |
| Simulated event | DMCBK test host | A chosen event produces the expected requested action. | The server accepts the action. |
| Live session | MCC on your controlled server | Host integration and observed server behavior. | Every server configuration or plugin combination. |

A successful load registers future work. It does not execute every handler, command, or timer.

## Keep a repeatable check

1. Read the file you actually distribute.
2. Use a full path for diagnostics and relative imports.
3. Use a fixed random seed for offline logic.
4. Check the command's exit code.
5. Check the expected output.
6. Test each event with a matching and unrelated record.
7. Check unavailable prerequisites separately.

An assertion makes a result explicit:

```beacon
# beacon 1
set total to 3 * 4
assert(total is 12, "order total")
show "Order checks passed"
```

The output is `Order checks passed`. A failed assertion produces a diagnostic and a failed run.

## Diagnose a failure

| Symptom | Possible cause | First check |
| --- | --- | --- |
| File not found | Wrong scripts root, path, or extension. | Selected configuration path and `.bcn` filename. |
| Header error | Missing or unsupported `# beacon 1`. | First line of the actual file. |
| Unknown function | Misspelling, missing import, or absent plugin export. | Function name and loaded provider. |
| Capability error | Undeclared or unavailable operation. | `# needs:`, tracking, and provider availability. |
| No handler output | Event did not match or did not arrive. | Script list, exact filter, and live chat parsing. |
| Missing world value | The area is not tracked or loaded. | Terrain tracking and nearby observations. |
| Lost count after rename | The filename changed the script ID. | Original ID and state file. |
| Network refused | Destination absent from allowlist. | `configurations/beacon.toml` host entry. |
| Formatting check fails | MCC would normalize the source. | Printed diff, then normal `format`. |

Correct syntax errors before investigating runtime failures. Check the first diagnostic before later diagnostics.

## Advanced automated tests

Plugin and application authors can use `DMCBK.Testing`. Its `ScriptTestHost` records requested messages and supplies controlled game observations.

DMCBK's [Beacon test examples](https://github.com/MCCTeam/DMCBK/blob/master/docs/beacon/guide/08-testing.md) include simulated events and a virtual clock.

These tests need a .NET project. Ordinary MCC script users do not need that project to follow this guide.

See [Chapter 8](guide/08-testing.md) for MCC checks and [recipes](recipes.md) for complete automation files.
