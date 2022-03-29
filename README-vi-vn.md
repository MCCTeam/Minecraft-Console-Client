Minecraft Console Client
========================

[![GitHub Actions build status](https://github.com/MCCTeam/Minecraft-Console-Client/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/MCCTeam/Minecraft-Console-Client/releases/latest)

Minecraft Console Client (MCC) là một ứng dụng nhẹ cho phép bạn kết nối tới bất kì máy chủ Minecraft nào, gửi lệnh và nhận tin nhắn bằng một cách nhanh và dễ dàng mà không cần phải mở Minecraft. Nó cũng cung cấp nhiều hoạt động tự động mà bạn có thể bật để quản lý và nhiều mục đích khác.

## Tải 🔽

Tải binary ngay tại [đây](https://github.com/MCCTeam/Minecraft-Console-Client/releases/latest).
File exe là .NET binary nên cũng có thể chạy được trên MacOS và Linux

## Hướng dẫn sử dụng 📚

Hãy xem [file cài đặt mẫu](MinecraftClient/config/) có bao gồm hướng dẫn sử dụng [README](https://github.com/MCCTeam/Minecraft-Console-Client/tree/master/MinecraftClient/config#minecraft-console-client-user-manual).

## Hỗ trợ 🙋

Hãy xem [README](https://github.com/MCCTeam/Minecraft-Console-Client/tree/master/MinecraftClient/config#minecraft-console-client-user-manual) và [Thảo luận](https://github.com/MCCTeam/Minecraft-Console-Client/discussions): Có thể câu hỏi của bạn sẽ được trả lời ở đây. Nếu không, hãy mở một [cuộc thảo luận mới](https://github.com/MCCTeam/Minecraft-Console-Client/discussions/new) và hỏi câu hỏi của bạn. Nếu bạn tìm thấy lỗi, hãy báo cáo chúng ở [khu vực vấn đề](https://github.com/MCCTeam/Minecraft-Console-Client/issues).

## Giúp đỡ chúng tôi ❤️

Chúng tôi là một cộng đồng nhỏ nên chúng tôi cần giúp đỡ để cài đặt cập nhật cho những phiên bản mới hơn, sửa lỗi trong ứng dụng và mở rộng kế hoạch. Chúng tôi luôn tìm kiếm những người có động lục để đóng góp. Nếu bạn nghĩ đó có thể là bạn, hãy xem phần [issues](https://github.com/MCCTeam/Minecraft-Console-Client/issues?q=is%3Aissue+is%3Aopen+label%3Awaiting-for%3Acontributor) :)

## Làm thế nào để đóng góp? 📝

Nếu bạn cảm thấy thích đóng góp cho Minecraft Console Client, tuyệt vời, chỉ cần fork repository này và nộp 1 cái pull request trên nhánh *Master*. MCC đang được phân phối bằng những phiên bản development (thường là ổn định) và chúng tôi không còn sử dụng nhánh *Indev*.

## Dịch Minecraft Console Client 🌍

Nếu bạn muốn dịch Minecraft Console Client bằng một ngôn ngữ khác, hãy tải file mẫu này [ở đây](https://github.com/MCCTeam/Minecraft-Console-Client/tree/master/MinecraftClient/Resources/lang) hoặc chỉ cần fork repository này. Khi bạn xong với việc dịch, nộp 1 cái pull request hoặc gửi cho chúng tôi tại [khu vực vấn đề](https://github.com/MCCTeam/Minecraft-Console-Client/issues).

Để sử dụng file dịch, hãy để nó trong thư mục `lang/mcc/` và chỉnh ngôn ngữ của bạn trong file `.ini` config. Bạn có thể tạo thư mục đó nếu không có.

Để xem hướng dẫn đặt tên, hãy xem [bình luận này](https://github.com/MCCTeam/Minecraft-Console-Client/pull/1282#issuecomment-711150715).

## Xây từ gốc 🏗️

_The recommended development environment is [Visual Studio](https://visualstudio.microsoft.com/). If you want to build the project without installing a development environment, you may also follow these instructions:_

First of all, get a [zip of source code](https://github.com/MCCTeam/Minecraft-Console-Client/archive/master.zip), extract it and navigate to the `MinecraftClient` folder.

Edit `MinecraftClient.csproj` to set the Build target to `Release` on [line 4](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/MinecraftClient.csproj#L4):

```xml
<Configuration Condition=" '$(Configuration)' == '' ">Release</Configuration>
```

### On Windows 🪟

1. Locate `MSBuild.exe` for .NET 4 inside `C:\Windows\Microsoft.NET\Framework\v4.X.XXXXX`
2. Drag and drop `MinecraftClient.csproj` over `MSBuild.exe` to launch the build
3. If the build succeeds, you can find `MinecraftClient.exe` under `MinecraftClient\bin\Release`

### On Mac and Linux 🐧

1. Install the [Mono Framework](https://www.mono-project.com/download/stable/#download-lin) if not already installed
2. Run `msbuild MinecraftClient.csproj` in a terminal
3. If the build succeeds, you can find `MinecraftClient.exe` under `MinecraftClient\bin\Release`

## License ⚖️

Unless specifically stated, the code is from the MCC Team or Contributors, and available under CDDL-1.0. Else, the license and original author are mentioned in source file headers.
The main terms of the CDDL-1.0 license are basically the following:

- You may use the licensed code in whole or in part in any program you desire, regardless of the license of the program as a whole (or rather, as excluding the code you are borrowing). The program itself may be open or closed source, free or commercial.
- However, in all cases, any modifications, improvements, or additions to the CDDL code (any code that is referenced in direct modifications to the CDDL code is considered an addition to the CDDL code, and so is bound by this requirement; e.g. a modification of a math function to use a fast lookup table makes that table itself an addition to the CDDL code, regardless of whether it's in a source code file of its own) must be made publicly and freely available in source, under the CDDL license itself.
- In any program (source or binary) that uses CDDL code, recognition must be given to the source (either project or author) of the CDDL code. As well, modifications to the CDDL code (which must be distributed as source) may not remove notices indicating the ancestry of the code.

More info at http://qstuff.blogspot.fr/2007/04/why-cddl.html
Full license at http://opensource.org/licenses/CDDL-1.0
