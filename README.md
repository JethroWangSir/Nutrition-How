# 營養 HOW 解

大學畢業專題的 Unity 2D 遊戲。這個 repository 專用於 macOS 整理、問題修復與 Unity 6.3 LTS 升級；舊帳號上的原版 repository 不會在此改動。

## 目前版本

- Unity Editor: `6000.3.26f1`（Unity 6.3 LTS）
- 專案內容: `Assets/`, `Packages/`, `ProjectSettings/`
- Git LFS: 大型圖片與影片素材
- 目標平台: Apple Silicon Mac（`arm64`）
- 已驗證開場、選單、四個關卡流程及全部 23 個建置場景

## 已完成修復

- 修正 Input System 在場景切換時的 `IndexOutOfRangeException`
- 修正第三關 Boss 區缺少 Animator 時的 `NullReferenceException`
- 專案已完成 Unity 6.3 LTS 轉換

## 開啟專案

1. 安裝 Unity Hub 與 Unity `6000.3.26f1`。
2. 在 Unity Hub 選擇 **Add project from disk**。
3. 選擇這個 repository 的根目錄（同時包含 `Assets`、`Packages`、`ProjectSettings` 的資料夾）。
4. 首次開啟時讓 Unity 重建 `Library/`；該資料夾不會納入 Git。

## 建置 Apple Silicon 版本

在 Unity 選擇 **Build > Nutrition HOW > macOS Apple Silicon**。

命令列建置可使用：

```sh
/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath /path/to/Nutrition-How \
  -executeMethod NutritionHowBuild.BuildMacArm64 \
  -quit
```

預設輸出為 `/tmp/Nutrition-How-macOS-arm64.app`；可用
`NUTRITION_HOW_BUILD_PATH` 指定其他輸出位置。建置會使用無損的 LZ4HC
資料壓縮，縮小成品容量但不降低圖片品質。
