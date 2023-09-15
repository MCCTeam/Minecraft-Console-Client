---
title: Creating Simple Script
---

# Creating Simple Script

A simple script is a text file with one command per line. See [Internal Commands](https://mccteam.github.io/guide/usage.html#internal-commands) section or type `/help` in the console to see available commands. Any line beginning with `#` is ignored and treated as a comment.

Application variables defined using the set command or [AppVars] INI section can be used. The following read-only variables can also be used: `%username%, %login%, %serverip%, %serverport%, %datetime%`

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

Go to [example scripts](https://github.com/MCCTeam/Minecraft-Console-Client/tree/master/MinecraftClient/config) to see more example.

If you want need advanced functions, please see [Creating Chat Bots](creating-bots.md)