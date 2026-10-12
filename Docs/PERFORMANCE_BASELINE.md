# Apple Silicon 執行效能基準

最終量測日期：2026-10-12

專案版本：Unity 6000.3.26f1、App 1.0.0

硬體：Apple M3、16 GB 統一記憶體

系統：macOS 26.6.2

測試版本：Apple Silicon Development Build（arm64）

## 結論

第三階段的執行效能、記憶體與程式穩定性驗證已完成：

- 六個代表場景在本輪 60 Hz VSync 環境皆維持約 60 FPS，P95 幀時間低於 17.7 ms。
- 兩輪場景切換後，Unity reserved memory 最後穩定在約 367.27 MiB，沒有觀察到持續上升型洩漏。
- `level4-1` 的閒置每幀 GC allocation 已由 26 B/frame 降至 0。
- `level1` 舌頭背景動畫由 226 張、60 FPS 調整為 113 張、30 FPS；1920×500 單張解析度與 3.77 秒總長不變。
- `level1` 依賴來源總量由約 206 MiB 降至 151 MiB；兩輪載入時間由 5.61/5.52 秒降至 5.35/5.26 秒，約改善 4.6%。
- `level1`、`level2`、`level3-1` 仍有約 164、196、412 B/frame 的低量配置，但未造成可量測的持續掉幀或記憶體累增。
- 11/11 PlayMode 測試通過；其中新增的壓力回歸會以 2 倍遊戲速度執行四個主要關卡，並提高 Level 1 敵方投射物頻率及啟動粒子系統。

## 測試方法

使用 `scripts/benchmark_macos.sh` 建置獨立的 Development App，並在真正的 Unity Player 中自動執行兩輪：

1. `menu`
2. `level1`
3. `level2`
4. `level3-1`
5. `level3-2`
6. `level4-1`

每個場景載入完成後先等待 2 秒，再取樣 5 秒。測試使用 1280×800 視窗、VSync 1、`Application.targetFrameRate = -1`。每輪記錄場景載入時間、平均與 P95 幀時間、Main Thread、每幀 GC allocation，以及 Unity total allocated、reserved 與 Mono used memory 峰值。

執行方式：

```sh
./scripts/benchmark_macos.sh
```

預設輸出至 `/tmp/nutrition-how-benchmark/`，包含 Development App、build log、player log 與完整 JSON。正式數值另存於 [`performance-stage3-final-2026-10-12.csv`](performance-stage3-final-2026-10-12.csv)。

場景依賴可透過 Unity 選單 **Tools > Nutrition HOW > Audit Scene Dependencies** 重跑；命令列可執行 `NutritionHowSceneDependencyAudit.AuditSceneDependencies`。

## 最終兩輪結果

| 場景 | 第一輪載入 | 第二輪載入 | 平均 FPS | P95 | GC bytes/frame | 配置峰值 |
|---|---:|---:|---:|---:|---:|---:|
| menu | 291 ms | 1,413 ms | 60.00 | 17.65 ms 以下 | 0 | 193.64 MiB |
| level1 | 5,353 ms | 5,265 ms | 60.00 | 16.90 ms 以下 | 164–165 | 238.06 MiB |
| level2 | 2,800 ms | 2,792 ms | 59.90 | 17.64 ms 以下 | 195–196 | 228.41 MiB |
| level3-1 | 2,973 ms | 2,987 ms | 60.00 | 17.59 ms 以下 | 412 | 229.17 MiB |
| level3-2 | 517 ms | 514 ms | 59.99 | 17.67 ms 以下 | 31–32 | 226.65 MiB |
| level4-1 | 1,932 ms | 1,931 ms | 60.00 | 17.64 ms 以下 | 0 | 226.18 MiB |

## 判讀

### 幀率

所有場景都受 VSync 限制在本輪顯示器的 60 Hz，P95 幀時間相近，沒有場景在閒置狀態出現持續性掉幀。前一日基準約為 74 FPS，是顯示器更新率狀態不同，不能把 74 與 60 直接解讀為效能退步。

Main Thread recorder 包含等待 VSync 的時間，不能解讀成 CPU 每幀都處於滿載；本報告主要以相同設定下的場景相對差異判讀。

### GC allocation

數值不變時重複寫入 UI 文字與 HP 動畫的程式已改為狀態變更時才更新。此修正讓倒數計時場景 `level4-1` 穩定降至 0 B/frame，也避免遊玩期間不必要的 Animator 重播。

其他三個主要關卡仍有低量配置，且數值大致隨場景系統複雜度增加。相同工具重測沒有顯示這些配置會造成幀率或記憶體持續惡化，因此第三階段不再為追求數字歸零而冒險改動角色物理或第三方套件。後續新增功能若使配置明顯高於此基準，應再用 Profiler Timeline 追查呼叫堆疊。

### 記憶體

優化後 `level1` total allocated 峰值由約 239.57 MiB 降至 238.06 MiB。第二輪 reserved memory 在 `level1` 到達約 367.27 MiB 後保持不變，較符合 Unity allocator 擴充後保留記憶體，而不是每換一次場景就無限增加的洩漏。

### 載入時間與素材依賴

Unity `AssetDatabase` 稽核顯示，原始 `level1` 有 2,173 個依賴，明顯高於其他代表場景的 462–777 個。主因是 226 張 1920×500、60 FPS 的舌頭背景逐格動畫。改為 30 FPS 後：

- 動畫參照影格：226 → 113。
- Level 1 依賴數：2,173 → 2,060。
- 來源依賴總量：約 206 MiB → 151 MiB。
- 第二輪載入：5,517 ms → 5,265 ms。

改善幅度約 4.6%，證明大量獨立 Sprite 是載入成本的一部分，但不是唯一成本。剩餘約 5.3 秒屬大型場景的可重現載入成本；若未來要進一步縮短，應將這類逐格背景改成影片、圖集或可串流資源，而不是再降低單張解析度。

## 穩定性與壓力驗證

最終 PlayMode 為 11/11 通過，包含：

- 23 個正式 Build Settings 場景全部可載入。
- 開場、主選單、四關入口與完成後結局流程。
- 暫停／繼續／回首頁、死亡／重試、Boss 出口與第四關逾時。
- 快速連按場景切換與倒數跨越零值回歸。
- 四個主要關卡在 2 倍遊戲速度下連續執行；Level 1 敵方投射物縮短至 0.05 秒生成並啟動現有粒子系統。

壓力測試期間沒有未預期 Error 或 Exception。

## 量測限制

- 自動壓力回歸能抓例外與狀態錯誤，但不等同真人完整操作和主觀手感驗收。
- Development Build 有 Profiler 與除錯開銷；Release Build 通常會更快。
- 最低 FPS 容易受單幀排程影響，因此本報告主要使用平均與 P95。
- 30 FPS 舌頭背景已通過載入、建置與流程測試；動態觀感仍應在後續畫面適配階段一併做人工視覺驗收。
