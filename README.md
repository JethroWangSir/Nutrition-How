# 營養 HOW 解

大學畢業專題的 Unity 2D 遊戲。這個 repository 專用於 macOS 整理、問題修復與 Unity 6.3 LTS 升級；舊帳號上的原版 repository 不會在此改動。

## 目前基線

- Unity Editor: `2019.4.16f1`
- 專案內容: `Assets/`, `Packages/`, `ProjectSettings/`
- Git LFS: 大型圖片與影片素材
- 已在 macOS 進入遊戲開場與選單畫面

## 已知問題

`com.unity.inputsystem` 1.0.2 在場景切換時會出現 `IndexOutOfRangeException`。預定先在 Unity 2019.4 基線修復與驗證，再升級到 Unity 6.3 LTS。

## 開啟專案

1. 安裝 Unity Hub 與 Unity `2019.4.16f1`。
2. 在 Unity Hub 選擇 **Add project from disk**。
3. 選擇這個 repository 的根目錄（同時包含 `Assets`、`Packages`、`ProjectSettings` 的資料夾）。
4. 首次開啟時讓 Unity 重建 `Library/`；該資料夾不會納入 Git。

## 升級原則

`main` 保留可追溯的基線，修復與 Unity 6.3 LTS 轉換在獨立 branch 進行，確認能編譯、開啟場景與執行主要流程後才合併。
