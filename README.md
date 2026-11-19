## 記憶體優化與修改說明

這個版本主要針對長期掛機與小記憶體 VPS 環境做了清理，修掉幾個沒釋放資源導致的記憶體洩漏，並減少封包讀取與 20 TPS 更新迴圈裡的無謂物件分配。原版的協定版本、指令、設定檔與腳本 API 均維持原樣沒有刪減。

### 1 小時記憶體測試 (20 TPS, 3600s)

連續跑 1 小時（每 30 秒紀錄一次，共 57,201 次迭代）的記憶體紀錄：

![Memory Usage Over 1 Hour](docs/memory_test_1hour_chart.svg)

| 項目 | 初始 (0m) | 15m | 30m | 45m | 結束 (60m) | 平均 | 最高 |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| Working Set (實際佔用 RAM) | 44.33 MB | 52.25 MB | 52.53 MB | 50.85 MB | 47.36 MB | 51.37 MB | 55.83 MB |
| Private Memory (私有記憶體) | 30.46 MB | 30.29 MB | 28.43 MB | 26.86 MB | 23.57 MB | 28.81 MB | 32.98 MB |
| GC Heap (.NET 託管堆積) | 1.63 MB | 8.64 MB | 8.35 MB | 13.70 MB | 4.19 MB | 8.97 MB | 15.56 MB |

* **Private Memory (平均約 28.8 MB)**：程式自己獨佔的記憶體（區塊資料、方塊、背包、實體等）。跑滿 1 小時後從 30.46 MB 降到 23.57 MB，沒有隨時間往上疊加。多開時每多開一隻 bot 大約就是增加這 25~28 MB。
* **Working Set (平均約 51.4 MB)**：私有記憶體加上系統與 .NET Runtime 共用函式庫（約 23 MB）。共用函式庫在系統裡所有行程會共用同一份，不會因為多開而重複佔用。

### 修改內容

* **資源釋放與記憶體洩漏**
  * `Protocol18.cs` / `McClient.cs`：斷線與重連時釋放 `netMain`、`netReader`、`timeoutdetector` 的 `CancellationTokenSource`，並對 `packetQueue` (`BlockingCollection`) 呼叫 `CompleteAdding()` 與 `Dispose()`。
  * `ConsoleIcon.cs` / `TaskWithResult.cs`：補上 Win32 `DestroyIcon(hIcon)` 避免 GDI handle 累積；`ManualResetEventSlim` 在 `WaitGetResult()` 結束時直接 `Dispose()`。
  * `DiscordBridge.cs` / `TelegramBridge.cs` / `Mailer.cs` / `FileMonitor.cs`：Bot 卸載或斷線時清掉靜態 `instance` 參考、解除 `FileSystemWatcher` 事件訂閱並釋放 Timer。
  * `AesCfb8Stream.cs` / `ChatLog.cs` / `FileLogLogger.cs` / `CryptoHandler.cs`：補齊 Stream 與 AES 物件的 `Dispose` / `using`。
* **減少重複記憶體分配**
  * `SocketWrapper.cs` / `AesCfb8Stream.cs`：讀 VarInt 時共用單 byte 緩衝區，不再每次 `new byte[1]`；AES 解密改用 `ArrayPool<byte>.Shared`。
  * `Protocol18.cs` / `ZlibUtils.cs`：解壓縮直接傳 `byte[]` 的 offset 與 length，拿掉原本先轉 `Queue<byte>` 再 `.ToArray()` 的重複複製。
  * `McClient.cs` / `World.cs` / `Chunk.cs`：把 `OnUpdate` 藥水檢查與 `FindBlock` 裡的 LINQ 改寫為一般迴圈；單一材質區塊改用 `Array.Fill` (`FillWithBlock`)；工具與材質查表改用 `FrozenSet`。
  * `ItemPalette.cs` / `PacketTypePalette.cs` / `CSharpRunner.cs`：反向調色盤改為靜態共用字典，重連時不用重建；C# 腳本編譯快取加上 128 筆上限（LRU）。
