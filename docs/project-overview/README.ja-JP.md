# March7thHoney

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white" alt=".NET 10" />
  <img src="https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20%7C%20macOS-2ea44f" alt="Platform" />
  <img src="https://img.shields.io/badge/Status-Active%20Development-f39c12" alt="Status" />
  <img src="https://img.shields.io/badge/License-AGPL--3.0-blue" alt="License" />
</p>

[English](../../README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | 日本語

March7thHoney は C# と .NET 10 で書かれたゲームサーバーです。Windows、Linux、macOS で動作し、現在も活発に開発されています。

このページ以外のドキュメントは英語と簡体字中国語で提供されています。以下のリンクは英語版を指します。

## 機能

| モジュール | 状態 | 説明 |
|---|---|---|
| ショップ | 安定 | 閲覧と購入の流れが動作 |
| 編成 | 安定 | チームとスロットの切り替えに対応 |
| 跳躍/ガチャ | 安定 | 抽選の流れと結果表示が完全に動作 |
| 戦闘フロー | 安定 | 基本の戦闘ループをプレイ可能 |
| オープンワールド | 安定 | シーンの読み込み、インタラクション、探索 |
| キャラクター育成 | 安定 | レベルアップと昇格が利用可能 |
| 任務 | 開発中 | メインと共通の進行は動作、例外的なケースが残る |
| フレンド | 安定 | 基本のやり取りと表示 |
| 異相の仲裁 | 安定 | 入場、挑戦、報酬に対応 |
| チャレンジモード | 安定 | 忘却の庭、虚構叙事、末日の幻影をプレイ可能 |
| 貨幣戦争 | 開発中 | 基本のゲームフローが利用可能 |
| アチーブメント | 安定 | ほとんどのアチーブメントに対応 |

全体の一覧は[ゲームプレイ実装ツリー](../development/gameplay-implementation.md)を参照してください。

## クイックスタート

.NET SDK 10（10.0.100 以降）と Git が必要です。Windows、Linux、macOS（`x64` または `arm64`）に対応しています。

```bash
git clone https://github.com/Mar7thDev/March7thHoney.git
cd March7thHoney
dotnet run --project Program/Program.csproj
```

リポジトリにはゲームリソースは含まれていません。サーバーはローカルのリソースキャッシュから読み込みます。作成方法は[クライアントのエクスポートからリソースキャッシュを再構築する](../operations/resource-cache-from-client-export.md)を参照してください。

Linux サーバーでの運用（systemd サービスと更新を含む）は[ネイティブ Linux ガイド](../getting-started/native-linux.md)に従ってください。

## ビルド

```bash
dotnet build Program/Program.csproj -c Release
```

ビルド時に、バージョンを固定した `Google.Protobuf.Tools` 3.33.1 を使って、リポジトリ内の `Proto/StarRail.proto` からプロトコルの C# を生成します。生成されたファイルは `Proto/obj/` に置かれ、バージョン管理の対象外です。各ゲームバージョンのプロトコルはそれぞれのバージョンブランチに保存されています。スキーマの更新方法とプラットフォーム要件は[プロトコルガイド](../development/protobuf.md)を参照してください。

CI は `main` の自己完結型ビルドをワークフローの成果物として公開します。

| ランタイム | 成果物名 |
|---|---|
| `win-x64` | `March7thHoney-win-x64` |
| `linux-x64` | `March7thHoney-linux-x64` |
| `linux-arm64` | `March7thHoney-linux-arm64` |

## ドキュメント

[ドキュメント索引](../README.md)にすべての文書が載っています。主な入口：

- サーバーの運用：[ネイティブ Linux ガイド](../getting-started/native-linux.md)、[アカウントセキュリティ](../operations/account-security.md)
- API：[MUIP API](../api/muip-api.md)、[Web アカウント API](../api/web-account-api.md)
- 開発に参加する：[コントリビューションガイド](../../CONTRIBUTING.md)、[プロトコル](../development/protobuf.md)、[ソースジェネレーター](../development/source-generators.md)
- 回帰テスト：[セキュリティゲート](../testing/security-regression.md)と `docs/testing/` 以下の文書

## コントリビューション

変更の提案を歓迎します。ビルド方法、実行すべき回帰テスト、コミットとプルリクエストの規約は[コントリビューションガイド](../../CONTRIBUTING.md)を参照してください。脆弱性は[セキュリティポリシー](../../SECURITY.md)に従って非公開で報告してください。

## ロードマップ

1. 貨幣戦争と階差宇宙の改善。
2. 残りの任務と特殊なトリガーロジックの補完。
3. 例外的なケースでの精算と同期の安定性向上。
4. イベントモードの対応範囲の拡大。
5. UI と状態同期の問題の継続的な修正。
6. 設定可能なサーバーオプションの追加。

## トラブルシューティング

- 設定したログディレクトリのサーバーログを確認してください。
- `/scene reload` でほとんどのシーンの問題が解決します。
- 更新後に動作がおかしい場合は、ランタイムのキャッシュとデータを消去し、新しいアカウントで再確認してください。
- コミュニティサポート：<https://discord.gg/CyreneEchoes>

## クレジット

### コラボレーター

- [Oriha](https://github.com/OrihaWeave)
- Cyrup
- [Melioli](https://github.com/Melioli)

### コントリビューター

- [Yuki](https://github.com/Yuki8859)
- [Cyt](https://github.com/qinfyy)
- [Glzd001](https://github.com/Glzd001)
- [blamefire159-debug](https://github.com/blamefire159-debug)
- [AzureXuanVerse](https://github.com/AzureXuanVerse)

### 関連プロジェクト

- [LunarCore](https://github.com/Melledy/LunarCore)
- [DanhengServer](https://github.com/OrihaWeave/DanhengServer-OpenSource)
- [KcpSharp](https://github.com/yigolden-oss/KcpSharp)

## ライセンス

March7thHoney は [GNU Affero General Public License v3.0](../../LICENSE) の下でライセンスされています。
