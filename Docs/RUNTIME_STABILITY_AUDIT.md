# 執行效能與程式穩定性盤點

盤點日期：2026-10-12

專案版本：Unity 6000.3.26f1（Unity 6.3 LTS）

盤點基準：`main` / `a115ca5`

## 結論

目前版本可作為後續優化基準：23 個 Build Settings 場景皆可載入，主要導覽、四關入口與全關完成後進入結局的 4 項 PlayMode 測試全部通過。

下一階段應先處理「流程可重複觸發」與「每幀物件搜尋」，再做 Mac 16:10 畫面調整及實機 Profiler 量測。這個順序可以先降低偶發錯誤，且不必先改變角色手感或關卡內容。

## 已驗證基準

- PlayMode：4/4 通過，0 failed，執行時間 25.56 秒。
- 場景：23/23 可載入，載入期間沒有未預期的 Warning、Error 或 Exception。
- 流程：開始畫面 → 開場 → 主選單通過。
- 流程：主選單四個關卡按鈕皆可到達正確教學場景。
- 流程：四關完成後可到達結局場景，且完成狀態會清除。
- 程式規模：`Assets/Scripts` 約 3,240 行；20 個 `Update`、7 個 `FixedUpdate`。

測試注意事項：Unity 6.3 + URP 在 `-nographics` 載入本專案 2D 場景時會於 Sprite batching 原生渲染流程崩潰。使用一般圖形裝置的 `-batchmode` 可正常完成測試；後續自動測試不應加上 `-nographics`。

## 優先處理項目

### P1：避免同一流程被排程多次

影響：可能重複播放音效、重複顯示失敗視窗、重複切換場景，或在低幀率與碰撞停留期間累積多個 `Invoke`。

- `player.OnTriggerStay2D` 在角色停留於門或 Boss 區域時，每個物理更新都可能再次排程 `GotoMenu`、`NextScene` 或 `Lose`。
- `player.Hpfunction` 與 `ship.Hpfunction` 在 HP 小於等於 0 時，每幀都可能再次排程 `Lose`。
- `caker.Hpfunction` 在 Boss HP 等於 0 時，每幀都會再次排程 `Escape` 與 `Appear`。
- `ship4.Bigger` 在每個 0.5 秒時間區間內會連續播放音效、播放動畫並排程多次狀態切換。
- `levelLoader` 沒有「正在載入」鎖定；快速連按可同時啟動多個場景切換 coroutine，也沒有場景索引範圍檢查。

建議修法：為死亡、過關、Boss 退場、計時階段與場景載入各加一次性旗標；切換開始後忽略後續請求；載入前驗證索引。

### P1：移除每幀名稱搜尋

影響：產生不必要的 CPU 工作，也讓物件改名或暫時不存在時直接發生 `NullReferenceException`。

- `pass1`、`pass2`、`pass3`、`pass4` 每幀呼叫 `GameObject.Find(...).GetComponent<...>()`。
- `pass.Update` 每幀對四個完成圖示重複呼叫 `SetActive`，即使狀態沒有改變。

建議修法：在 `Start`/`Awake` 快取玩家元件；只有完成狀態改變時更新圖示。更完整的做法是由關卡完成事件直接寫入進度，移除四個輪詢腳本。

### P1：修正計時器的浮點數結束條件

`startTimer` 使用 `timeLeft == 0f`。以 `Time.deltaTime` 遞減時通常會從正值直接跨到負值，因此結束條件可能永遠不成立。

建議修法：改為 `timeLeft <= 0f`，並把數值限制為 0；結束動作只執行一次。

### P1：清除跨場景暫態狀態

`player.isInvincible`、`ship.isInvincible`、`player.hp`、`caker.hp` 與四關完成狀態使用 static 欄位。部分欄位會在 `Start` 重設，但無敵狀態沒有明確在新關卡重設；例外退出或重新開始可能把狀態帶到下一次遊戲。

建議修法：把只屬於單一場景的狀態改為 instance 欄位，或在關卡啟動與離開時集中重設。四關完成進度可保留跨場景，但應由一個明確的進度元件管理。

