我的世界控制台客户端
========================

[![Appveyor build status](https://ci.appveyor.com/api/projects/status/github/ORelio/Minecraft-Console-Client?branch=Indev)](https://ci.appveyor.com/project/ORelio/minecraft-console-client)

我的世界控制台客户端(MCC)是一个轻量级的程序，它允许你连接至任何我的世界服务器，
简单快速地发送指令和接收聊天信息而不需要开启游戏。它也提供了多种自动化管理服务器和进行其他操作的可能性。

**注意！** MCC仅可以连接到**我的世界Java版**，而**不能连接到我的世界基岩版/中国版！**

## 正在寻找维护者

由于不再有足够的时间来为新的我的世界版本提供升级和修复错误，开发者正在寻找有开发动力的人来接手该项目。如果您认为您可以接手该项目，请查看 [issues](https://github.com/MCCTeam/Minecraft-Console-Client/issues?q=is%3Aissue+is%3Aopen+label%3Awaiting-for%3Acontributor) 部分 :)

## 下载

从最新的[开发构建](https://ci.appveyor.com/project/ORelio/minecraft-console-client/build/artifacts)处获取exe文件。
这是一个同样兼容于macOS以及Linux的.NET可执行文件。

## 如何使用

在此查看[示例配置文件](MinecraftClient/config/) ，其中有基础使用教程 README 文件。<br>
更多帮助和信息可以从[我的世界官方论坛](http://www.minecraftforum.net/topic/1314800-/)中查询。

## 从原代码编译

首先，下载[原代码zip压缩包](https://github.com/MCCTeam/Minecraft-Console-Client/archive/master.zip)，将其解压并进入`MinecraftClient`文件夹

编辑 `MinecraftClient.csproj` 的[第四行](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/MinecraftClient.csproj#L4)，将编译目标设置为 `Release`:

```xml
<Configuration Condition=" '$(Configuration)' == '' ">Release</Configuration>
```

### 在Windows环境下

1. 找到 `C:\Windows\Microsoft.NET\Framework\v4.X.XXXXX` 下的 `MSBuild.exe`
2. 将 `MinecraftClient.csproj` 拖到 `MSBuild.exe` 上方并放开以开始编译
3. 如果编译成功，您将可以在 `MinecraftClient\bin\Release` 路径下找到 `MinecraftClient.exe`

### 在macOS/Linux环境下

1. 安装[Mono Framework](https://www.mono-project.com/download/stable/#download-lin)
2. 在终端内执行 `msbuild MinecraftClient.csproj`
3. 如果编译成功，您将可以在 `MinecraftClient\bin\Release` 路径下找到 `MinecraftClient.exe`

## 贡献代码

如果您希望为我的世界控制台客户端出一份力的话，我们不胜感激，您可以fork此repo并提交合并请求。 *Indev* 分支将不会继续被使用, 我们将只会把MCC作为测试版软件发布。

## 许可证

除非有特殊说明，此项目代码全部来自MCC开发者，并以CDDL-1.0协议发布。
在其他情况下，许可证和原作者会被提及于源码文件的顶部。
CDDL-1.0许可证的主要条件基本上在列明于下列：

- 你可以在任何一个程序使用许可证编码不管是使用完整的或一部分，程序的许可证是处于完整（或者相当的，不包括你借用的编码）。程序本身可以使开放来源或是封闭来源，自由的或商业的。
- 无论如何，在CDDL编码（在CDDl编码里被任何编码引用直接修改会被认为是增建部分于CDDL编码里，所以是被限制于这需求；列子：对math fuction的改进使用快速查阅资料表会让资料表被认为是个增建部分，不管这是否在自己本身的来源编码之中）里，所有案列例如任何修改，改进，或者是增建部分必须使其公开的和自由的在来源中，当然也被限制于CDDL许可证里。
- 在任何程序（来源或二进制）使用CDDL编码，确认必须要被给于CDDl编码的来源（任何一个项目或作者）。同样的，对CDDL编码（必须分布作为来源）的改进不得移除作为指引来源编码的通知。

更多资讯在 http://qstuff.blogspot.fr/2007/04/why-cddl.html<br>
完整许可证在 http://opensource.org/licenses/CDDL-1.0
