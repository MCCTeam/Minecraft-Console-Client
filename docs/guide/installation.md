# Installation

-   [YouTube Tutorials](#youtube-tutorials)
-   [Download a compiled binary](#download-a-compiled-binary)
-   [Building from the source code](#building-from-the-source-code)
-   [Run using Docker](#using-docker)
-   [Run on Android](#run-on-android)
-   [Run MCC 24/7 on a VPS](#run-on-a-vps)

## YouTube Tutorials

If you're not the kind of person that likes textual tutorials, our community has made video tutorials available on YouTube.

-   [Installation on Windows by Daenges](https://www.youtube.com/watch?v=BkCqOCa2uQw)
-   [Installation on Windows + Auto AFK and More by Dexter113](https://www.youtube.com/watch?v=FxJ0KFIHDrY)

## Download a compiled binary

In order to run **Minecraft Console Client** you need [.NET 6.0 or new-er](https://dotnet.microsoft.com/en-us/download)

> **⚠️ IMPORTANT: If you already have .NET, but an older version than `6.0`, please update to `6.0` since MCC might not work as expected or at all.**

#### How to install .NET on your platform

-   [Install .NET on Windows](https://docs.microsoft.com/en-us/dotnet/core/install/windows)
-   [Install .NET on Linux](https://docs.microsoft.com/en-us/dotnet/core/install/linux)
-   [Install .NET on macOS](https://docs.microsoft.com/en-us/dotnet/core/install/macos)

#### Download

You can download a compiled binary file of the latest build from our Releases section on Git Hub: [Download](https://github.com/MCCTeam/Minecraft-Console-Client/releases)

## Building from the source code

### Windows

Requirements:

-   [Git](https://www.git-scm.com/)
-   [.NET 6.0 or new-er](https://dotnet.microsoft.com/en-us/download) or [Visual Studio](https://visualstudio.microsoft.com/) configured for C# app development

#### Cloning using Git

Install [Git](https://www.git-scm.com/)

1. Make a new folder where you want to keep the source code
2. Then open it up, hold `SHIFT` and do a `right-click` on the empty white space in the folder
3. Click on `Git Bash Here` in the context menu
4. Clone the [Git Hub Repository](https://github.com/MCCTeam/Minecraft-Console-Client) by typing end executing the following command:

```bash
git clone https://github.com/MCCTeam/Minecraft-Console-Client.git --recursive
```

5. Once the repository has been cloned, you can close the `Git Bash` terminal emulator
6. Open up the new cloned folder

#### Building using the Visual Studio

1. Open up the `MinecraftClient.sln` via Visual Studio
2. Right click on `MinecraftClient` solution in the `Solution Explorer`
3. Click on `Properties`
4. Open up the `Build` tab and select configuration `Release`
5. Press `CTRL + S` and close the file
6. Right click on `MinecraftClient` solution in the `Solution Explorer`
7. Click `Build`

If the build has succeeded, the compiled binary `MinecraftClient.exe` will be in `MinecraftClient/bin/Release/net6.0/win-x64/publish` folder.

#### Building using .NET manually without Visual Studio

1. Open the `Minecraft-Console-Client` folder you've cloned or downloaded
2. Open the PowerShell (`Right-Click` on the whitespace and click `Open PowerShell`, or in Windows Explorer: `File -> Open PowerShell`)
3. Run the following command to build the project:

```bash
dotnet publish MinecraftClient -f net6.0 -r win-x64 --no-self-contained -c Release -p:UseAppHost=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None
```

If the build has succeeded, the compiled binary `MinecraftClient.exe` will be in `MinecraftClient/bin/Release/net6.0/win-x64/publish` folder.

### Linux, macOS

Requirements:

-   Git

    -   Linux:

        > **ℹ️ NOTE: If you're using Linux we will assume that you should be able to install git on your own. If you don't know how, search it up for your distribution, it should be easy. (Debian based distros: `apt install git`, Arch based: `pacman -S git`)**

    -   [Install Git on macOS](https://git-scm.com/download/mac)

-   .NET SDK 6.0 or new-er

    -   [Install .NET on Linux](https://docs.microsoft.com/en-us/dotnet/core/install/linux)
    -   [Install .NET on macOS](https://docs.microsoft.com/en-us/dotnet/core/install/macos)

#### Cloning using Git

1. Open up a terminal emulator and navigate to the folder where you will store the MCC
2. Recursively clone the [Git Hub Repository](https://github.com/MCCTeam/Minecraft-Console-Client) by typing end executing the following command:

```bash
git clone https://github.com/MCCTeam/Minecraft-Console-Client.git --recursive
```

3. Go to the folder you've cloned (should be `Minecraft-Console-Client`)
4. Run the following command to build the project:

    - On Linux:

        ```bash
        dotnet publish MinecraftClient -f net6.0 -r linux-x64 --no-self-contained -c Release -p:UseAppHost=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None
        ```

        > **ℹ️ NOTE: If you're using Linux that is either ARM, 32-bit, Rhel based, Using Musl, or Tirzen, [find an appropriate RID](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog#linux-rids) for your platform and replace the `'-r linux-64` with an appropriate `-r <your RID>` (Example for arm: `-r linux-arm64`)**

    - On macOS:

        ```bash
        dotnet publish MinecraftClient -f net6.0 -r osx-x64 --no-self-contained -c Release -p:UseAppHost=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None
        ```

        > **ℹ️ NOTE: If you're not using MAC with Intel, find an appropriate RID for your ARM processor, [find an appropriate RID](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog#macos-rids) and replace the `'-r osx-64` with an appropriate `-r <your RID>` (Example for arm: `-r osx.12-arm64`)**

If the build has succeeded, the compiled binary `MinecraftClient` will be in:

-   Linux: `MinecraftClient/bin/Release/net6.0/linux-x64/publish/`
-   macOS: `MinecraftClient/bin/Release/net6.0/osx-x64/publish/`

## Using Docker

Requirements:

-   Git
-   Docker

> **ℹ️ NOTE: This section is for more advanced users, if you do not know how to install git or docker, you can take a look at other sections for Git, and search on how to install Docker on your system.**

> **⚠️ WARNING: Pay attention at warnings, Docker currently works, but you must start the containers in the interactive mode or MCC will crash, we're working on solving this.**

1. Clone the [Git Hub Repository](https://github.com/MCCTeam/Minecraft-Console-Client) by typing end executing the following command:

```bash
git clone https://github.com/MCCTeam/Minecraft-Console-Client.git --recursive
```

2. Navigate to `Minecraft-Console-Client/Docker`
3. Build the image using the following command

```bash
docker build -t minecraft-console-client:latest .
```

**Start the container using Docker:**

> **⚠️ VERY IMPORTANT ⚠️: There is a bug with the ConsoleInteractive which causes a crash when a container is started in a headless mode, so you need to use the interactive mode. Do not restart containers in a classic way, stop then and start them with interactive mode (this command), after that simply detach with `CTRL + P` and then `CTRL + Q`.**

```bash
# You could also ignore the -v parameter if you dont want to mount the volume that is up to you. If you don't it's harder to edit the .ini file if thats something you want to do
docker run -it -v <PATH_ON_YOUR_MACHINE_TO_MOUNT>:/opt/data minecraft-console-client:latest
```

Now you could login and the Client is running.
To detach from the Client but still keep it running in the Background press: `CTRL + P` and then after `CTRL + Q`.
To reattach use the `docker attach` command.

**Start the container using docker-compose:**

By default, the volume of the container gets mapped into a new folder named `data` in the same folder the `docker-compose.yml` is stored.

If you don't want to map a volume, you have to comment out or delete the entire volumes section:

```yml
#volumes:
#- './data:/opt/data'
```

Make sure you are in the directory the `docker-compose.yml` is stored before you attempt to start. If you do so, you can start the container:

```bash
docker-compose run MCC
```

Remember to remove the container after usage:

```bash
docker-compose down
```

If you use the INI file and entered your data (username, password, server) there, you can start your container using

```bash
docker-compose up
docker-compose up -d #for deamonized running in the background
```

Note that you won't be able to interact with the client using `docker-compose up`. If you want that functionality, please use the first method: `docker-compose run MCC`.
As above, you can stop and remove the container using

```bash
docker-compose down
```

## Run on Android

It is possible to run the Minecraft Console Client on Android through Termux and Ubuntu 22.04 in it, however it requires a manual setup with a lot of commands, be careful no to skip any steps. Note that this might take anywhere from 10 to 20 minutes or more to do depending on your technical knowledge level, Internet speed and CPU speed.

> **ℹ️ NOTE: This section is going to get a bit technical, I'll try my best to make everything as simple as possible. If you are having trouble following along or if you encounter any issues, feel free to open up a discussion on our Github repository page.**

> **ℹ️ NOTE: You're required to have some bare basic knowledge of Linux, if you do not know anything about it, watch [this video](https://www.youtube.com/watch?v=SkB-eRCzWIU) to get familiar with basic commands.**

> **ℹ️ NOTE: Here we're installing everything on the root account for simplicity sake, if you want to make a user account, make sure you update the command which reference the `/root` directory with your home directory.**

### Installation

#### Termux

> **⚠️ IMPORTANT: The Play Store version of Termux is outdated and not supported, do not use it, use the the [Github one](https://github.com/termux/termux-app/releases/latest/).**

Go to [the Termux Github latest release](https://github.com/termux/termux-app/releases/latest/), download the `debug_universal.apk`, unzip it and run it.

> **ℹ️ NOTE: If your file manager does not let you run APK files, install and use `File Manager +` and give it a permission to install 3rd party applications when asked.**

> **⚠️ VERY IMPORTANT ⚠️: Once you have installed Termux, open it, bring down the Android menu for notifications, on Termux notification, drag down until you see the following options: `Exit | Acquire wakelock`, press on the `Acquire wakelock` and allow Termux to have a battery optimization exclusion permission when asked. If you do not do this, your performance will be poorer and the Termux might get killed by Android while running in the background!**

#### Installing Ubuntu 22.04

At this stage, you have 2 options:

1. Following this textual tutorial
2. Watching a [Youtube tutorial for installing Ubuntu](https://www.youtube.com/watch?v=5yit2t7smpM)

> **ℹ️ NOTE: If you decide to watch the Youtube tutorial, watch only up to `1:58`, the steps after are not needed and might just confuse you.**

In order to install Ubuntu 22.04 in Termux you require `wget` and `proot`, we're going to install them in the next step.

Once you have Termux installed open it up and run the following command one after other (in order):

1. `pkg update`
2. `pkg upgrade`
3. `pkg install proot wget`

> **ℹ️ NOTE: If you're asked to press Y/N during the update/upgrade command process, just enter Y and press Enter**

Then you need to download an installation script using the following command:

```bash
wget https://raw.githubusercontent.com/MFDGaming/ubuntu-in-termux/master/ubuntu.sh
```

Once the script has downloaded, run it with:

```bash
bash ubuntu.sh
```

Then you will be asked a question, enter `Y` and press `Enter`.
Once the installation is complete, you can start Ubuntu with:

```bash
./startubuntu.sh
```

> **ℹ️ NOTE: Now every time you open Termux after it has been closed, in order to access Ubuntu you have to use this command**

#### Installing .NET on ARM

Since there are issues installing .NET 6.0 via the APT package manager at the time of writing, we will have to install it manually.

First we need to update the APT package manager repositories and install dependencies.

To update the APT repositories, run the following command:

```bash
apt update -y && apt upgrade -y
```

After you did it, we need to install dependencies for .NET, with the following command:

```bash
apt install wget nano unzip libc6 libgcc1 libgssapi-krb5-2 libstdc++6 zlib1g libicu70 libssl3 -y
```

After you have installed dependencies, it's time to install .NET, you either can follow this tutorial or the [Microsoft one](https://docs.microsoft.com/en-us/dotnet/core/install/linux-scripted-manual#manual-install).

Navigate to your `/root` home directory with the following command:

```bash
cd /root
```

First you need to download .NET 6.0, you can do it with the following command:

```bash
wget https://download.visualstudio.microsoft.com/download/pr/901f7928-5479-4d32-a9e5-ba66162ca0e4/d00b935ec4dc79a27f5bde00712ed3d7/dotnet-sdk-6.0.400-linux-arm64.tar.gz
```

> **ℹ️ NOTE: This tutorial assumes that you have 64 bit version of ARM processor, if you happen to have a 32-bit version replace the link in the command above with [this one](https://download.visualstudio.microsoft.com/download/pr/cf567026-a29a-41aa-bc3a-e4e1ad0df480/0925d411e8e09e31ba7a39a3eb0e29af/aspnetcore-runtime-6.0.8-linux-arm.tar.gz)**.

> **ℹ️ NOTE: This tutorial assumes that you're following along and using Ubuntu 22.04, if you're using a different distro, like Alpine, go to [here](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) and copy an appropriate link for your distro.**

Once the file has been downloaded, you need to run the following commands in order:

1. `DOTNET_FILE=dotnet-sdk-6.0.400-linux-arm64.tar.gz`

    > **⚠️ IMPORTANT: If you're using a different download link, update the file name in this command to match your version.**

2. `export DOTNET_ROOT=/root/.dotnet`

    > **⚠️ IMPORTANT: Here we're installing .NET in `/root`, if you're installing it somewhere else, make sure to set your own path!**

3. `mkdir -p "$DOTNET_ROOT" && tar zxf "$DOTNET_FILE" -C "$DOTNET_ROOT"`
4. `export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools`

Now we need to tell our shell to know where the `dotnet` command is, for future sessions, since the commands above just tell this current session where the `dotnet` is located.

> **⚠️ IMPORTANT: You will need a basic knowledge of Nano text editor, if you do not know how to use it, watch this [Youtube video tutorial](https://www.youtube.com/watch?v=DLeATFgGM-A)**

To enable this, we need to edit our `/root/.bashrc` file with the following command:

```bash
nano /root/.bashrc
```

Scroll down to the bottom of the file using `Page Down` (`PGDN`) button, make a new line and paste the following text:

```bash
export DOTNET_ROOT=/root/.dotnet/
export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools
```

> **⚠️ IMPORTANT: Here we're installing .NET in `/root`, if you're installing it somewhere else, make sure to set your own path!**

Save the file usign the following combination of keys: `CTRL + X`, type `Y` and press Enter.

Veryfy that .NET was installed correctly by running:

```bash
dotnet
```

You should get a help page:

```bash
root@localhost:~# dotnet

Usage: dotnet [options]
Usage: dotnet [path-to-application]

Options:
  -h|--help         Display help.
  --info            Display .NET information.
  --list-sdks       Display the installed SDKs.
  --list-runtimes   Display the installed runtimes.

path-to-application:
  The path to an application .dll file to execute.
```

#### Installing MCC

Finally, we can install MCC.

> **⚠️ IMPORTANT: If you have a 32 ARM processor, you need to build the MCC yourself, take a look at the [Building From Source](#building-from-the-source-code) section. Also make sure to be using the appropriate `-r` parameter value for your architecture.**

Let's make a folder where the MCC will be stored with the following command:

```bash
mkdir MinecraftConsoleClient
```

Then enter it the newly created folder:

```bash
cd MinecraftConsoleClient
```

Download the MCC with the following command:

```bash
wget https://github.com/MCCTeam/Minecraft-Console-Client/releases/latest/download/MinecraftClient-linux-arm64.zip
```

Unzip it with the following command:

```bash
unzip MinecraftClient-linux-arm64.zip
```

You can remove the zip archive now, we do not need it anymore, with:

```bash
rm MinecraftClient-linux-arm64.zip
```

And finally run it with:

```
./MinecraftClient
```

#### After installation

When you run Termux next time, you need to start Ubuntu with: `./startubuntu.sh`

Then you can start the MCC again with `./MinecraftClient`

To stop MCC from running you can press `CTRL + C`

To edit the configuration/settings, you need a text editor, we recommend Nano, as it's very simple to use, if you have followed the installation steps above, you should be familiar with it, if not, check out [this tutorial](https://www.youtube.com/watch?v=DLeATFgGM-A).

For downloading files, you can use the `wget` file we have installed, simply run:
`wget your_link_here` (you have examples above, and a video tutorial down bellow).

Also, here are some linux tutorials for people who are new to it:

-   [Linux Terminal Introduction by ExplainingComputers](https://www.youtube.com/watch?v=SkB-eRCzWIU)
-   [Linux Crash Course - nano (command-line text editor) by Learn Linux TV](https://www.youtube.com/watch?v=DLeATFgGM-A)
-   [Linux Crash Course - The wget Command by Learn Linux TV](https://www.youtube.com/watch?v=F80Z5qd2b_4)
-   [Linux Basics: How to Untar and Unzip Files (tar, gzip) by webpwnized](https://www.youtube.com/watch?v=1DF0dTscHHs)

## Run on a VPS

> **ℹ️ NOTE: This is a new section, if you find a mistake, please report it by opening an Issue in our [Github repository](https://github.com/MCCTeam/Minecraft-Console-Client). Thank you!**

The **Minecraft Console Client** can be run on a VPS 24 hours, 7 days a week.

-   [What is a VPS?](#what-is-a-vps)
-   [Prerequisites](#prerequisites)
-   [Where to get a VPS](#where-to-get-a-vps)
-   [Initial Amazon VPS setup](#initial-amazon-vps-setup)
-   [Initial VPS setup](#initial-vps-setup)
-   [Creating a new user account](#creating-a-new-user)
-   [Installing .NET Core 6](#installing-net-core-6)
-   [Installing the Minecraft Console Client](#installing-mcc-on-a-vps)

### What is a VPS?

VPS stands for a **V**irtual **P**rivate **S**erver, it's basically a remote virtual PC that is running in the cloud, 24 hours a day, 7 days in week. To be precise, it's a virtual machine that runs on top of a host operating system (eg. Proxmox).

You can use a VPS for hosting a website, or a an app, or a game server, or your own VPN, or the Minecraft Console Client.

Here is a [Youtube video](https://youtu.be/42fwh_1KP_o) that explains it in more detail if you're interested.

### Prerequisites

1. Gitbash (if you're on Windows)

    Download and install [Gitbash](https://git-scm.com/downloads).

    > **ℹ️ NOTE: Make sure to allow the installation to add it to the context menu**

2. `ssh` and `ssh-keygen` commands (On Windows they're available with Gitbash, on macOs and Linux they should be available by default, it not, search on how to install them)

3. Basic knowledge of Linux shell commands, terminal emulator usage, SSH and Nano editor.

    If you already know this, feel free to skip.

    if you get stuck, watch those tutorials.

    If you're new to this, you can learn about it here:

    - [What is Linux? by Bennett Bytes](https://www.youtube.com/watch?v=JsWQUOEL0N8)
    - [Linux Terminal Introduction by ExplainingComputers](https://www.youtube.com/watch?v=SkB-eRCzWIU)
    - [Linux Crash Course - nano (command-line text editor) by Learn Linux TV](https://www.youtube.com/watch?v=DLeATFgGM-A)
    - [Linux Crash Course - The wget Command by Learn Linux TV](https://www.youtube.com/watch?v=F80Z5qd2b_4)
    - [Linux Basics: How to Untar and Unzip Files (tar, gzip) by webpwnized](https://www.youtube.com/watch?v=1DF0dTscHHs)

### Where to get a VPS

You have 2 options:

-   [Buying a VPS](#buying-a-vps)
-   [Getting an AWS EC2 VPS for free (12 months free trial)](#aws-ec2-vps)

#### Buying a VPS

If you do not want to give your info to Amazon or don't have a debit card, you can buy your own VPS.

**What hardware requirements I need for running the MCC?**

The MCC is not expensive to run, so it can run on basically any hardware, you do not need to spend a lot of money on a VPS if you are going to run just the MCC, go with the cheapest option.

**Where to buy a VPS?**

> **⚠️ VERY IMPORTANT: In this tutorial we will be using `Ubuntu 22.04`, make sure to select it as the OS when buying a VPS.**

Some of the reliable and cheap hosting providers (sorted for price/performance):

-   [E-Trail](https://e-trail.net/vps)

    **Minimum price**: `2.50 EUR / month`

    > **ℹ️ NOTE: Does not have Ubuntu 22.04 in the dropdown menu when ordering, you will have to re-install later or ask support to do it.**

-   [OVH Cloud](https://www.ovhcloud.com/de/vps/)

    **Minimum price**: `3.57 EUR / month`

-   [Hetzner Cloud](https://www.hetzner.com/cloud)

    **Minimum price**: `4.51 EUR / month`

-   [Digital Ocean](https://www.digitalocean.com/pricing/droplets)

    **Minimum price**: `4 EUR / month`

-   [Contabo](https://contabo.com/en/vps/)

    **Minimum price**: `7 EUR / month`

    **More serious VPS able to host multiple applications, 4 CPU cores and 8 GB of RAM, 200 GB SSD**

You also may want to search for better deals.

#### AWS EC2 VPS

> **⚠️ VERY IMPORTANT: This will require you to have a valid debit card that can be used on internet and a mobile phone number, as well as giving that info to Amazon corporation.**

> **⚠️ WARNING: Scammers often get AWS VPS and use it to mass login on to stolen Microsoft accounts, some AWS IP addresses might be blocked by Microsoft because of that, if so, you might need to switch regions or to use a Proxy. To debug if your IP has been banned by Microsoft, use the `ping <ip>` and `traceroute <ip>` commands.**

> **⚠️ WARNING: Related to the warning above, if you have issues logging with Microsoft and you're not banned, you may want to check the Security center on your account and approve the login from the VPS, this can be the case for some users.**

> **ℹ️ NOTE: If you're not banned, sometimes fetching the keys can take some time, try giving it a minute or two, if it still hangs, hit some keys to refresh the screen, or try restarting and running again. If it still happens, use tmux instead of screen.**

Register on AWS and enter all of your billing info and a phone number.
Once you're done, you can continue to [Setting up the Amazon VPS](#setting-up-an-aws-vps).

### Initial Amazon VPS setup

> **ℹ️ NOTE: Skip this section if you're not using AWS. Go to [Initial VPS setup](#initial-vps-setup)**

When you register and open the `AWS Console`, click on the Search field on the top of the page and search for: `EC2`

> **ℹ️ NOTE: Make sure to select the region closest to you for the minimal latency**

Click on the **Launch instance** button.

Fill out the `Name` field with a name of your preference.

![http://i.pics.rs/22ArI.png](http://i.pics.rs/WWzUM.png)

For the **Application and OS images** select `Ubuntu Server 22.04 LTS (HVM), SSD Volume Type`.

> **⚠️ VERY IMPORTANT: Make sure that it has `Free tier eligible` next to it.**

![http://i.pics.rs/22ArI.png](http://i.pics.rs/22ArI.png)

For the **Instance type** select `t2.micro`.

For the **Key pair (login)** click on **Create new key pair** and name it `VpsRoot`, leave the rest of settings as default and click **Create key pair**, this will generate a RSA private key that will be automatically downloaded.

> **⚠️ EXTREMELY IMPORTANT: Make sure that you save this file in a safe place and do not loose it, it's of an upmost importance since it's used to access the root/admin account of the VPS. Without it you will not be able to access the root account of the VPS! Also do not let it fall into wrong hands.**

![http://i.pics.rs/jjD4d.png](http://i.pics.rs/jjD4d.png)

For the **Network settings** check the following checkboxes on:

-   `Allow SSH traffic from` (Anywhere)
-   `Allow HTTPs traffic from the internet`
-   `Allow HTTP traffic from the internet`

> **ℹ️ NOTE: The SSH traffic from Anywhere is not the best thing for security, you might want to enter IP addresses of your devices from which you want to access the VPS manually.**

![http://i.pics.rs/77atQ.png](http://i.pics.rs/77atQ.png)

For the **Storage** enter `30`.

![http://i.pics.rs/cc4yz.png](http://i.pics.rs/cc4yz.png)

Finally, review the **Summary** confirm that everything is as in the tutorial and that you will not be charged and click on the **Launch instance**. Once you've clicked on the button, it will take a couple of minutes for the instance to be available up and running.

Once the instance is up and running, go to it's details and copy the `Public DNS v4 IP`.

You now need to login, go to your folder where you keep the private key you've generated and downloaded (make sure you make a new folder for it, do not keep in the downloads folder) and right click on the empty white space (not on files), if you're on Windows click **Git Bash here**, on mac OS and Linux click on **Open Terminal** (or whatever it is called).

In order to login with SSH, you are going to use the following command:

```bash
ssh -i <name of your private root key here> ubuntu@<your public dns v4 ip here>
```

> **ℹ️ NOTE: `<` and `>` are not typed, that is just a notation for a placeholder!**

> **ℹ️ NOTE: `ubuntu` is a default root account username for Ubuntu on AWS!**

Example:

```bash
ssh -i VpsRoot.pem ubuntu@ec2-3-71-108-69.eu-central-1.compute.amazonaws.com
```

If you've provided the right info you should get `Welcome to Ubuntu 20.04.5 LTS` message.

Now you can continue to [Creating a new user](#creating-a-new-user)

### Initial VPS setup

> **ℹ️ NOTE: This section if for those who do not use AWS, if you use AWS skip it**

When you order the VPS, most likely you will be asked to provide the root account name and password, if it is the case, name the account as `root` and give it a password of your choice.

Other option is that you will get your login info in the email once the setup is done.

Once you have the root login account info, you need [Gitbash](https://git-scm.com/downloads) on Windows and `ssh` if you're on macOS or Linux (if you do not have it by some chance, search on how to install it, it is simple).

If you're on Windows open `Git Bash`, on mac OS and Linux open a `Terminal` and type the following command:

```bash
ssh <username>@<ip>
```

> **ℹ️ NOTE: If you're given a custom port other than `22` by your host, you should add `-p <port here>` before the username (eg. `ssh -p <port here> <username>@<ip>`) or `:<port>` after the ip (eg. `ssh <username>@<ip>:<port>`)**

Example:

```bash
ssh root@142.26.73.14
```

Example with port:

```bash
ssh -p 2233 root@142.26.73.14
```

Once you've logged in you should see a Linux prompt and a welcome message if there is one set by your provider.

### Creating a new user

Once you've logged in to your VPS you need to create a new user and give it SSH access.

In this tutorial we will be using `mcc` as a name for the user account that will be running the MCC.

> **ℹ️ NOTE: You may be wondering why we're creating a separate user account and making it be accessible over SSH only. This is for security reasons, if you do not want to do this, you're free to skip it, but be careful.**

To create a new user named `mcc` execute the following command:

```bash
sudo useradd mcc -m
```

Now we need to give it a password, execute the following command, type the password and confirm it:

```bash
sudo passwd mcc
```

> **ℹ️ NOTE: When you're typing a password it will not be displayed on the screen, but you're typing it for real.**

> **ℹ️ NOTE: Make sure you have a strong password!**

Now we need to give our user account the admin permissions:

```bash
sudo usermod -aG sudo mcc
```

Now we are going to set it's shell to bash:

```bash
sudo chsh mcc -s /bin/bash
```

Now we need to log in as the `mcc` user:

```bash
su mcc
```

Fill in your password when asked.

Navigate to the `mcc` user home directory with:

```bash
cd ~
```

Make a new `.ssh` directory:

```bash
mkdir .ssh
```

Enter it with:

```bash
cd .ssh
```

Make a new empty file named `authorized_keys`:

```bash
touch authorized_keys
```

Do no close the Git bash/Terminal emulator.
On your PC, make a new folder where you are going to store your SSH keys that you're going to use to log in to the user account.
Open the folder, and right click on the empty white space (not on files), if you're on Windows click **Git Bash here**, on mac OS and Linux click on **Open Terminal** (or whatever it is called).

Type the following command:

```bash
ssh-keygen -t RSA -b 4096
```

Enter the name of the key file to be: `MCC_Key`, press Enter.

When asked for a `passphrase`, enter a password of your choice and confirm it, make sure it's strong and that you remember it, best if you write it down on a piece of paper.

This will generate a private and a public key that you will use to log in to the VPS as a user that you've created.

Now open the `MCC_Key.pub` file with a text editor of your choice and copy it's contents to the clipboard.

Return to the Git Bash/Terminal emulator and execute the following command:

```bash
sudo apt install nano -y
```

This will install the Nano editor on your VPS.

Now we need to let the SSH service on your VPS know about your newly generated SSH key pair.

Make sure you are in the `/home/mcc/.ssh` folder, you can confirm this by executing:

```bash
pwd
```

If it does not print `/home/mcc/.ssh`, navigate to it with:

```bash
cd /home/mcc/.ssh
```

Now you need to open the `authorized_keys` file with the nano editor:

```bash
nano authorized_keys
```

Now paste the copied contents of the `MCC_Key.pub` into the nano editor by right clicking on it.

Save the file with `CTRL + O`, press Enter, and then exit it with `CTRL + X`.

Now we need to configure the SSHD service to let us login with the SSH key we have generated, for this we need to edit the `/etc/ssh/sshd_config` file with nano:

```bash
sudo nano /etc/ssh/sshd_config
```

Find the `#PubkeyAuthentication yes` line and remove the `#` in front to uncomment the line.

Then find the `#AuthorizedKeysFile .ssh/authorized_keys .ssh/authorized_keys2` line and remove the `#` to uncomment the line.

Additionally for better security you can do the following:

-   Set `PermitRootLogin` to `yes`
-   Change the `Port` to some number of your choice (22-65000) (Make sure it's at least 2 digits and avoid common ports used by other apps like: 21, 80, 35, 8080, 3000, etc...)
-   Uncomment `#PasswordAuthentication yes` by removing the `#` in front and set it to `yes` (This will disable password login, you will be able to login with SSH keys only!)

Save the file with `CTRL + O`, hit Enter, close it with `CTRL + X`.

Now we need to restart the SSHD service with:

```bash
sudo systemctl restart sshd
```

Let's check if everything is working correctly:

```bash
sudo systemctl status sshd
```

If everything has been configured as it should be you should see `active (running)` as a status of the service.
If not, open the config file again and check for mistakes.

Press `q` to exit the log mode.

Logout from the `mcc` user with `exit` command, and then logout from the root `ubuntu` user by typing `exit` again.

Now we can login to the user with our private `MCC_Key` file.

Command:

```bash
ssh -i <path to the MCC_Key private key> mcc@<ip here>
```

Example:

```bash
ssh -i MCC_Key mcc@3.71.108.69
```

> **ℹ️ NOTE: If you've changed the `Port`, make sure you add a `-p <your port here>` option after the `-i <key>` option (eg. `ssh -i MCC_Key -p 8973 mcc@3.71.108.69`)!**

If did everything correctly you should see a Linux prompt and a welcome message if there is one on your provider.

You can do `whoami` to see your username.

Now you can install .NET Core 6 and MCC.

### Installing .NET Core 6

> **ℹ️ NOTE: If your VPS has an ARM CPU, follow [this](#installing-net-on-arm) part of the documentation and then return to section after this one.**

> **⚠️ WARNING: With newer versions of .NET Core 6 on Ubuntu 22.04 you might get the following error: `A fatal error occurred, the folder [/usr/share/dotnet/host/fxr] does not contain any version-numbered child folders`, if you get it, use [this solution](https://github.com/dotnet/sdk/issues/27082#issuecomment-1211143446)**

Log in as the user you've created.

Update the system packages and package manager repositories:

```bash
sudo apt update -y && sudo apt upgrade -y
```

Install `wget`:

```bash
sudo apt install wget -y
```

Go to your home directory with:

```bash
cd ~
```

Download the Microsoft repository file:

```bash
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
```

Add Microsoft repositories to the package manager:

```bash
sudo dpkg -i packages-microsoft-prod.deb
```

Remove the file, we do not need it anymore:

```bash
rm packages-microsoft-prod.deb
```

Finally, install .NET Core 6:

```bash
sudo apt-get update -y && sudo apt-get install -y dotnet-sdk-6.0
```

Run the following command to check if everything was installed correctly:

```bash
dotnet
```

You should get:

```
Usage: dotnet [options]
Usage: dotnet [path-to-application]

Options:
  -h|--help         Display help.
  --info            Display .NET information.
  --list-sdks       Display the installed SDKs.
  --list-runtimes   Display the installed runtimes.

path-to-application:
  The path to an application .dll file to execute.
```

If you do not get this output and the installation was not successful, [try other methods](https://docs.microsoft.com/en-us/dotnet/core/install/linux-ubuntu#2204).

If it was successful, you can now install the MCC.

### Installing MCC on a VPS

Now that you have .NET Core 6.0 and a user account, you should install the `screen` utility, you will need this in order to keep the MCC running once you close down the SSH session (if you do not have it, the MCC will just stop working once you disconnect). You can look at the `screen` like a window, except it's in a terminal, it lets you have multiple "windows" open at the same time.

> **ℹ️ NOTE: There is also a Docker method, if you're using Docker, you do not need the `screen` program.**

You also can learn about the screen command from [this Youtube tutorial](https://youtu.be/_ZJiEX4rmN4).

To install the `screen` execute the following command:

```bash
sudo apt install screen -y
```

Now you can install the MCC:

-   [Download a compiled binary](#download-a-compiled-binary)
-   [Building from the source code](#building-from-the-source-code)
-   [Run using Docker](#using-docker) (Doesn't require the `screen` command)

How to use the `screen` command?

> **⚠️ WARNING: If you have issues with Screen command, like output not being properly formatted or program handing/freezing, try using tmux, click [here](https://www.youtube.com/watch?v=Yl7NFenTgIo) to learn how to use it.**

To start a screen, type:

```bash
screen -S mcc
```

> **ℹ️ NOTE: `mcc` here is the name of the screen, you can use whatever you like, but if you've used a different name, make sure you use that one instead of the `mcc` in the following commands.**

> **ℹ️ NOTE: You need to make a screen only once, however if you reboot your VPS, you need to start it on each reboot.**

Now you will be in the screen, now you can start the MCC and detach from the screen.

To detach from the screen press `CTRL + A + D`.

To re-attach/return to the screen, execute the following command:

```bash
screen -r mcc
```

If you've accidentally closed the SSH session without detaching from the screen it might be still attached, to detach it use:

```bash
screen -d mcc
```

To list out screens you can use:

```bash
screen -ls
```

To stop the MCC, you can hit `CTRL + D` (hit it few times).