### P2：降低場景編號耦合

主選單、結局與多個角色腳本直接使用 `0`、`2`、`9`、`11`、`13`、`18`、`22` 等場景編號。只要在 Build Settings 插入或調整場景，功能就可能導向錯誤場景。

建議修法：先建立集中式 `SceneIds` 常數，測試也共用同一份定義；後續可改用場景名稱或 ScriptableObject 場景目錄。

### P2：Mac 顯示與 UI 比例

- Player 預設解析度仍為 1024×768（4:3）。
- 23 個場景的 Canvas Scaler 參考解析度皆為 800×600（4:3），`Match Width Or Height` 為 0。
- Retina 支援已開啟。
- 視窗目前不可調整大小。

這些設定在 16:10 MacBook 螢幕上可能造成左右留黑、畫面裁切或 UI 比例不一致。不能只批次改數值，需先用 16:10、16:9、4:3 三種解析度逐場景截圖比對，再決定維持 letterbox 或擴充可視區域。

### P2：每幀 UI 與動畫寫入

- `player`、`ship`、`ship4` 與 `startTimer` 每幀改寫 UI 文字或動畫狀態。
- `menuAnim` 在閒置後每幀呼叫 `anim.Play("idle")`。
- `player` 在 Boss 已死亡後每幀呼叫 `winAnim.Play("win")`。

建議修法：只在數值或狀態改變時更新文字與動畫；用一次性狀態轉換取代持續 `Play`。

## 效能量測邊界

本輪沒有宣稱實際 FPS、GPU 時間或 Player RAM 峰值。Editor 測試的記憶體與渲染數據包含 Unity Editor 開銷，不能代表發布版 App。

可靠量測應使用 Development Build + Autoconnect Profiler，在 Apple Silicon 實機依序記錄：

1. 主選單閒置 30 秒。
2. 各關卡起點、敵人／粒子最多處、Boss 戰與關卡結束各 30 秒。
3. 連續進出四個關卡後的記憶體是否回落。
4. CPU Main Thread、Render Thread、GC Alloc、Total Used Memory、Texture Memory、Audio Memory、FPS 與場景切換時間。

首輪量測優先場景：`level1`、`level2`、`level3-1`、`level3-2`。它們也是目前序列化場景檔最大的四個，較可能包含最多物件與動畫，但檔案大小本身不等於執行期成本。

## 建議執行順序與驗收

1. 修正 `startTimer`、所有重複 `Invoke` 與 `levelLoader` 重入問題。
2. 快取四個過關監聽器的玩家參照，讓完成圖示只在狀態改變時更新。
3. 新增快速連按、死亡／重試、暫停／回主選單、倒數結束、Boss 死亡等 PlayMode 測試。
4. 集中管理場景識別與跨場景進度。
5. 做 Development Build Profiler 實測；依數據處理真正的 CPU、GC、RAM 或 GPU 熱點。
6. 最後處理 16:10/Retina 畫面與 UI，再做完整人工遊玩驗收。

每一批修正完成後，都應維持既有 4 項測試通過，並重新建置 arm64 App。涉及畫面比例或角色手感的修改需額外做實機畫面與操作驗證。

## 第一批處理紀錄

2026-10-11 已完成第一批 P1 項目：

- 場景載入增加索引驗證與重入鎖定，快速連按只保留第一個請求。
- `startTimer` 改用安全的零值判斷並保證結束動作只執行一次。
- 四個過關監聽器改為快取玩家元件，完成後停止輪詢。
- 過關圖示只在顯示狀態變化時呼叫 `SetActive`。
- 玩家與飛船的失敗、過關流程增加一次性排程保護。
- Boss 退場與第四關成長動畫增加一次性階段保護。
- 新關卡啟動時重設角色與飛船的 static 無敵狀態。
- 新增倒數跨越零值與快速連按場景切換的回歸測試。

驗證結果：

