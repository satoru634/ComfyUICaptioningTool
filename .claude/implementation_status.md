# 実装状況

## 現在の状態（2026-07-07 時点）

WPF-UI テンプレートから生成した直後の状態。`MainPage`/`DataPage` はテンプレート由来のサンプル実装（カウンターボタン、ランダムカラー一覧など）のままで、キャプショニング機能固有の実装はまだ入っていない。

`ComfyUILibs`（別リポジトリ）は Python版 `run_workflow` 相当のロジック（`WorkflowRunner` / `ConfigLoader` / `WorkflowBuilder` / `ComfyUIClient` / `Wd14TaggerRunner` / `PreviewImageCacheService` 等）を実装済み・master マージ済み。詳細は `ComfyUILibs/.claude/implementation_status.md` を参照。

`ViewModels/Windows/MainWindowViewModel.cs` の `ApplicationTitle` がテンプレート由来の `"ComfyUIRunWorkflow"` になっていた不整合は修正済み。

### 多言語化（実装ロードマップに先行して着手・完了）

`ComfyUIRunWorkflow` の仕組み（`LocalizationManager` + `Strings.resx`/`Strings.en.resx`）を移植し、日本語（既定）／英語の切り替えに対応した。

- `Helpers/LocalizationManager.cs`、`Resources/Strings.resx`・`Strings.en.resx`・`Strings.cs` を新設。
- `Services/ApplicationHostService.cs` で起動時に `Config.Data.Language` から表示言語を適用。
- `ViewModels/Pages/SettingsViewModel.cs` の言語切替（`OnSelectedLanguageChanged`）を有効化（コメントアウト解除）し、再起動不要で即時反映。
- `ViewModels/Windows/MainWindowViewModel.cs` のナビゲーションメニュー項目（`MenuItems`/`FooterMenuItems`/`TrayMenuItems`）を `BuildMenuItems()` で `LocalizationManager` から構築し、言語切替時に再構築するよう変更。
- `Views/Pages/SettingsPage.xaml` のラベルを `LocalizationManager` へのインデクサーバインディングに置き換え。
- 現時点で翻訳キーを用意しているのは `MainWindow_*`（ホーム／データ／設定メニュー、トレイメニュー）と `Settings_*`（設定ページの見出し・ラベル）のみ。`MainPage`/`DataPage` はテンプレート由来のプレースホルダーのため未翻訳（フェーズ2以降の置き換え時に追加する）。
- `ComfyUICaptioningToolTests`（xUnit）プロジェクトを新設し、`ComfyUICaptioningTool.sln` に追加。`Helpers/LocalizationManagerTests.cs` でカルチャ切替・キー解決・フォールバックを検証済み（全件パス）。

## 移植元（Python版 captioning_tool）

`E:\Python_project\comfyui_tools\captioning_tool\`（詳細仕様: `captioning_tool/doc/SPEC.md`）。

指定ディレクトリ内の画像を WD Timm Tagger でタグ付けし、同名の `.txt` キャプションファイルをバッチ生成する CLI ツール。主な機能:

- ディレクトリ内画像の一括タグ付け（`.jpg` `.jpeg` `.png` `.webp`、`--recursive` でサブディレクトリ対応）
- 既存 `.txt` のスキップ / 上書き（`--overwrite`）
- 冒頭追記タグ（`--prepend`）・除外タグ（`--exclude`）によるタグフィルタ
- タグ集計レポート生成（`--report` → `tags_report.txt`）
- `config.json`（`comfyui_url` / `wd14_tagger.model_name` / `general_threshold` / `character_threshold` / `prepend_tags` / `exclude_tags`）

## 実装ロードマップ（案）

まだ着手前のため、以下は暫定的なロードマップ。実装開始前に方針を確認すること。

### フェーズ1: ロジック配置の検討・ComfyUILibs の拡張

`Wd14TaggerRunner`（単一画像のタグ取得）は実装済みだが、ディレクトリ走査・タグフィルタ（prepend/exclude）・タグ集計レポートに相当するロジックは `ComfyUILibs` にまだ存在しない。
「UI・プレゼンテーション層を含まないビジネスロジックは `ComfyUILibs` に置く」という責務分離の原則に従うなら、Python版 `CaptioningTool` クラス相当のロジック（`_apply_tag_filters` / `_collect_all_tags` 等）を `ComfyUILibs` 側に新設する形が候補となる。

### フェーズ2: ディレクトリ一括タグ付け実行ページ（`MainPage` を置換）

- ディレクトリ選択・再帰処理オプション・上書きオプション・prepend/exclude タグ入力
- バッチ実行の進捗表示（例: `[1/42] photo.jpg → OK`）・完了サマリ（処理数/スキップ数/エラー数）

### フェーズ3: SettingsPage の拡張

- ComfyUI URL、WD14 モデル名・しきい値（general/character threshold）
- デフォルトの prepend/exclude タグ

### フェーズ4: 処理結果・タグ集計レポート表示（`DataPage` を置換）

- バッチ実行結果の一覧表示
- タグ集計レポート（`tags_report.txt` 相当）の表示・再生成

### フェーズ5: 多言語化（実装ロードマップに先行して完了済み）

- `.resx` + `LocalizationManager` の仕組みは移植・接続済み（詳細は上記「多言語化」節を参照）。
- フェーズ2〜4 で新設する画面・ViewModel の文言は、実装のたびに `Strings.resx`/`Strings.en.resx` へキーを追加していくこと。

### 将来的な拡張

- `doc/` ディレクトリ（使い方ドキュメント・クラス図）の整備
