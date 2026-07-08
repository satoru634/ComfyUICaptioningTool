# 実装状況

## 現在の状態（2026-07-08 時点）

フェーズ1（`ComfyUILibs` への `CaptioningService` 新設）・フェーズ2（`MainPage` のディレクトリ一括タグ付け実行ページへの置換）・フェーズ3（`SettingsPage` へのデフォルト prepend/exclude タグ追加）が実装完了。`DataPage` はまだテンプレート由来のサンプル実装（ランダムカラー一覧）のまま。

`ComfyUILibs`（別リポジトリ）は Python版 `run_workflow` 相当のロジック（`WorkflowRunner` / `ConfigLoader` / `WorkflowBuilder` / `ComfyUIClient` / `Wd14TaggerRunner` / `PreviewImageCacheService` / `CaptioningService` 等）を実装済み・master マージ済み。詳細は `ComfyUILibs/.claude/implementation_status.md` を参照。

`ViewModels/Windows/MainWindowViewModel.cs` の `ApplicationTitle` がテンプレート由来の `"ComfyUIRunWorkflow"` になっていた不整合は修正済み。

### 多言語化（実装ロードマップに先行して着手・完了）

`ComfyUIRunWorkflow` の仕組み（`LocalizationManager` + `Strings.resx`/`Strings.en.resx`）を移植し、日本語（既定）／英語の切り替えに対応した。

- `Helpers/LocalizationManager.cs`、`Resources/Strings.resx`・`Strings.en.resx`・`Strings.cs` を新設。
- `Services/ApplicationHostService.cs` で起動時に `Config.Data.Language` から表示言語を適用。
- `ViewModels/Pages/SettingsViewModel.cs` の言語切替（`OnSelectedLanguageChanged`）を有効化（コメントアウト解除）し、再起動不要で即時反映。
- `ViewModels/Windows/MainWindowViewModel.cs` のナビゲーションメニュー項目（`MenuItems`/`FooterMenuItems`/`TrayMenuItems`）を `BuildMenuItems()` で `LocalizationManager` から構築し、言語切替時に再構築するよう変更。
- `Views/Pages/SettingsPage.xaml` のラベルを `LocalizationManager` へのインデクサーバインディングに置き換え。
- 翻訳キーは `MainWindow_*`・`Settings_*`・`Common_*`・`Main_*`（フェーズ2で追加したディレクトリ一括タグ付けページの文言）を用意済み。`DataPage` はテンプレート由来のプレースホルダーのため未翻訳（フェーズ4の置き換え時に追加する）。
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

### フェーズ1: ロジック配置の検討・ComfyUILibs の拡張（実装完了・マージ済み）

