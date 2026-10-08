# Accounts and authentication

An account tells MCC how to authenticate. A server entry tells MCC where to connect. These are separate choices.

## Choose the account type

| `Kind` | Use | Sign-in |
| --- | --- | --- |
| `offline` | Private servers that allow unauthenticated players | A player name, without a password |
| `microsoft` | Normal Minecraft Java accounts | Microsoft device code in a browser |
| `microsoft-browser` | Alternative Microsoft sign-in flow | Browser sign-in, then paste the displayed authorization code |
| `yggdrasil` | A server's custom authentication provider | Provider URL and interactive credentials |

An offline account does not bypass a normal server's online authentication. Server plugins can add another login step after connection. That step is separate from Minecraft account authentication.

## First-run Microsoft sign-in

1. Start MCC in an interactive terminal.
2. Select Microsoft in the account prompt.
3. Run `/connect <server>` if MCC opens an idle prompt.
4. Open the URL that MCC shows.
5. Enter the device code on the Microsoft page.
6. Complete authentication in the browser.
7. Return to MCC.

In the TUI, the sign-in dialog can open the URL and copy the code. MCC saves the resolved account after successful sign-in. Later runs can use cached tokens.

## Configure accounts.toml

This complete example selects a private-test account:

```toml
Active = "local"
SessionCache = "disk"
ProfileKeyCache = "disk"
CacheDirectory = "cache"

[[Account]]
Name = "local"
Kind = "offline"
Login = "GuideBot"

[[Account]]
Name = "main"
Kind = "microsoft"
Login = "MyPlayerName"
```

`Name` is a local label. `Login` is the login hint or player name. `Active` selects an entry by `Name`. If `Active` is empty, MCC uses the first account. Use your own Microsoft profile name for the login hint.

Offline names must contain 1 to 16 characters. The server can impose stricter name rules.

## Switch accounts

Inside MCC:

```text
/reco main
/connect survival main
```

The first command reconnects to the current server as `main`. The second selects both the server and account. The labels must exist in your saved files.

## Select a custom provider

Add an account with your provider URL:

```toml
[[Account]]
Name = "custom"
Kind = "yggdrasil"
Login = "MyPlayerName"
AuthServer = "https://auth.example.net/"
```

Use a provider specified by the server operator. The sample domain is a placeholder. MCC requests the credentials through the interactive authentication interface. Do not add a password field to this account entry.

The command-line equivalent is:

```bash
./Mcc.Cli MyPlayerName - play.example.net --auth yggdrasil --auth-server https://auth.example.net/
```

## Token storage

| Setting | Choices | Purpose |
| --- | --- | --- |
| `SessionCache` | `none`, `memory`, `disk` | Retain an authenticated session |
| `ProfileKeyCache` | `none`, `memory`, `disk` | Retain the chat-signing certificate |
| `CacheDirectory` | Folder path | Cache root, relative to the configuration folder |

Disk is the default for both cache modes. MCC keeps authentication tokens under the selected configuration cache. A token can permit account access. Keep the cache private.

For a shared computer, select `none` or use a separate protected data folder. You may need to sign in again. MCC does not store your typed authentication password in `accounts.toml`.

## Unattended operation

File input and redirected input cannot answer first-run account prompts. Configure the account before starting an unattended client. For online accounts, complete interactive sign-in first.

A cached online session can still expire or require another sign-in. Treat an authentication failure as a need to inspect the account. Do not repeatedly send a password as command-line text.

[Saved servers](servers.md) explains the other half of a connection.
