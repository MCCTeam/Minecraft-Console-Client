# MCP server

Exposes session queries and controlled game actions over a local Model Context Protocol HTTP endpoint.

| Package ID | Plugin release | Manifest capabilities |
| --- | --- | --- |
| `mcp-server` | `2.0.0` | `commands`, `aspnetcore` |

The plugin lives in the separate [DMCBK-Plugins repository](https://github.com/MCCTeam/DMCBK-Plugins/tree/main/src/McpServer). It is not built into MCC. Its declared game/resource usage is `chat`, `commands`, `network`. These declarations describe usage, not permission boundaries.

## Install and activate

Use the [plugin management guide](../managing.md) to install a released archive or local package. A source checkout by itself is not a compiled plugin package.

If a publisher already offers this release in a marketplace named `official`, enter:

```text
/plugins install mcp-server@official 2.0.0
/plugins enable mcp-server
/plugins info mcp-server
```

These commands depend on that publisher having uploaded working release assets. The repository catalogue alone does not prove that a downloadable release exists.

1. Check that `/plugins list` shows `mcp-server` as loaded.
2. Open `plugins/userdata/mcp-server/settings.toml` under your plugin root.
3. Change the settings for your task.
4. Save the file.
5. Enter `/plugins reload mcp-server`.

The plugin root normally sits beside the configurations directory. `MCC_PLUGINS` can select another root. Never edit a manifest under `versions/` to enable a plugin.

## First working example

Apply these values to the existing settings file. Keep unrelated values unless the example says otherwise. Replace example IDs, names, and coordinates with your own values.

```toml
Enabled = true
BindHost = "127.0.0.1"
Port = 33333
Route = "/mcp"
RequireAuthToken = true
AuthTokenEnvVar = "MCC_MCP_AUTH_TOKEN"
AllowNonLoopback = false
```

Set a private random token in `MCC_MCP_AUTH_TOKEN`. Reload the plugin. Run `/mcp` to inspect the endpoint. Connect an MCP client that sends an Authorization Bearer header.

```text
/mcp
```

Create an environment variable named `MCC_MCP_AUTH_TOKEN`. Set its value to a private token with at least 32 characters. MCC also reads a `.env` beside its configuration TOML files.

The default URL is `http://127.0.0.1:33333/mcp`. Configure the MCP client with that URL and an `Authorization: Bearer <your-token>` header. Do not place the token in a public client configuration or this documentation. The exact client configuration format depends on the MCP client.

The settings use the C# property names shown above. The old manual descriptions of `bind-host` and capability tables do not describe this current settings type.

## Settings reference

This plugin generates settings from `McpServerSettings`. Use the first example for listener settings. `CapSessionStatus`, `CapChatAndCommands`, `CapMovement`, `CapInventory`, and `CapEntityWorld` default to `true`. Disable capabilities that the operator does not need.

## Requirements and limits

Needs the `aspnetcore` host capability, which MCC composes. Its tools can act as your player. Keep authentication enabled, even on loopback. Inspect every tool result success field. Sending a game action does not prove that the server accepted it.

## When it does not work

1. Enter `/plugins info mcp-server`.
2. Read any activation error.
3. Enter `/plugins doctor`.
4. Check the required game features and external service configuration.
5. Check `Enabled` in the user settings.
6. Reload only this plugin.

If its command is unknown, the plugin is usually not loaded. A loaded plugin can still have its behavior disabled in settings. These are separate states.

To stop the plugin, enter `/plugins disable mcp-server`. To remove its package while retaining user data, enter `/plugins uninstall mcp-server`. Add `purge` only when you intend to delete its settings and data too.

[All official plugins](../official.md) · [Plugin management](../managing.md) · [Make a plugin](../development/index.md)
