Minecraft Console Client
========================

[![GitHub Actions build status](https://github.com/MCCTeam/Minecraft-Console-Client/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/MCCTeam/Minecraft-Console-Client/releases/latest)

Minecraft Console Client (MCC) is een simpele app waarmee je Minecraft servers kan joinen, command sturen en berichten ontvangen op een snelle en makkelijke manier zonder Minecraft echt hoeven te openen. Het biedt ook verschillende geautomatiseerde functies die je kan aanzetten voor administratie en andere dingen.

## Download 🔽

Krijg het binary bestand van onze laatste [development build](https://github.com/MCCTeam/Minecraft-Console-Client/releases/latest).
De exe file is een .NET binary die ook werkt op Mac en Linux.

## How to use 📚

Bekijk de [sample configuration files](MinecraftClient/config/) die de how-to-use bevat [README](https://github.com/MCCTeam/Minecraft-Console-Client/tree/master/MinecraftClient/config#minecraft-console-client-user-manual).

## Getting Help 🙋

Bekijk de [README](https://github.com/MCCTeam/Minecraft-Console-Client/tree/master/MinecraftClient/config#minecraft-console-client-user-manual) en de bestaande [Discussions](https://github.com/MCCTeam/Minecraft-Console-Client/discussions): Misschien is je vraag daar al beantwoord. Als dat niet zo is open dan een [New Discussion](https://github.com/MCCTeam/Minecraft-Console-Client/discussions/new) en stel je vraag. Als je een bug vind, raporteer dat in de [Issues](https://github.com/MCCTeam/Minecraft-Console-Client/issues) sectie.

## Helping Us ❤️
 
We are a small community so we need help to implement upgrades for new Minecraft versions, fixing bugs and expanding the project. We are always looking for motivated people to contribute. If you feel like it could be you, please have a look at the [issues](https://github.com/MCCTeam/Minecraft-Console-Client/issues?q=is%3Aissue+is%3Aopen+label%3Awaiting-for%3Acontributor) section :)

## How to contribute 📝

If you'd like to contribute to Minecraft Console Client, great, just fork the repository and submit a pull request on the *Master* branch. MCC is currently distributed as development builds only (which are usually stable) so we no longer use the *Indev* branch.

## Translating Minecraft Console Client 🌍

If you would like to translate Minecraft Console Client to a different language, please download the translation file from [the lang folder](https://github.com/MCCTeam/Minecraft-Console-Client/tree/master/MinecraftClient/Resources/lang) or just fork the repository. Once you finished the translation work, submit a pull request or send us the file through an [Issue](https://github.com/MCCTeam/Minecraft-Console-Client/issues) in case you are not familiar with Git.

To use the translated language file, place it under `lang/mcc/` folder and set your language in `.ini` config. You may create the directory if does not exist.

For the names of the translation file, please see [this comment](https://github.com/MCCTeam/Minecraft-Console-Client/pull/1282#issuecomment-711150715).

## Building from source 🏗️

_The recommended development environment is [Visual Studio](https://visualstudio.microsoft.com/). If you want to build the project without installing a development environment, you may also follow these instructions:_

First of all, download the .NET 6.0 SDK [here](https://dotnet.microsoft.com/en-us/download) and follow the install instructions.

Get a [zip of source code](https://github.com/MCCTeam/Minecraft-Console-Client/archive/master.zip), extract it and navigate to the `MinecraftClient` folder.

### On Windows 🪟

1. Open a Terminal / Command Prompt.
2. Type in `dotnet publish --no-self-contained -r win-x64 -c Release`.
3. If the build succeeds, you can find `MinecraftClient.exe` under `MinecraftClient\bin\Release\net6.0\win-x64\publish\`

### On Linux 🐧

1. Open a Terminal / Command Prompt.
2. Type in `dotnet publish --no-self-contained -r linux-x64 -c Release`.
3. If the build succeeds, you can find `MinecraftClient` under `MinecraftClient\bin\Release\net6.0\linux-x64\publish\`

### On Mac 🍎

1. Open a Terminal / Command Prompt.
2. Type in `dotnet publish --no-self-contained -r osx-x64 -c Release`.
3. If the build succeeds, you can find `MinecraftClient` under `MinecraftClient\bin\Release\net6.0\osx-x64\publish\`

## License ⚖️

Unless specifically stated, the code is from the MCC Team or Contributors, and available under CDDL-1.0. Else, the license and original author are mentioned in source file headers.
The main terms of the CDDL-1.0 license are basically the following:

- You may use the licensed code in whole or in part in any program you desire, regardless of the license of the program as a whole (or rather, as excluding the code you are borrowing). The program itself may be open or closed source, free or commercial.
- However, in all cases, any modifications, improvements, or additions to the CDDL code (any code that is referenced in direct modifications to the CDDL code is considered an addition to the CDDL code, and so is bound by this requirement; e.g. a modification of a math function to use a fast lookup table makes that table itself an addition to the CDDL code, regardless of whether it's in a source code file of its own) must be made publicly and freely available in source, under the CDDL license itself.
- In any program (source or binary) that uses CDDL code, recognition must be given to the source (either project or author) of the CDDL code. As well, modifications to the CDDL code (which must be distributed as source) may not remove notices indicating the ancestry of the code.

More info at http://qstuff.blogspot.fr/2007/04/why-cddl.html
Full license at http://opensource.org/licenses/CDDL-1.0