* **移動與尋路問題修正**
  * `McClient.cs`：修正 `GetCurrentMovementGoal()` 條件寫反（原本移動中反而回傳 `Location.Zero`）的問題；修正 `ClientIsMoving()` 在只剩最後一步 (`pathTarget != null`) 時提早判定為停止的問題。
  * `Farmer.cs` / `ItemsCollector.cs` / `Map.cs`：等待移動的迴圈加上 `running && ClientIsMoving()` 檢查避免卡死；`Map.cs` 存檔成功後補上 `break` 跳出重試迴圈。

---

<div align="center">

<img src="https://i.pics.rs/LLDhE.png" alt="Logo"/>

# Minecraft Console Client (MCC)

[Documentation](https://mccteam.github.io/) | [Download](#download) | [Installation](https://mccteam.github.io/guide/installation.html) | [Configuration](https://mccteam.github.io/guide/configuration.html) | [Usage](https://mccteam.github.io/guide/usage.html)

</div>

<div align="center">

[English](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/README.md) | [Nederlands](https://github.com/MCCTeam/MCCTeam.github.io/blob/master/MCC-README/README-Dutch.md) | [Русский](https://github.com/MCCTeam/MCCTeam.github.io/blob/master/MCC-README/README-Russian.md) | [Српски](https://github.com/MCCTeam/MCCTeam.github.io/blob/master/MCC-README/README-Serbian_Cyrillic.md) | [Türkçe](https://github.com/MCCTeam/MCCTeam.github.io/blob/master/MCC-README/README-Turkish.md) | [Tiếng Việt](https://github.com/MCCTeam/MCCTeam.github.io/blob/master/MCC-README/README-Vietnamese.md) | [简体中文](https://github.com/MCCTeam/MCCTeam.github.io/blob/master/MCC-README/README-Chinese_Simplified.md) | [繁體中文](https://github.com/MCCTeam/MCCTeam.github.io/blob/master/MCC-README/README-Chinese_Traditional.md)

</div>

<div align="center">

[![GitHub Actions build status](https://github.com/MCCTeam/Minecraft-Console-Client/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/MCCTeam/Minecraft-Console-Client/releases/latest) <a href="https://discord.gg/sfBv4TtpC9"><img src="https://img.shields.io/discord/1018553894831403028?color=5865F2&logo=discord&logoColor=white" alt="Discord server" /></a>

</div>

> [!IMPORTANT]
> ## 📢 Major News
>
> After 13 years of active development, we are rewriting MCC from the ground up to make development faster and enable powerful new features. The rewrite is currently being led by [@milutinke](https://github.com/milutinke). Along the refactoring, the client is being polished, the UX and UI are being improved, new features are being added and existing ones improved. ✨
>
> **Update (10.10.2026)** The early access build (No. 12) for the 2.0 client is available for download: [here](https://github.com/MCCTeam/Minecraft-Console-Client/releases/tag/v2.0.0-early-access.12)
>
> Please leave feedback on Discord.
>
> After the release of the Minecraft Console Client 2.0, the old client will remain available to use but will no longer be updated. The new client is expected for public testing in the coming weeks. 🚀
>
> Follow progress in [#🚀refactor-updates](https://discord.com/channels/1018553894831403028/1543210248439992461) on our [Discord server](https://discord.gg/sfBv4TtpC9)!

## **About ℹ️**

**Minecraft Console Client (MCC)** is a lightweight cross-platform open-source Minecraft TUI client for **Java** edition that allows you to connect to any Minecraft Java server, send commands and receive text messages in a fast and easy way without having to open the main Minecraft game.

## Download

Get the latest release from the [Releases section](https://github.com/MCCTeam/Minecraft-Console-Client/releases/latest).

## Quick Install ⚡

Open a terminal in the folder where you want MCC and run:

Linux / macOS:

```bash
curl -fsSL https://mccteam.github.io/install.sh | sh
```

Windows (PowerShell):

```powershell
iwr -useb https://mccteam.github.io/install.ps1 | iex
```

The script detects your architecture and downloads the right binary. For more options (including `wget` and manual downloads), see the [installation guide](https://mccteam.github.io/guide/installation.html).

## How to use 📚

-   🌐 [Full Documentation](https://mccteam.github.io/)
-   📦 [Installation](https://mccteam.github.io/guide/installation.html)
-   📖 [Usage](https://mccteam.github.io/guide/usage.html)
-   ⚙️ [Configuration](https://mccteam.github.io/guide/configuration.html)
-   🤖 [Chat Bots](https://mccteam.github.io/guide/chat-bots.html)
-   📝 [Sample configuration files](MinecraftClient/config/)

## Getting Help 🙋

Check out the [Website](https://mccteam.github.io/), [README](https://github.com/MCCTeam/Minecraft-Console-Client/tree/master/MinecraftClient/config#minecraft-console-client-user-manual) and existing [Discussions](https://github.com/MCCTeam/Minecraft-Console-Client/discussions): Maybe your question is answered there. If not, please open a [New Discussion](https://github.com/MCCTeam/Minecraft-Console-Client/discussions/new) and ask your question. If you find a bug, please report it in the [Issues](https://github.com/MCCTeam/Minecraft-Console-Client/issues) section.

## Discord

We now have a Discord server, click [here](https://discord.gg/sfBv4TtpC9) to join.

## Helping Us ❤️

We are a small community so we need help to implement upgrades for new Minecraft versions, fixing bugs and expanding the project. We are always looking for motivated people to contribute. If you feel like it could be you, please have a look at the [issues](https://github.com/MCCTeam/Minecraft-Console-Client/issues?q=is%3Aissue+is%3Aopen+label%3Awaiting-for%3Acontributor) section :)

## How to contribute 📝

If you'd like to contribute to Minecraft Console Client, great, just fork the repository and submit a pull request on the _Master_ branch. To contribute to the website / online documentation see also the [Website repository](https://github.com/MCCTeam/MCCTeam.github.io).

![Alt](https://repobeats.axiom.co/api/embed/c8a6c7c47fde8fcbe3727a21eab46e6b39dff60d.svg "Repobeats analytics image")

## Translating Minecraft Console Client 🌍

To improve translations for MCC, please visit: [Crowdin - Minecraft Console Client](https://crowdin.com/project/minecraft-console-client).

## Building from the source 🏗️

This section has been moved to our new [Documentation website](https://mccteam.github.io/guide/installation.html#building-from-the-source-code).

## Community projects 🧩

Tools built by the community around MCC. They are not maintained by the MCC Team, report issues in their own trackers.

- **[MCC GUI V2](https://github.com/AnonBOTpl/MCC-GUI-V2)** > Windows 11 desktop app that runs many accounts at once: one MCC process per connection, a tab with a live colored log for each account, and chat or commands sent to one account or to all of them. It does not bundle or modify MCC, it starts the `MinecraftClient.exe` you point it at, and it works with MCC 1.x. Free and open source (MIT).

## License ⚖️

Unless specifically stated, the code is from the MCC Team or Contributors, and available under CDDL-1.0. Else, the license and original author are mentioned in source file headers.
The main terms of the CDDL-1.0 license are basically the following:

-   You may use the licensed code in whole or in part in any program you desire, regardless of the license of the program as a whole (or rather, as excluding the code you are borrowing). The program itself may be open or closed source, free or commercial.
-   However, in all cases, any modifications, improvements, or additions to the CDDL code (any code that is referenced in direct modifications to the CDDL code is considered an addition to the CDDL code, and so is bound by this requirement; e.g. a modification of a math function to use a fast lookup table makes that table itself an addition to the CDDL code, and so is bound by this requirement; e.g. a modification of a math function to use a fast lookup table makes that table itself an addition to the CDDL code, regardless of whether it's in a source code file of its own) must be made publicly and freely available in source, under the CDDL license itself.
-   In any program (source or binary) that uses CDDL code, recognition must be given to the source (either project or author) of the CDDL code. As well, modifications to the CDDL code (which must be distributed as source) may not remove notices indicating the ancestry of the code.

More info at http://qstuff.blogspot.fr/2007/04/why-cddl.html
Full license at http://opensource.org/licenses/CDDL-1.0

## Uses technologies from
<div align="center">
<a href="https://sentry.io/welcome/">
  <img src="https://github.com/breadbyte/Minecraft-Console-Client/assets/14045257/411e9a2f-cd9b-4bb5-b7e9-cd7529c76b88" alt="Sentry"  />
</a>
</div>
