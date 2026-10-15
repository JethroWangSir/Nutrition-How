# 本機遊玩難度指標

遊戲會將關卡嘗試結果寫入 Mac 使用者資料夾：

```text
~/Library/Application Support/JethroWangSir/營養 How 解/playtest-metrics.jsonl
```

這份資料只保存在本機，不會上傳，也不包含玩家名稱、帳號或裝置識別資訊。每一行是一筆獨立 JSON，欄位如下：

| 欄位 | 說明 |
| --- | --- |
| `timestampUtc` | 結果發生的 UTC 時間 |
| `level` | 第一至第四關，以 `1` 至 `4` 表示 |
| `attempt` | 本次啟動遊戲期間，該關的第幾次嘗試 |
| `outcome` | `completed` 或 `failed` |
| `reason` | 失敗時為 `health_depleted`、`objective_requirement` 或 `time_expired`；完成時留空 |
| `elapsedSeconds` | 從該關起始場景到結果發生的實際秒數，不受暫停時間倍率影響 |

第三關與第四關各自包含前後半場景，仍會合併計算為同一次關卡嘗試。直接開啟後半場景時不會建立不完整紀錄。

建議累積至少 5 位首次遊玩的測試者，再比較各關完成率、失敗原因與完成時間中位數；不要只依單一玩家或平均值直接調整難度。自動化 PlayMode 測試會改寫到獨立暫存檔，不會混入正式資料。

若 Mac 已安裝 `jq`，可用下列指令檢視所有紀錄：

```bash
jq -s '.' "$HOME/Library/Application Support/JethroWangSir/營養 How 解/playtest-metrics.jsonl"
```
