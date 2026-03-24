---
title: Creating Simple Script
---

# Creating Simple Script

A simple script is a text file with one command per line. See the [Internal Commands](usage.md#internal-commands) section, or type `/help` in the console to see the available commands. Any line beginning with `#` is ignored and treated as a comment.

Application variables defined with the `set` command or in the `[AppVars]` config section can be used. The following read-only variables are also available: `%username%`, `%login%`, `%serverip%`, `%serverport%`, `%datetime%`, `%players%` (`%players%` expands to the current online player names separated by commas, or an empty string when not connected).

## Example

`sample-script.txt`: Send a hello message, wait 60 seconds and disconnect from server.
```
# This is a sample script for Minecraft Console Client
# Any line beginning with "#" is ignored and treated as a comment.

send Hello World! I'm a bot scripted using Minecraft Console Client.
wait 60
send Now quitting. Bye :)
exit
```

See the [example scripts](https://github.com/MCCTeam/Minecraft-Console-Client/tree/master/MinecraftClient/config) folder for more examples.

If you need more advanced behavior, see [Creating Chat Bots](creating-bots.md).
