# MCC 記憶體優化與漏洞修復說明文件 / Memory Optimization & Bug Fixes Documentation

本文件詳細記錄了針對 **Minecraft Console Client (MCC)** 所進行的全面性架構清理、歷史 Bug 修復、記憶體洩漏處理與高頻零分配優化。所有改動均通過 1 小時實體記憶體耐久測試（Zero Memory Leak）與 66 項全量單元測試，並維持 100% 向後相容性。

---

## 📊 1 小時高負載測試圖表與驗證成果 (1-Hour Endurance Test Chart & Results)

在模擬 Minecraft 20 TPS 真實高負載環境下連續運行 1 小時（3,600 秒 / 57,201 次迭代，每 30 秒取樣共 121 筆數據點），完整記憶體趨勢圖與統計數據如下：

<div align="center">
  <img src="docs/memory_test_1hour_chart.svg" alt="MCC 1-Hour Memory Leak Endurance Test Chart" width="100%"/>
</div>

### 數據統計表 (Summary Metrics Table)

| 指標項 (Metric) | 初始 (0m) | 15m | 30m | 45m | 結束 (60m) | 平均值 (Avg) | 峰值 (Max) | 趨勢判定 (Trend) |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :--- |
| **實體記憶體 (WorkingSet / RAM)** | 44.33 MB | 52.25 MB | 52.53 MB | 50.85 MB | **47.36 MB** | **51.37 MB** | 55.83 MB | 水平穩定波動 (~50 MB) |
| **私有提交記憶體 (Private Memory)** | 30.46 MB | 30.29 MB | 28.43 MB | 26.86 MB | **23.57 MB** | **28.81 MB** | 32.98 MB | 淨下降 **-6.89 MB** (零累積) |
| **.NET 託管堆積 (GC Managed Heap)** | 1.63 MB | 8.64 MB | 8.35 MB | 13.70 MB | **4.19 MB** | **8.97 MB** | 15.56 MB | 規律鋸齒狀回收 (零孤島物件) |
| **垃圾回收累計 (GC Gen 0 / 1 / 2)** | 0 / 0 / 0 | 494 / 124 / 111 | 1001 / 253 / 225 | 1509 / 382 / 339 | **2032 / 516 / 458** | — | — | 均勻分佈，無長延遲停頓 |

```mermaid
xychart-beta
    title "MCC 1-Hour (60 min) Memory Usage Trend (MB)"
    x-axis ["0m", "5m", "10m", "15m", "20m", "25m", "30m", "35m", "40m", "45m", "50m", "55m", "60m"]
    y-axis "Memory (MB)" 0 --> 65
    line "WorkingSet (RAM)" [44.3, 53.6, 50.9, 52.3, 50.1, 52.8, 52.5, 51.3, 51.1, 50.9, 50.4, 54.1, 47.4]
    line "Private Memory" [30.5, 32.8, 29.9, 30.3, 27.6, 29.7, 28.4, 27.3, 27.1, 26.9, 25.9, 29.6, 23.6]
    line "GC Managed Heap" [1.6, 13.1, 2.7, 8.6, 4.9, 4.7, 8.4, 7.6, 5.2, 13.7, 13.5, 5.1, 4.2]
```

### 💡 實體記憶體 (WorkingSet) 與 私有記憶體 (Private Memory) 的差別

1. **私有提交記憶體 (Private Memory / Private Bytes) —— 平均 `28.81 MB`（結束時 `23.57 MB`）**：
   * **定義**：只有 MCC 這個程式「自己獨佔」申請的記憶體（包含地图區塊資料、方塊座標、背包物品、實體清單、封包緩衝區等）。
   * **為什麼最重要**：這是**判斷有沒有記憶體洩漏（Memory Leak）最準確的指標**，而且在多開 Bot 時，每多開一隻 Bot 只會額外增加這 `~25–28 MB` 的私有記憶體。測試 1 小時後從 `30.46 MB` 降至 `23.57 MB`，證明完全沒有任何記憶體洩漏。
