<div align="center">

# March7thHoney

**以 C# 與 .NET 10 編寫、可以實際遊玩的遊戲伺服端。<br />複製儲存庫，一條命令啟動，然後登入。**

<p>
  <a href="https://github.com/Mar7thDev/March7thHoney/actions/workflows/ci.yml"><img src="https://github.com/Mar7thDev/March7thHoney/actions/workflows/ci.yml/badge.svg?branch=main" alt="CI" /></a>
  <a href="https://github.com/Mar7thDev/March7thHoney/stargazers"><img src="https://img.shields.io/github/stars/Mar7thDev/March7thHoney?style=flat&logo=github&color=f5c542" alt="Stars" /></a>
  <a href="https://github.com/Mar7thDev/March7thHoney/commits/main"><img src="https://img.shields.io/github/last-commit/Mar7thDev/March7thHoney?color=2ea44f" alt="Last commit" /></a>
  <img src="https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white" alt=".NET 10" />
  <img src="https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20%7C%20macOS-0a7ea4" alt="Platform" />
  <a href="../../LICENSE"><img src="https://img.shields.io/badge/License-AGPL--3.0-blue" alt="License" /></a>
  <a href="https://discord.gg/CyreneEchoes"><img src="https://img.shields.io/badge/Discord-Join-5865F2?logo=discord&logoColor=white" alt="Discord" /></a>
</p>