- PlayMode：6/6 通過，包含 23 個場景載入、主要遊戲流程與 2 個新增回歸測試。
- Apple Silicon Build：Success，LZ4HC App 約 418 MB。
- 執行檔架構：僅 `arm64`。
- App 簽章結構：`codesign --verify --deep --strict` 通過。
- 顯示名稱：`營養 How 解`。
- Bundle Identifier：`com.jethrowangsir.nutritionhow`。

## 第二批處理紀錄

2026-10-11 已完成流程測試擴充與場景識別集中化：

- 新增獨立的 `NutritionHow.SceneCatalog` assembly，集中定義正式 23 個場景索引。
- 主選單、角色回首頁、結局影片與完成流程改用共用場景常數。
- PlayMode 測試與遊戲程式共用同一份場景目錄。
- 場景 smoke test 固定驗證 23 個正式場景，不受 Unity Test Runner 臨時初始化場景影響。
- 新增暫停／繼續／暫停後回主選單測試。
- 新增死亡視窗顯示與重新開始關卡測試。
- 新增 Boss 死亡後出口開啟測試。
- 新增第四關倒數結束、失敗視窗與水區觸發器停用測試。

驗證結果：

- PlayMode：10/10 通過，0 failed。
- Apple Silicon Build：Success，LZ4HC App 約 418 MB。
- 執行檔架構：僅 `arm64`。
- App 簽章結構：`codesign --verify --deep --strict` 通過。

## 第三批處理紀錄

2026-10-11 已完成 Apple Silicon Development Player 效能基準：

- 新增可重複執行的 `scripts/benchmark_macos.sh`。
- 自動建置 arm64 Development App 並驗證簽章結構。
- 以 1280×800 實際渲染 `menu` 與五個代表遊戲場景。
- 每個場景穩定 2 秒後取樣 5 秒，連續執行兩輪。
- 記錄載入時間、平均／P95／最差幀時間、Main Thread、每幀 GC allocation 與記憶體峰值。
- PlayMode 仍維持 10/10 通過。
- 正式 Release Build 約 418 MB、僅 `arm64`、簽章結構通過，且不包含 benchmark 執行邏輯。
- 完整方法與結果見 [`PERFORMANCE_BASELINE.md`](PERFORMANCE_BASELINE.md)。

主要結果：閒置幀率受 VSync 限制並保持穩定；`level1` 仍是載入最慢場景；`level3-1` 每幀 GC allocation 為代表場景最高；兩輪記憶體配置大致穩定，未觀察到持續上升型洩漏。

## 第四批處理紀錄（第三階段完成）

2026-10-12 已完成效能修正、載入優化與壓力驗證：

- 玩家、飛船、第四關倒數及開始倒數的 UI 文字只在數值改變時更新。
- 玩家與飛船 HP 動畫只在 HP 狀態改變時播放。
- 第四關倒數圖示只在顯示階段改變時切換，閒置 GC 由 26 B/frame 降至 0。
- 新增 Unity 場景依賴稽核工具，定位 `level1` 的 226 張舌頭背景逐格動畫。
- 舌頭動畫由 60 FPS／226 張調整為 30 FPS／113 張，維持 1920×500 解析度與 3.77 秒總長。
- `level1` 來源依賴總量由約 206 MiB 降至 151 MiB，載入時間約改善 4.6%。
- 新增四個主要遊戲場景的自動壓力回歸，涵蓋 2 倍速度、高頻敵方投射物與粒子播放。

最終驗證結果：

- PlayMode：11/11 通過，0 failed。
- 23 個正式場景全部可載入。
- 六場景兩輪效能與記憶體基準完成，沒有觀察到持續上升型洩漏。
- 壓力回歸沒有未預期 Error 或 Exception。
- Apple Silicon Release Build：Success，App 約 398 MB，僅包含 `arm64`。
- App 簽章結構、中文顯示名稱與 `com.jethrowangsir.nutritionhow` Bundle Identifier 驗證通過。
- Release 組件不包含 Development benchmark 啟動邏輯。
- 完整最終數據見 [`PERFORMANCE_BASELINE.md`](PERFORMANCE_BASELINE.md)。
