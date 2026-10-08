# LLM core

Provides model providers, budgets, skills, and remote tools to other plugins.

| Package ID | Plugin release | Manifest capabilities |
| --- | --- | --- |
| `llm-core` | `0.2.0` | `commands` |

The plugin lives in the separate [DMCBK-Plugins repository](https://github.com/MCCTeam/DMCBK-Plugins/tree/main/src/LlmCore). It is not built into MCC. Its declared game/resource usage is `network`. These declarations describe usage, not permission boundaries.

## Install and activate

Use the [plugin management guide](../managing.md) to install a released archive or local package. A source checkout by itself is not a compiled plugin package.

If a publisher already offers this release in a marketplace named `official`, enter:

```text
/plugins install llm-core@official 0.2.0
/plugins enable llm-core
/plugins info llm-core
```

These commands depend on that publisher having uploaded working release assets. The repository catalogue alone does not prove that a downloadable release exists.

1. Check that `/plugins list` shows `llm-core` as loaded.
2. Open `plugins/userdata/llm-core/settings.toml` under your plugin root.
3. Change the settings for your task.
4. Save the file.
5. Enter `/plugins reload llm-core`.

The plugin root normally sits beside the configurations directory. `MCC_PLUGINS` can select another root. Never edit a manifest under `versions/` to enable a plugin.

## First working example

Apply these values to the existing settings file. Keep unrelated values unless the example says otherwise. Replace example IDs, names, and coordinates with your own values.

```toml
Enabled = true
AllowLocalEndpoints = true
DailyTokenBudget = 100000
```

Start a compatible local model server. Use the setup commands below with a model ID that your server actually lists. Check `/llm status` and `/llm doctor`.

```text
/llm setup local http://localhost:11434/v1/ - local/qwen3-8b
/llm model list local
/llm status
/llm doctor
/llm budget 100000
```

## Settings reference

The values below come from the repository bundled settings. The installed package can carry different defaults for another release.

| Setting | Bundled value | Meaning |
| --- | --- | --- |
| `Enabled` | `true` | Set to false to keep the plugin loaded but idle. |
| `DefaultModel` | `""` | Default model as provider/model-id. Empty means callers must name one. |
| `DefaultMaxTokens` | `0` | Cap per answer. Zero means the provider default. |
| `DefaultMaxContext` | `32000` | Context token cap for auto compaction. Zero means 32000. |
| `DailyTokenBudget` | `0` | Global daily token budget. Zero means unlimited. |
| `AllowLocalEndpoints` | `false` | Allow http localhost endpoints, for Ollama and test stubs. |

## Provider, budget, and remote-tool tables

These table entries do not appear in a default file until you configure them. A provider uses a local name and an API endpoint:

```toml
[[Providers]]
Name = "local"
Endpoint = "http://localhost:11434/v1/"
Api = "chat"
Enabled = true
ApiKeyEnv = ""

[[PluginBudgets]]
Plugin = "alerts"
TokensPerDay = 50000

[[McpServers]]
Name = "wiki"
Transport = "http"
Url = "http://localhost:3000/mcp"
Tools = ["search"]
```

This configuration expects running services at the example addresses. `Api` is `responses` or `chat`. Select the format supported by your provider. `Transport` is `http` or `sse`. `Tools` lists remote tool names that this provider may expose.

`ApiKeyEnv` contains an environment variable name, not its secret value. An empty value is suitable only when the selected local service does not require authentication. Keep `AllowLocalEndpoints = true` among the global settings before these tables when you use HTTP localhost services.

## Requirements and limits

This is a provider service, not a chat bot. Model IDs and endpoint support vary by provider. Remote calls can send chat or game context and consume a paid allowance. API keys belong in environment variables. A token budget limits usage, not currency.

## Detailed operation

### Providers

A provider is one AI address. Localhost, a cloud account, a friend's
server. Each holds its own models and its own key.

```
/llm provider list
/llm provider add local http://localhost:11434/v1/
/llm provider edit local keyenv=OLLAMA_KEY
/llm provider enable local
/llm provider disable local
/llm provider status local
/llm provider remove local local
```

Adding probes the address first. A dead address never lands in your
settings. Editing changes the address, the format, or the key
variable. Disabling keeps the entry and skips it. Removing asks you to
repeat the name, so stray keystrokes delete nothing.

Providers live in `settings.toml` under `[[Providers]]` too. Each row
names the address, the format (`responses` or the older `chat`), and
the key variable. Commands are the easy path. The file is the backup.

```toml
[[Providers]]
Name = "local"
Endpoint = "http://localhost:11434/v1"
Api = "responses"
Enabled = true
ApiKeyEnv = ""
```

### Models

Each provider holds models. The core asks the address for the list and
caches it for a day. Some servers hide models from the list. Pin those
by hand.

```
/llm model list local
/llm model refresh local
/llm model add local my-custom-model
/llm model remove local my-custom-model
/llm cache clear local
```

Listing reads the cache. Refresh asks the server again. Adding pins an
id the server forgot to mention. The cache separates discovered IDs from pinned IDs. Clearing the cache preserves pinned entries.

Plugins name models per task as `provider/model-id`. Choose a model for each task. Empty means the core default.

```
/llm use local/qwen3-8b
```

### Token usage

AI calls consume tokens and can cost money. Two commands report and limit token use.
`/llm status` shows providers, the default model, and spend so far.
`/llm budget` shows and sets ceilings.

```
/llm budget
/llm budget 1000000
/llm budget alerts 50000
```

The first form only looks. The second caps the whole core per day.
The third caps one plugin per day. Zero means no cap. Calls fail when the configured token budget is exhausted.

`/llm why` explains the last answer. Which model ran. Which tools and
skills took part. How many tokens went each way. Paste its output when
you ask for help with a weird answer.

### Health

`/llm doctor` probes everything and prints fixes you can paste. Dead
addresses. Missing keys. Empty model lists. Run it first when anything
AI stops working. It answers most questions before you ask them.

```
/llm doctor
```

### Skills

Skills are folders of know-how the model can pull while it works. Each
holds a `SKILL.md` file with plain instructions, plus extra files it
links to. The core serves four read-only tools for them. List them.
Read one. Search them. Read one file. Nothing ever writes or runs.

```
/llm skill list
```

Plugins register their own skill folders through the core. Skill texts
load only when needed, so idle skills cost nothing.

### MCP servers

MCP servers lend the model outside tools over the network. A wiki
search. A calculator. Anything with an MCP address. Remote tools show
up named like `wiki_search` and run only when a call names them.

```
/llm mcp list
/llm mcp check wiki
/llm mcp add wiki http://localhost:3000/mcp search
/llm mcp remove wiki wiki
```

Adding connects first and verifies the tool list. A server that fails
the check never lands in your settings. Removing asks twice like
providers do. Shared servers live under `[[McpServers]]` in
settings with their address and tool list.

### Settings file

Almost everything above writes `settings.toml` for you. Read it to
learn. Edit it by hand when commands feel slow. The knobs that matter:

- `DefaultModel` is the model used when a call names none.
- `DefaultMaxTokens` caps answer length. Zero means the server decides.
- `DefaultMaxContext` caps context size for long talks.
- `DailyTokenBudget` caps the whole core per day. Zero means no cap.
- `AllowLocalEndpoints` must be true for `http://localhost` addresses.
- `[[Providers]]` rows mirror `/llm provider add`.
- `[[McpServers]]` tables mirror `/llm mcp add`.

### When it breaks

Start with `/llm doctor`. Then check the usual suspects.

No providers listed means none configured. Run `/llm setup` again.
`401` or `auth` errors mean the key variable is empty or wrong. Check that the named variable exists and contains the correct key. Do not print the key into terminal logs. Model-not-found means the id is
misspelled or belongs to another provider. `/llm model list local` shows the
true names. Localhost refused means the server is down or
`AllowLocalEndpoints` is false. Timeouts mean an overloaded server or
a dead address. Point a browser or curl at the address to tell which.

## When it does not work

1. Enter `/plugins info llm-core`.
2. Read any activation error.
3. Enter `/plugins doctor`.
4. Check the required game features and external service configuration.
5. Check `Enabled` in the user settings.
6. Reload only this plugin.

If its command is unknown, the plugin is usually not loaded. A loaded plugin can still have its behavior disabled in settings. These are separate states.

To stop the plugin, enter `/plugins disable llm-core`. To remove its package while retaining user data, enter `/plugins uninstall llm-core`. Add `purge` only when you intend to delete its settings and data too.

[All official plugins](../official.md) · [Plugin management](../managing.md) · [Make a plugin](../development/index.md)
