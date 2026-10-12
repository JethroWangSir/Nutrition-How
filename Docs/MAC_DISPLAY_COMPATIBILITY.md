# macOS 顯示與操作相容性

第四階段已於 2026-10-12 完成，目標是讓原本在 Windows 筆電製作的版面，
在 Apple Silicon Mac 的 Retina 與寬螢幕視窗中保留原始構圖與可操作範圍。

## 顯示策略

- `level1`、`level2`、`level3-1`、`level3-2`、`level4-1`、`level4-2`
  使用中央 4:3 遊玩區，避免寬螢幕額外顯示關卡左右內容。
- 選單、知識、遊戲說明與影片場景依原始 16:9 素材顯示，避免左右文字和插圖被裁切。
- 當視窗比例與場景不同時，以黑邊置中，不拉伸畫面。
- Overlay Canvas 會映射至同一安全區；HUD、按鈕及虛擬游標不會落在黑邊或視窗外。
- Canvas 以 800×600（4:3）或 800×450（16:9）作為參考解析度，支援 Retina 縮放。

## macOS 視窗與輸入

- 預設視窗為 1280×800，可自由調整大小。
- App 失去焦點時不繼續在背景執行。
- 系統游標不再鎖在視窗中央；視窗失去焦點時保持可見。
- 搖桿虛擬游標限制在目前場景安全區內。

## 自動驗證

PlayMode 測試共 13 項，結果 13/13 通過，涵蓋：

- 全部 23 個 Build Settings 場景載入。
- 選單、場景切換、關卡完成及主要遊戲流程。
- 6 個遊玩場景的自動壓力測試。
- 4:3 與 16:9 場景分類、置中 viewport 計算。

視覺回歸可用以下指令重新產生：

```sh
./scripts/capture_visual_regression.sh /tmp/nutrition-how-visual-regression
```

腳本會建置 Apple Silicon Development App，驗證 `arm64` 與簽章，再於
1024×768、1280×800、1920×1080 各擷取 23 個場景，共 69 張 PNG，並為每組
輸出安全區 manifest 與 Player log。2026-10-12 最終驗證的 69 張皆產生成功，
三組 Player log 均無例外或錯誤。

> 截圖只用於本機驗證，不納入 Git。正式 Release Build 不包含截圖控制程式。
