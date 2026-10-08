# File input and unattended operation

Use Beacon for event-driven behavior. Use file input when another process needs to supply the same commands a person would type.

## Prepare the account

1. Start MCC interactively with your persistent configuration folder.
2. Complete account authentication.
3. Save the server.
4. Stop MCC with `/exit`.

A first-run login prompt cannot work with file input or closed stdin. Online authentication can require another interactive login after a token expires.

## Start file input on Linux or macOS

```bash
mkdir -p "$HOME/mcc-data"
touch "$HOME/mcc-data/input.txt"
MCC_FILE_INPUT=1 MCC_INPUT_FILE="$HOME/mcc-data/input.txt" \
  ./Mcc.Cli --configurations "$HOME/mcc-data/configurations"
```

Use an absolute input path. Keep this process running. Open a second terminal to append commands:

```bash
printf '/health\n' >> "$HOME/mcc-data/input.txt"
printf '/list\n' >> "$HOME/mcc-data/input.txt"
printf '/exit\n' >> "$HOME/mcc-data/input.txt"
```

Each line needs a final newline. MCC waits for a complete line before executing it.

## Start file input in PowerShell

```powershell
New-Item -ItemType Directory -Force (Join-Path $HOME "mcc-data") | Out-Null
$env:MCC_FILE_INPUT = "1"
$env:MCC_INPUT_FILE = Join-Path $HOME "mcc-data/input.txt"
New-Item -ItemType File -Force $env:MCC_INPUT_FILE | Out-Null
.\Mcc.Cli.exe --configurations (Join-Path $HOME "mcc-data/configurations")
```

In a second PowerShell window:

```powershell
Add-Content -Path (Join-Path $HOME "mcc-data/input.txt") -Value "/health"
Add-Content -Path (Join-Path $HOME "mcc-data/input.txt") -Value "/exit"
```

## Understand file consumption

MCC polls the file about every 150 milliseconds. It consumes existing complete lines from the beginning when the driver starts. It then processes appended complete lines once during that process.

Do not truncate or replace the input file during a running session. The driver tracks a line count. Start with a new or deliberately prepared file for each run.

An exact bare `quit` or `exit` line stops the file driver. This special behavior belongs to file input. In the default interactive slash mode, use `/quit` or `/exit`.

`/exit <code>` routes through the normal command dispatcher and controls the process exit code. A remote disconnect also ends a file-input run.

## Environment variables

| Variable | Use |
| --- | --- |
| `MCC_FILE_INPUT=1` | Enable file input |
| `MCC_INPUT_FILE` | Input file, default `mcc_input.txt` relative to the working directory |
| `MCC_PLUGINS` | Select the plugin root |
| `MCC_CLI_FAULT_DETAIL=1` | Include deeper connection-fault details |
| `MCC_RUN_ROOT` | Runtime working directory used by development helpers |
| `MCC_BUILD_MODE=tmpfs` | Route helper build output to temporary storage |

The last two belong to repository helpers. They are not runtime configuration keys inside `client.toml`.

## Run a game smoke exercise

```bash
./Mcc.Cli GuideBot - localhost --exercise smoke
```

This connects to the selected private offline test server, runs representative checks, and exits. The exercise is a diagnostic operation. It can issue game actions and requires a suitable test world.

Do not use it as a harmless public-server health probe. For script syntax alone, use the offline Beacon tools.

## Containers

The [Docker Compose guide](../deployment/docker.md) explains persistent volumes, first-run sign-in, attached input, and file-driven containers. Keep account tokens and plugin user data on persistent storage.