2. **實際工作集記憶體 (Working Set / Physical RAM) —— 平均 `51.37 MB`**：
   * **定義**：`私有記憶體 (~28 MB)` ＋ `作業系統與 .NET 執行環境共用函式庫 (~23 MB，如 System.Net.Sockets.dll 等)`。
   * **多開優勢**：那 `~23 MB` 的系統共用區塊在整台 VPS 上**所有程式只會載入並共享同一份**，不會隨多開數量倍增。

---

## 🛠️ 詳細改動清單 (Detailed Changelog)

### 1. 記憶體洩漏與資源生命週期修復 (Memory Leaks & Disposal)

* **`MinecraftClient/Protocol/Handlers/Protocol18.cs`**:
  * 斷線與物件釋放時，補齊 `netMain` 與 `netReader` 執行緒的 `CancellationTokenSource` 釋放（`Cancel()` 及 `Dispose()`）。
  * 針對封包佇列 `packetQueue` (`BlockingCollection`) 補上 `CompleteAdding()` 與 `Dispose()`，杜絕底層 `SemaphoreSlim` 與 WaitHandle 系統資源殘留。
* **`MinecraftClient/McClient.cs`**:
  * 登入失敗重試（`Retry:` 標籤）、超時與中斷連線時，確實處置 `timeoutdetector` 的 `CancellationTokenSource`。
  * 斷線時確實釋放 `client` (TcpClient) 與 `handler` (ProtocolHandler)。
* **`MinecraftClient/WinAPI/ConsoleIcon.cs`**:
  * 修復 Windows GDI 句柄洩漏：使用 `using` 包裹各類 Bitmap/Stream，並在 `finally` 區塊加入 Win32 `DestroyIcon(hIcon)`，解決控制台圖示更新時累積 GDI Handle 的歷史問題。
* **`MinecraftClient/TaskWithResult.cs`**:
  * 內部同步句柄 `ManualResetEventSlim` 於 `WaitGetResult()` 的 `finally` 區塊立即處置，防止短命排程任務等待 GC 堆積。
* **`MinecraftClient/ChatBots/` (DiscordBridge, TelegramBridge, Mailer, FileMonitor)**:
  * `DiscordBridge.cs` & `TelegramBridge.cs`：在 `OnUnload()` 與 `Disconnect()` 時將全域靜態單例 `instance` 重設為 `null`，避免 Bot 卸載後整份實例依然被全域靜態參考牽住無法 GC；定時器確實執行 `Dispose()`。
  * `Mailer.cs`：在 `OnUnload()` 時確實釋放資料庫檔案監聽器 `mailDbFileMonitor?.Dispose()`。
  * `FileMonitor.cs`：在 `Dispose()` 時解除 `monitor.Changed -= handler` 事件訂閱並停止輪詢 Task。
* **串流與密碼學資源處置**:
  * `AesCfb8Stream.cs`：覆寫 `Dispose(bool disposing)` 確保底層 `Aes` 密碼編譯提供者及時關閉。
  * `ChatLog.cs`, `FileLogLogger.cs`, `CryptoHandler.cs`, `TuiConsoleBackend.cs`：所有臨時 Stream 與 BinaryReader/Writer 全面補齊 `using` 宣告。

---

### 2. 高頻迴圈（20 TPS / 網路封包 / 方塊遍歷）零分配優化

* **網路封包接收與 VarInt 解析**:
  * **`SocketWrapper.cs`**: 新增 `ReadByteRAW()` 複用單字節緩衝區，杜絕讀取封包長度 VarInt 時每次配置 `new byte[1]` 的 GC 壓力。
  * **`Protocol18.cs` & `ZlibUtils.cs`**: 封包解壓縮改採 byte array 切片傳入 offset/count，廢除舊版先建立 `Queue<byte>` 再 `.ToArray()` 的雙重複製；移除了正常封包接收時無條件執行的 `packetData.ToArray()` 複製。
  * **`AesCfb8Stream.cs`**: 解密迴圈採用 `ArrayPool<byte>.Shared` 租用緩衝區，長連線接收時零重複位元組陣列配置。
