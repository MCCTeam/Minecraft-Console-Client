Minecraft Console Client
========================

[![Appveyor build status](https://ci.appveyor.com/api/projects/status/github/ORelio/Minecraft-Console-Client?branch=Indev)](https://ci.appveyor.com/project/ORelio/minecraft-console-client)

Minecraft Console Client(MCC) is a lightweight app allowing you to connect to any Minecraft server,
send commands and receive text messages in a fast and easy way without having to open the main Minecraft game. It also provides various automation for administration and other purposes.

## Looking for maintainers

Due to no longer having time to implement upgrades for new Minecraft versions and fixing bugs, I'm looking for motivated people to take over the project. If you feel like it could be you, please have a look at the [issues](https://github.com/ORelio/Minecraft-Console-Client/issues?q=is%3Aissue+is%3Aopen+label%3Awaiting-for%3Acontributor) section :)

## Download

Get exe file from the latest [development build](https://ci.appveyor.com/project/ORelio/minecraft-console-client/build/artifacts).
This exe file is a .NET binary which also works on Mac and Linux.

## How to use

Check out the [sample configuration files](MinecraftClient/config/) which includes the how-to-use README.
Help and more info is also available on the [Minecraft Forum thread](http://www.minecraftforum.net/topic/1314800-/).<br/>

## Building from source

First of all, get a [zip of source code](https://github.com/ORelio/Minecraft-Console-Client/archive/master.zip), extract it and navigate to the `MinecraftClient` folder.

Edit `MinecraftClient.csproj` to set the Build target to `Release` on [line 4](https://github.com/ORelio/Minecraft-Console-Client/blob/master/MinecraftClient/MinecraftClient.csproj#L4):

```xml
<Configuration Condition=" '$(Configuration)' == '' ">Release</Configuration>
```

### On Windows

1. Locate `MSBuild.exe` for .NET 4 inside `C:\Windows\Microsoft.NET\Framework\v4.X.XXXXX`
2. Drag and drop `MinecraftClient.csproj` over `MSBuild.exe` to launch the build
3. If the build succeeds, you can find `MinecraftClient.exe` under `MinecraftClient\bin\Release`

### On Mac and Linux

1. Install the [Mono Framework](https://www.mono-project.com/download/stable/#download-lin) if not already installed
2. Run `msbuild MinecraftClient.csproj` in a terminal
3. If the build succeeds, you can find `MinecraftClient.exe` under `MinecraftClient\bin\Release`

## How to contribute

If you'd like to contribute to Minecraft Console Client, great, just fork the repository and submit a pull request. The *Indev* branch for contributions to future stable versions is no longer used as MCC is currently distributed as development builds only.

## Translate Minecraft Console Client to your language

If you would like to translate Minecraft Console Client to a different language, please download the translation file from [the repository](https://github.com/ORelio/Minecraft-Console-Client/tree/master/MinecraftClient/Resources/lang) or just fork the repository. Once you finished the translation work, submit a pull request or send us the file in case you did not fork the repository.

To use the translated language file, place it under `lang/mcc/` folder and set your language in `.ini` config. You may create the directory if does not exist.

For the names of the translation file, please see [this comment](https://github.com/ORelio/Minecraft-Console-Client/pull/1282#issuecomment-711150715).

## License

Unless specifically stated, the code is from the MCC developers, and available under CDDL-1.0.
Else, the license and original author are mentioned in source file headers.
The main terms of the CDDL-1.0 license are basically the following:

- You may use the licensed code in whole or in part in any program you desire, regardless of the license of the program as a whole (or rather, as excluding the code you are borrowing). The program itself may be open or closed source, free or commercial.
- However, in all cases, any modifications, improvements, or additions to the CDDL code (any code that is referenced in direct modifications to the CDDL code is considered an addition to the CDDL code, and so is bound by this requirement; e.g. a modification of a math function to use a fast lookup table makes that table itself an addition to the CDDL code, regardless of whether it's in a source code file of its own) must be made publicly and freely available in source, under the CDDL license itself.
- In any program (source or binary) that uses CDDL code, recognition must be given to the source (either project or author) of the CDDL code. As well, modifications to the CDDL code (which must be distributed as source) may not remove notices indicating the ancestry of the code.

More info at http://qstuff.blogspot.fr/2007/04/why-cddl.html
Full license at http://opensource.org/licenses/CDDL-1.0
