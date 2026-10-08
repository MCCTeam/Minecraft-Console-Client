# Docker Compose

Docker runs MCC in an isolated Linux environment. The supplied Compose file builds your local checkout. It does not download an old MCC image.

The image includes the ASP.NET shared framework because some plugins need it. MCC itself is still a terminal application.

## Before you start

You need [Docker Engine](https://docs.docker.com/engine/install/) or [Docker Desktop](https://docs.docker.com/desktop/) with [Docker Compose](https://docs.docker.com/compose/install/). You also need [Git](https://git-scm.com/downloads/) and an initialized ConsoleInteractive submodule.

Use a server that permits automated clients. You do not need to run a Minecraft server inside this Compose project.

## Build the image

1. Open a terminal in the MCC repository.
2. Initialize the console dependency.

   ```bash
   git submodule update --init ConsoleInteractive
   ```

3. Check the Compose configuration.

   ```bash
   docker compose config --quiet
   ```

4. Build the image.

   ```bash
   docker compose build mcc
   ```

The build restores DMCBK and UMPK from NuGet. It does not need either source repository. The final image runs as the non-root `app` user.

## Open an interactive client

```bash
docker compose run --rm mcc
```

This command creates a temporary container with terminal input. Its named volume remains after the container exits. Follow the account prompts as described in [your first session](../getting-started/first-session.md).

For a help screen without a connection:

```bash
docker compose run --rm mcc --help-short
```

Container arguments replace the Compose `command`. Include `--configurations /data/configurations` when you supply custom connection arguments.

```bash
docker compose run --rm mcc --configurations /data/configurations TestClient - host.docker.internal:25565
```

On Docker Desktop, `host.docker.internal` normally identifies your computer. On Linux Engine, add this service option if needed:

```yaml
extra_hosts:
  - "host.docker.internal:host-gateway"
```

`localhost` inside a container identifies that container. It does not identify your computer or another service.

## Persistent files

The `mcc-data` volume mounts at `/data`. Its layout is:

```text
/data/
├── configurations/   # Profiles, settings, token cache and logs
├── plugins/          # Installed packages, locks and plugin userdata
└── scripts/          # Your .bcn files
```

`MCC_PLUGINS=/data/plugins` selects the plugin root. The CLI receives `--configurations /data/configurations`. Relative script paths use `/data` as their working directory.

Keep this volume when you rebuild the image. A normal `docker compose down` preserves named volumes. `docker compose down --volumes` removes them, including accounts and plugin data.

To copy a script from Bash, Zsh or another POSIX shell into the volume:

```bash
docker compose run --rm --entrypoint sh -v "$PWD/my-script.bcn:/tmp/example.bcn:ro" mcc -c 'cp /tmp/example.bcn /data/scripts/example.bcn'
```

From PowerShell, use this equivalent command:

```powershell
docker compose run --rm --entrypoint sh -v "${PWD}/my-script.bcn:/tmp/example.bcn:ro" mcc -c "cp /tmp/example.bcn /data/scripts/example.bcn"
```

Then enter this command in MCC:

```text
/scripts run /data/scripts/example.bcn
```

## Keep a configured client running

Prepare a working account and server profile during an interactive run. Then start the service:

```bash
docker compose up -d mcc
docker compose logs --tail 100 mcc
docker compose attach mcc
```

To detach without stopping the client, press **Ctrl+P**, then **Ctrl+Q**. To stop the service, run `docker compose stop mcc`.

The default Compose file does not restart MCC automatically. Add `restart: unless-stopped` after you check its configuration and authentication work.

## TUI and automated input

To open the TUI:

```bash
docker compose run --rm mcc --configurations /data/configurations --console.General.ConsoleMode=tui
```

Use a real terminal. Some terminal features depend on font, mouse and clipboard support. The classic interface is usually easier for unattended operation.

File input uses these service settings:

```yaml
environment:
  MCC_PLUGINS: /data/plugins
  MCC_FILE_INPUT: "1"
  MCC_INPUT_FILE: /data/commands.txt
```

Create `/data/commands.txt` before starting MCC. Write one command per line. See [automated input](../client/automation.md) for how MCC handles new lines.

To check the container user before changing bind-mount permissions:

```bash
docker compose run --rm --entrypoint id mcc
```

The output lists the numeric user and group IDs. Grant those IDs access only to the directory you intend to share.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| Build says ConsoleInteractive is missing | Initialize the submodule before the image build. |
| Cannot connect to `localhost` | Use the external server address or the host gateway. |
| Login needs a code | Complete one interactive login before unattended operation. |
| Permission denied under `/data` | A bind mount must be writable by the image's `app` user. The image initializes the default named volume with suitable ownership. |
| Script not found | Use `/data/scripts/name.bcn` or a path relative to `/data`. |
| Plugin changes disappear | Check that a persistent volume mounts at `/data`. |

The Compose terminal settings follow the [Docker service reference](https://docs.docker.com/reference/compose-file/services/).
