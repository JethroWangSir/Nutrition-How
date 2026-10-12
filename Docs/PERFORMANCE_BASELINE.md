# Apple Silicon 執行效能基準

量測日期：2026-10-11  
專案版本：Unity 6000.3.26f1、App 1.0.0  
硬體：Apple M3、16 GB 統一記憶體  
系統：macOS 26.6.2  
測試版本：Apple Silicon Development Build（arm64）

## 結論

目前六個代表場景的閒置幀率穩定，首要問題不是持續渲染效能，而是部分場景載入時間與少量每幀 managed allocation：

- 六個場景平均約 73.8–74.0 FPS，P95 幀時間約 13.86–14.28 ms。
- `level1` 兩輪載入皆約 4.5 秒，是最明顯且可重現的載入瓶頸。
- `level3-1` 每幀 GC allocation 約 358–359 bytes，為量測場景最高。
- 第二輪的場景記憶體配置大致與第一輪相同，沒有觀察到持續上升型洩漏。
- Unity reserved memory 最後穩定在約 370.91 MiB；保留記憶體不等於實際使用量，也不能單獨視為洩漏。

下一步應先移除 `level1`、`level2`、`level3-1` 的每幀 UI 字串配置，再分析 `level1` 場景依賴與載入流程。不建議在目前資料下先降低圖片品質或調整角色物理更新。

## 測試方法

使用 `scripts/benchmark_macos.sh` 建置獨立的 Development App，並在真正的 Unity Player 中自動執行兩輪：

1. `menu`
2. `level1`
3. `level2`
4. `level3-1`
5. `level3-2`
6. `level4-1`

每個場景載入完成後先等待 2 秒，再取樣 5 秒。測試使用 1280×800 視窗、VSync 1、`Application.targetFrameRate = -1`。每輪記錄：

- 場景載入時間。
- 平均、P95 與最差幀時間。
- Main Thread recorder。
- 每幀 GC allocation。
- Unity total allocated、reserved 與 Mono used memory 峰值。

執行方式：

```sh
./scripts/benchmark_macos.sh
```

預設輸出至 `/tmp/nutrition-how-benchmark/`，包含 Development App、build log、player log 與完整 JSON。

同一份程式碼亦重新完成正式 Release Build 驗證：App 約 418 MB、僅包含 `arm64`，簽章結構通過。Release 的 `Assembly-CSharp.dll` 不包含 benchmark 類別或啟動參數，量測流程只會存在於 Development Build／Editor。

## 兩輪結果摘要

| 場景 | 第一輪載入 | 第二輪載入 | 平均 FPS | P95 | GC bytes/frame | 配置峰值 |
|---|---:|---:|---:|---:|---:|---:|
| menu | 291 ms | 1,132 ms | 73.97 | 14.13 ms 以下 | 0 | 193.48 MiB |
| level1 | 4,513 ms | 4,472 ms | 73.98 | 13.99 ms 以下 | 156–157 | 239.28 MiB |
| level2 | 2,272 ms | 2,268 ms | 73.97 | 14.28 ms 以下 | 182 | 228.25 MiB |
| level3-1 | 2,423 ms | 2,420 ms | 73.97 | 13.94 ms 以下 | 358–359 | 229.01 MiB |
| level3-2 | 416 ms | 420 ms | 73.99 | 14.20 ms 以下 | 49–50 | 226.51 MiB |
| level4-1 | 1,566 ms | 1,565 ms | 73.98 | 14.08 ms 以下 | 26 | 226.02 MiB |

完整逐輪數值保存在 [`performance-baseline-2026-10-11.csv`](performance-baseline-2026-10-11.csv)。

## 判讀

### 幀率

所有場景皆接近相同的約 74 FPS，符合 VSync 下的顯示更新率限制。P95 幀時間相近，沒有場景在閒置狀態出現持續性掉幀。

Main Thread recorder 約 13.5 ms，當中包含等待 VSync 的時間，不能解讀成 CPU 每幀真的忙碌 13.5 ms。要分析實際 CPU 熱點，後續仍需在關閉 VSync或使用 Unity Profiler Timeline 的情境下量測。

### GC allocation

`level3-1` 的 358–359 bytes/frame 最高，其次是 `level2` 的 182 bytes/frame 與 `level1` 的 156–157 bytes/frame。現有腳本會在 `Update` 中持續執行 `ToString()` 與 UI 文字寫入，與此結果吻合。

這些數值很小，短期不會造成明顯卡頓，但可安全改為「數值變更時才更新」，目標是讓一般遊玩時接近 0 bytes/frame。benchmark 本身也會產生少量監測成本，因此應以修正前後相同工具的相對差異為準。

### 記憶體

`level1` 的 total allocated 峰值最高，第一輪 237.97 MiB、第二輪 239.28 MiB。其他遊戲場景大多在 226–229 MiB。

第二輪各場景配置量與第一輪接近；reserved memory 在第二輪 `level1` 到達 370.91 MiB 後保持不變。這比較符合 Unity allocator 擴充後保留記憶體，而不是每換一次場景就無限增加的洩漏。

### 載入時間

`level1`、`level3-1`、`level2` 依序最慢，而且第二輪沒有明顯改善，表示主要成本很可能是場景物件／素材反序列化與初始化，不只是首次讀取磁碟。

後續應使用 Editor dependency audit 與 Profiler Loading 模組拆分：

1. `level1` 的場景物件、Sprite／Animator 依賴及 `Start` 初始化。
2. 是否可在教學場景期間使用 `LoadSceneAsync` 預載下一關。
3. 是否有單一場景載入未使用的大型貼圖或動畫資源。

## 量測限制

- 場景在無人工輸入的閒置狀態取樣，尚未涵蓋敵人密集、Boss 戰、粒子大量出現與快速移動。
- Development Build 有 Profiler 與除錯開銷；Release Build 通常會更快。
- 最低 FPS 由單一最差幀換算，容易受排程或場景內週期性工作影響，不應單獨作為玩家體感結論。
- 本報告適合作為後續優化的可重複比較基準，不取代完整人工遊玩與 Unity Profiler Timeline 分析。