方針検討の結果、ディレクトリ走査・タグフィルタ（prepend/exclude）・タグ集計レポートは UI 非依存のビジネスロジックであり、将来の Discord ボットからの再利用も見込めるため、`ComfyUILibs`（`../ComfyUIRunWorkflow/ComfyUILibs/` 側の実体）に `Services/CaptioningService.cs` として新設した（[PR #15](https://github.com/satoru634/ComfyUILibs/pull/15) にてマージ済み）。Python版 `CaptioningTool` クラス相当のロジック（`_apply_tag_filters` → `ApplyTagFilters`、`_collect_all_tags` → `CollectAllTags`、`process_directory` → `ProcessDirectoryAsync`、`generate_report` → `GenerateReportAsync`）を移植済み。詳細・設計判断は `ComfyUILibs/.claude/implementation_status.md`（フェーズ3）を参照。

- `CaptioningService` はコンストラクターで `Wd14TaggerRunner` と prepend/exclude タグ（呼び出し側で config + 追加指定の union を解決済みのもの）を受け取る。自前で設定ファイルは読み込まない
- ディレクトリ一括処理の進捗は `IProgress<CaptioningProgress>` で 1 ファイルごとに通知する方式（フェーズ2 の GUI 進捗表示 `[1/42] photo.jpg → OK` はこれを購読して実装した）
- 画像 1 枚の処理中の例外はすべて捕捉して `CaptioningResult.Error` として継続する。`ProcessDirectoryAsync` 自体が例外を送出するのは指定ディレクトリが存在しない場合のみ
- `ComfyUILibsTests/Services/CaptioningServiceTests.cs`（13件）を新規作成、全件パス確認済み（既存分含め合計175件）
- 本プロジェクト（`ComfyUICaptioningTool` 側の `ComfyUILibs` submodule）のポインタ更新は未実施（ビルドには影響しない。詳細は `.claude/tech_stack.md` の「ComfyUILibs の参照経路に注意」を参照）

### フェーズ2: ディレクトリ一括タグ付け実行ページ（`MainPage` を置換、実装完了）

`MainPage`/`MainPageViewModel` をテンプレート由来のカウンターデモから、ディレクトリ一括タグ付け実行画面に置き換えた。

- 対象ディレクトリ選択（`Microsoft.Win32.OpenFolderDialog`）・再帰処理（Recursive）・上書き（Overwrite）・完了後のタグ集計レポート生成（GenerateReport）のオプション、先頭追加/除外タグ（カンマ区切りテキスト入力。フェーズ3で `AppConfig` 側のデフォルト値との union に対応済み）
- 実行時は `CaptioningService.ProcessDirectoryAsync` を呼び出し、`IProgress<CaptioningProgress>` 経由で 1 ファイルごとに `[現在/合計] ファイル名 → OK/SKIP/ERROR` 形式のログ行を `LogEntries`（`ObservableCollection<string>`）に追加。完了後は `完了: 処理 N, スキップ N, エラー N` 形式のサマリを表示し、`GenerateReport` チェック時は `GenerateReportAsync` も呼び出す
- **ComfyUI 接続設定の配線**: `CaptioningService` は `Wd14TaggerRunner` を必要とするため、`ComfyUIRunWorkflow` と同じ方式（外部 `workflow_config.json` をパスで指定し、GUI 内では値を直接編集しない）を採用した。`AppConfig.ConfigPath` を新設し、`SettingsPage` にファイル選択ダイアログのカードを追加（`SettingsViewModel.BrowseConfigPathCommand`）。`MainPageViewModel` はページ遷移のたびに `Config.Data.ConfigPath` から `Wd14TaggerRunner` を再読み込みする（`TaggerViewModel`（ComfyUIRunWorkflow）と同じパターン）
- **テスト容易性のための境界**: `Wd14TaggerRunner`/`CaptioningService` は内部コンストラクターがテストプロジェクトから不可視（`InternalsVisibleTo` は `ComfyUILibsTests` のみに付与）なので、ネットワーク通信を伴う `CaptioningService` をテストから差し替えられるよう、本プロジェクト側に `Services/ICaptioningService.cs`（`ProcessDirectoryAsync`/`GenerateReportAsync` のみを抜き出したインターフェース）と `Services/CaptioningServiceAdapter.cs`（実装ラッパー）を新設した。`MainPageViewModel` はこのファクトリーを DI コンストラクター引数（既定値あり）として受け取る
- `System.Progress<T>` は `SynchronizationContext` 経由でコールバックを非同期配送するためテストが不安定になる。`MainPageViewModel` 内に同期的にコールバックを呼ぶ `SynchronousProgress<T>`（private nested class）を定義して使用している（本 ViewModel の `await` はすべて UI スレッドのコンテキストを捕捉して再開するため、同期呼び出しでも実害はない）
- `ComfyUICaptioningToolTests`（`ViewModels/Pages/MainPageViewModelTests.cs` 全面書き換え、`Fakes/FakeCaptioningService.cs` 新設、`Models/AppConfigTests.cs` に `ConfigPath` のテスト追加）で計86件、全件パス確認済み。スナックバー表示（`SymbolIcon` 生成）を伴うテストは STA スレッドが必要なため `RunOnSta`（`MainWindowViewModelTests` の非同期版）でラップしている
- 実アプリを起動してスクリーンショットで見た目を確認済み（`実行` ボタンは `ConfigPath` 未設定・ディレクトリ未選択の状態では無効化される。進捗バーは `ProgressTotal` が 0 のままだと `Maximum=0` により満杯表示になってしまうため、`HasProgress`（`ProgressTotal > 0`）で実行前は非表示にする対応を追加した）

### フェーズ3: SettingsPage の拡張（実装完了）

- ComfyUI URL・WD14 モデル名・しきい値は、フェーズ2で採用した外部 `workflow_config.json` 方式（`AppConfig.ConfigPath` + ファイル選択ダイアログ）により GUI 内での直接編集は行わない方針としたため、本フェーズの対象外
- `AppConfig` に `DefaultPrependTags`/`DefaultExcludeTags`（カンマ区切り文字列、既定は空文字）を追加し、`SettingsPage` に「タグフィルタの既定値」カード（`MainPage` のタグ入力カードと同じ見た目の 2 つの `ui:TextBox`、`Config.Data.DefaultPrependTags`/`DefaultExcludeTags` へ直接 TwoWay バインド）を新設した。`ConfigPath` と同様、`SettingsViewModel` 側に専用プロパティ・`OnChanged` は設けず単純バインディングのみ
- `MainPageViewModel.RunAsync` で `MergeTags(既定値, MainPage 入力値)` を呼び出し、既定値を先頭にした union（大文字小文字無視で重複排除）を `CaptioningService`（`ICaptioningService` ファクトリー経由）に渡すよう変更した。同じタグが既定値と入力値の両方にある場合でも、タグフィルタ適用後の出力に二重挿入されない
- `ComfyUICaptioningToolTests`: `Models/AppConfigTests.cs` に `DefaultPrependTags`/`DefaultExcludeTags` のデフォルト値・`PropertyChanged` テストを追加、`ViewModels/Pages/MainPageViewModelTests.cs` に union の順序・重複排除を検証するテストを追加。計92件、全件パス確認済み
- 実アプリで `MainPage` の表示を再確認済み（進捗バー等に回帰なし）。`SettingsPage` は座標指定でのクリック操作が別ウィンドウを誤操作してしまう問題が2度発生したため実画面確認は行わず、既に動作確認済みの `ConfigPath` カードと同一の XAML 構造であることのコードレビューで代替した

### フェーズ4: 処理結果・タグ集計レポート表示（`DataPage` を置換）

- バッチ実行結果の一覧表示
- タグ集計レポート（`tags_report.txt` 相当）の表示・再生成

### フェーズ5: 多言語化（実装ロードマップに先行して完了済み）

- `.resx` + `LocalizationManager` の仕組みは移植・接続済み（詳細は上記「多言語化」節を参照）。
- フェーズ2〜4 で新設する画面・ViewModel の文言は、実装のたびに `Strings.resx`/`Strings.en.resx` へキーを追加していくこと。

### 将来的な拡張

- `doc/` ディレクトリ（使い方ドキュメント・クラス図）の整備
