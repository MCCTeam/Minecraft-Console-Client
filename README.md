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

## Building and usage on Linux
First you need to install the **Mono**.
Read this and follow the instructions: [Project Mono installation](https://www.mono-project.com/download/stable/#download-lin).

Then you need to install the **Git** if you do not have it installed.

To install it, follow the instructions from [Git](https://git-scm.com/download/linux).

After that, navigate to your prefered directory and execute the following:
```
git clone https://github.com/ORelio/Minecraft-Console-Client.git .
```
Then, navigate to the **Minecraft-Console-Client/MinecraftClient**.

Edit the **MinecraftClient.csproj**, change the following:
```
<Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
<Platform Condition=" '$(Platform)' == '' ">x86</Platform>
```
To:
```
<Configuration Condition=" '$(Configuration)' == '' ">Release</Configuration>
<Platform Condition=" '$(Platform)' == '' ">x86</Platform>
```
(Replace your platform).

Type the following:
```
msbuild
```
After the build has finished, then navigate to the **bin/Release** directory, and then you can move the **.exe** file to your prefered location.

To run the file just type: **mono MinecraftClient.exe**

You can create **start.sh** with '**mono MinecraftClient.exe**' inside it (**without quotes**), then do **chmod +x start.sh** and then you can run it by typing: **./start.sh**.

## How to use

Check out the [sample configuration files](MinecraftClient/config/) which includes the how-to-use README.
Help and more info is also available on the [Minecraft Forum thread](http://www.minecraftforum.net/topic/1314800-/).<br/>

## How to contribute

If you'd like to contribute to Minecraft Console Client, great, just fork the repository and submit a pull request. The *Indev* branch for contributions to future stable versions is no longer used as MCC is currently distributed as development builds only.

## License

Unless specifically stated, the code is from the MCC developers, and available under CDDL-1.0.
Else, the license and original author are mentioned in source file headers.
The main terms of the CDDL-1.0 license are basically the following:

- You may use the licensed code in whole or in part in any program you desire, regardless of the license of the program as a whole (or rather, as excluding the code you are borrowing). The program itself may be open or closed source, free or commercial.
- However, in all cases, any modifications, improvements, or additions to the CDDL code (any code that is referenced in direct modifications to the CDDL code is considered an addition to the CDDL code, and so is bound by this requirement; e.g. a modification of a math function to use a fast lookup table makes that table itself an addition to the CDDL code, regardless of whether it's in a source code file of its own) must be made publicly and freely available in source, under the CDDL license itself.
- In any program (source or binary) that uses CDDL code, recognition must be given to the source (either project or author) of the CDDL code. As well, modifications to the CDDL code (which must be distributed as source) may not remove notices indicating the ancestry of the code.

More info at http://qstuff.blogspot.fr/2007/04/why-cddl.html
Full license at http://opensource.org/licenses/CDDL-1.0