[快速開始](#快速開始) · [功能](#功能) · [文件](../README.zh-CN.md) · [參與貢獻](../../CONTRIBUTING.zh-CN.md) · [Discord](https://discord.gg/CyreneEchoes)

[English](../../README.md) | [简体中文](README.zh-CN.md) | 繁體中文 | [日本語](README.ja-JP.md)

</div>

| | |
|---|---|
| **從頭到尾可玩** | 商店、躍遷、戰鬥、大世界與各個終局挑戰玩法都能搭配真實用戶端運作，任務仍在補全。 |
| **為實際部署而做** | 支援 SQLite 或 MySQL，內建網頁後台、MUIP API 與可接入 systemd 的 Linux 部署方式。支援 Windows、Linux、macOS 的 `x64` 與 `arm64`。 |
| **執行時不靠反射** | 封包處理器、命令與資料庫存取由 Roslyn 原始碼產生器註冊，因此伺服端也能以 NativeAOT 發佈。 |
| **每次變更都有檢查** | 警告視為錯誤的建置，以及帳號、Web 服務、KCP 與原始碼產生器的回歸套件在 CI 中執行；登入、場景與各玩法另有需要真實資源的回歸套件。 |

除本頁外，其餘文件提供英文與簡體中文版本；下方連結指向簡體中文版本。

## 功能

| 模組 | 狀態 | 說明 |
|---|---|---|
| 商店系統 | 穩定 | 瀏覽與購買流程可用 |
| 編隊系統 | 穩定 | 支援隊伍與欄位切換 |
| 躍遷/抽卡 | 穩定 | 抽取流程與結果顯示完整 |
| 戰鬥流程 | 穩定 | 核心戰鬥循環可玩 |
| 大世界場景 | 穩定 | 場景載入、互動、探索 |
| 角色養成 | 穩定 | 升級與晉階可用 |
| 任務系統 | 進行中 | 主線與通用流程可用，仍有邊緣情況 |
| 好友系統 | 穩定 | 核心互動與顯示 |
| 異相仲裁 | 穩定 | 支援進入、挑戰與獎勵 |
| 挑戰玩法 | 穩定 | 忘卻之庭、虛構敘事、末日幻影可玩 |
| 貨幣戰爭 | 進行中 | 基礎玩法流程可用 |
| 成就系統 | 穩定 | 支援大部分成就 |

完整列表見[玩法實作進度](../development/gameplay-implementation.zh-CN.md)。

## 快速開始

需要 .NET SDK 10（10.0.100 或更新）與 Git，支援 Windows、Linux、macOS（`x64` 或 `arm64`）。

```bash
git clone https://github.com/Mar7thDev/March7thHoney.git
cd March7thHoney
dotnet run --project Program/Program.csproj
```

儲存庫不包含遊戲資源。伺服端從本機的資源快取讀取它們，做法見[從用戶端匯出重建資源快取](../operations/resource-cache-from-client-export.zh-CN.md)。

在 Linux 伺服器上部署（包含 systemd 服務與更新）請參考[原生 Linux 部署指南](../getting-started/native-linux.zh-CN.md)。

## 建置

```bash
dotnet build Program/Program.csproj -c Release
```

建置時會用固定版本的 `Google.Protobuf.Tools` 3.33.1，從儲存庫裡的 `Proto/StarRail.proto` 產生協議 C#。產生的檔案位於 `Proto/obj/`，不納入版本控制。每個遊戲版本的協議保存在各自的版本分支上。更新協議定義的方法與平台需求見[協議說明](../development/protobuf.zh-CN.md)。

CI 會把 `main` 的自包含建置發佈為工作流程產物：

| 執行環境 | 產物名稱 |
|---|---|
| `win-x64` | `March7thHoney-win-x64` |
| `linux-x64` | `March7thHoney-linux-x64` |
| `linux-arm64` | `March7thHoney-linux-arm64` |

## 文件

[文件索引](../README.zh-CN.md)列出了全部文件。常用入口：

- 執行伺服器：[原生 Linux 部署指南](../getting-started/native-linux.zh-CN.md)、[帳號安全](../operations/account-security.zh-CN.md)
- 介面：[MUIP API](../api/muip-api.zh-CN.md)、[網站遊戲帳號介面](../api/web-account-api.zh-CN.md)
- 參與開發：[貢獻指南](../../CONTRIBUTING.zh-CN.md)、[協議](../development/protobuf.zh-CN.md)、[原始碼產生器](../development/source-generators.zh-CN.md)
- 回歸測試：[安全回歸入口](../testing/security-regression.zh-CN.md)以及 `docs/testing/` 下的其他文件

## 參與貢獻

歡迎提交改動。建置方法、需要執行的回歸以及提交與 PR 的約定見[貢獻指南](../../CONTRIBUTING.zh-CN.md)。安全漏洞請依[安全政策](../../SECURITY.zh-CN.md)私下回報。

## 開發計畫

1. 完善貨幣戰爭與差分宇宙。
2. 補齊剩餘任務與特殊觸發邏輯。
3. 提升邊緣情況下結算與同步的穩定性。
4. 擴大活動玩法的覆蓋。
5. 持續修復介面與狀態同步問題。
6. 增加更多可設定的伺服器選項。

## 疑難排解

- 查看所設定日誌目錄下的伺服器日誌。
- `/scene reload` 能解決大部分場景問題。
- 更新後如果行為不一致，先清理執行時快取與資料，再用新帳號複測。
- 社群支援：<https://discord.gg/CyreneEchoes>

## 致謝

### 協作者

- [Oriha](https://github.com/OrihaWeave)
- Cyrup
- [Melioli](https://github.com/Melioli)

### 貢獻者

- [Yuki](https://github.com/Yuki8859)
- [Cyt](https://github.com/qinfyy)
- [Glzd001](https://github.com/Glzd001)
- [blamefire159-debug](https://github.com/blamefire159-debug)
- [AzureXuanVerse](https://github.com/AzureXuanVerse)
- [AcheronWife](https://github.com/AcheronWife)
- [Yuuki](https://github.com/akbaryahya)
- [ShuaiyuMiao](https://github.com/ShuaiyuMiao)

### 相關專案

- [LunarCore](https://github.com/Melledy/LunarCore)
- [DanhengServer](https://github.com/OrihaWeave/DanhengServer-OpenSource)
- [KcpSharp](https://github.com/yigolden-oss/KcpSharp)

<a href="https://github.com/Mar7thDev/March7thHoney/graphs/contributors"><img src="https://contrib.rocks/image?repo=Mar7thDev/March7thHoney" alt="貢獻者" /></a>

## Star 趨勢

<a href="https://www.star-history.com/#Mar7thDev/March7thHoney&Date"><img src="https://api.star-history.com/svg?repos=Mar7thDev/March7thHoney&type=Date" alt="Star History Chart" width="100%" /></a>

## 授權

March7thHoney 以 [GNU Affero 通用公共授權條款 v3.0](../../LICENSE) 授權。