* **20 TPS Tick 迴圈**:
  * **`McClient.cs` (OnUpdate)**: 廢除原本每 Tick 檢查過期藥水效果的 LINQ 查詢（`.Where(...).Select(...).ToList()`），改為零分配 foreach 條件遍歷。
  * **`McClient.cs` (IsSignMaterial)**: 告示牌材質判定由 `material.ToString().Contains("Sign")` 替換為常數 switch 表達式，消除每秒字串轉換配置。
* **地圖與方塊遍歷**:
  * **`Chunk.cs` & `Protocol18Terrain.cs`**: 新增 `public void FillWithBlock(Block block)`，在單一材質區塊時利用 `Array.Fill` 取代 4,096 次巢狀迴圈與邊界檢查，解碼速度大幅提升。
  * **`World.cs` (FindBlock)**: 廢除舊版 LINQ 笛卡爾積 (`Range.SelectMany...`) 建立龐大臨時 List 的作法，改為原地 3D 空間遍歷與即時過濾。
  * **`Material2Tool.cs` & `ItemTypeExtensions.cs`**: 工具等級分類與物品屬性列表改以 .NET `FrozenSet` 靜態唯讀快取，查詢複雜度自 $O(N)$ 降至 $O(1)$，每次呼叫零堆積分配。
* **反向調色盤快取 (Palettes)**:
  * **`ItemPalette.cs` & `PacketTypePalette.cs`**: 將原本每次連線/重連皆為實例重新建立數千條項目的反向查表字典，改為型別快取的跨實例共用靜態字典 (`ConcurrentDictionary<Type, ...>`)，大幅減少重複連線時的記憶體分配。
* **腳本編譯 LRU 邊界**:
  * **`CSharpRunner.cs`**: 動態腳本編譯快取加入上限（128 筆）與 LRU 淘汰機制，防止重複執行外部腳本導致記憶體無限膨脹。
  * **`DynamicRun/Builder/Compiler.cs`**: 靜態快取 `GetBaseReferences()`，避免重複 Roslyn 編譯時反覆掃描檔案系統。

---

### 3. 移動、尋路與地圖畫 Bot 核心邏輯加固

* **修復 `GetCurrentMovementGoal()` 歷史 Bug**:
  * `McClient.cs`：原版程式碼存在 `(ClientIsMoving() || path is null) ? Location.Zero : path.Last()` 的邏輯顛倒問題（移動中反而回傳 `Location.Zero`）。現已修正為：移動中優先回傳 `path.Last()` 或單步目標 `pathTarget!.Value`，未移動才回傳 `Location.Zero`。
* **修復 `ClientIsMoving()` 單步路徑提早判定結束**:
  * 原先物理巡路每彈出一個節點置於 `pathTarget`，當目的地僅有 1 步時，`path.Count` 立即為 0，導致 Bot 提早誤判移動結束。新版全面納入 `pathTarget is not null` 判定，確保走到定位前維持 Moving 狀態。
* **`CancelMovement()` 清理強化**:
  * 取消移動時同步清理 `pathTarget = null` 並呼叫 `physicsInput.Reset()`。
* **自動農場與物品收集防卡死守衛 (`Farmer.cs`, `ItemsCollector.cs`)**:
  * 在移動與挖掘等待迴圈加入 `running && ClientIsMoving()` 前置檢查，防止 Bot 卸載或卡在障礙物時無窮迴圈鎖死工作執行緒。
* **地圖存檔防抖重試 (`Map.cs`)**:
  * 修復存檔完成未 `break` 跳出重試迴圈的邏輯瑕疵，並在併發 I/O 時加入非阻塞等待重試，大幅提升批量導出地圖畫時的檔案系統穩定性。

---

## ✅ 相容性保證 (Compatibility Guarantees)
1. **指令相容**：無刪除任何既有指令類別，指令補全與解析 100% 相同。
2. **設定相容**：`MinecraftClient.ini` 與相關 TOML 設定項解析 100% 原樣支援。
3. **API 相容**：`ChatBot`、`Scripting` 與 `McClient` 公開方法簽章未做任何破壞性變更。
4. **協定支援**：1.4.6 至 1.21.x（共 52 個 Minecraft 通訊協定版本）完整保留。
5. **單元測試**：包含全新調色盤與移動回歸測試在內的 66/66 項單元測試全數通過。
