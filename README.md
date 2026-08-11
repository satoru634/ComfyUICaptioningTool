# ComfyUICaptioningTool

✨ [English](doc/README_english.md)

ComfyUI（WD Timm Tagger）を使った画像キャプショニング（タグ付け）を GUI から操作するツール。[comfyui_tools](https://github.com/satoru634/comfyui_tools) の `captioning_tool`（Python 実装）の C# WPF 移植版。

![MainPage](./doc/images/main_page.png)

## 機能

- 指定ディレクトリ内の画像を WD14 Tagger で一括タグ付けし、同名の `.txt` キャプションファイルを生成（再帰処理・上書きオプション対応）
- 先頭追加タグ（prepend）・除外タグ（exclude）によるタグフィルタ（`captioning_config.json` の既定値と実行時入力をマージ）
- 他の `captioning_config.json` からの prepend/exclude タグのインポート
- 実行結果（成功/失敗・処理件数・1 ファイルごとのログ）の一覧表示
- 画像とタグを並べて確認・編集できるギャラリー表示（画像タイル一覧＋選択画像のタグ編集の2ペイン構成。タグの選択・並び替え・一括でのタグ追加/削除、タグ一覧のクリップボードコピー、操作履歴の作業ログ出力）
- タグ集計レポートの生成・表示（タグ名でのインタラクティブなフィルタリング、選択タグの使用画像一覧表示に対応）
- `captioning_config.json`（ComfyUI URL・WD14 モデル名・しきい値・既定 prepend/exclude タグ）の GUI からの直接編集
- テーマ切り替え・接続設定の永続化
- 日本語/英語の表示言語切替（設定ページ、再起動不要で即時反映）

## クイックスタート

### 必要環境

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) （Visual Studio 2022以上）
- 起動済みの [ComfyUI](https://github.com/comfyanonymous/ComfyUI) サーバー

※本ツールが使用する WD14 Tagger 用ワークフローで、以下のカスタムノード（ComfyUI 側に事前インストールが必要です）を使用します。

- [ComfyUI-WD-Timm-Tagger](https://github.com/bedovyy/ComfyUI-WD-Timm-Tagger)

また、実行ファイルと同階層に WD14 Tagger 用ワークフローテンプレート（`templates/template_wd14_tagger.json`）を配置する必要があります。

### ビルド・起動

```bash
git clone --recursive https://github.com/satoru634/ComfyUICaptioningTool.git
cd ComfyUICaptioningTool
dotnet run --project ComfyUICaptioningTool
```

### 初回設定

1. **設定** ページを開きます
2. **captioning_config.json パス**・**実行結果ログ出力フォルダ** を設定します

![設定ページ](./doc/images/settings_page.png)

3. 指定したパスに `captioning_config.json` がまだ無い場合は、**Config** ページを開いて ComfyUI URL・WD14 モデル名・しきい値・既定の prepend/exclude タグを入力し保存すると、そのパスに新規作成されます

![Configページ](./doc/images/config_page.png)

### タグ付けを実行してみる

1. **Main** ページで対象ディレクトリを選択し、必要に応じて再帰処理・上書き・レポート生成のオプションと prepend/exclude タグを設定します
2. **実行** ボタンをクリックします
3. **データ** ページで実行結果を、**ギャラリー** ページで画像とタグを確認します

![ギャラリーページ](./doc/images/gallery_page.png)

各ページの詳しい使い方は [doc/usage.md](doc/usage.md) を参照してください。

## 多言語対応

GUI 全体（画面文言・メッセージ・ナビゲーションメニューなど）を日本語/英語で切り替えられます。

- 切替方法: **設定** ページの言語選択で「日本語」/「English」を選択
- 反映タイミング: 選択と同時に全画面へ即時反映（アプリの再起動は不要）
- 既定言語: 日本語（OS のロケール設定に関わらず、初回起動時は日本語）
- 選択した言語は設定として保存され、次回起動時にも引き継がれます

## 技術スタック

| 項目 | 内容 |
|---|---|
| ランタイム | .NET 8 / WPF |
| UI フレームワーク | Wpf.Ui v4.3.0 |
| MVVM | CommunityToolkit.Mvvm v8.4.2 |
| DI | Microsoft.Extensions.Hosting |
| 共有ライブラリ | ComfyUILibs（サブモジュール） |

## プロジェクト構成

```
ComfyUICaptioningTool/          ← ソリューションルート
  ComfyUILibs/                  ← 共有ライブラリ（サブモジュール）
  ComfyUICaptioningTool/        ← WPF GUI プロジェクト
  ComfyUICaptioningToolTests/   ← GUI テスト
```

## ライセンス

[LICENSE](LICENSE) を参照してください。
