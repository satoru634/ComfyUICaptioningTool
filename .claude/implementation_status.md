# 実装状況

## 現在の状態（2026-08-02 時点）

フェーズ1（`ComfyUILibs` への `CaptioningService` 新設）・フェーズ2（`MainPage` のディレクトリ一括タグ付け実行ページへの置換）・フェーズ3（`SettingsPage` へのデフォルト prepend/exclude タグ追加、フェーズ6で廃止）・フェーズ4（`DataPage` の実行結果・タグ集計レポート表示ページへの置換）・フェーズ6（既定 prepend/exclude タグの保持先を `captioning_config.json` に一本化）・フェーズ8（`ConfigPage` による captioning_config.json 直接編集）・フェーズ9（`MainPage` 実行成功時の実行結果設定 JSON `captioning_config_result.json` 出力）・フェーズ10（`DataPage` を実行結果表示専用ページに簡素化し、タグ集計レポートを新規 `ReportPage` に分離）・フェーズ11（実行ログ + 使用した設定をマージした結果ログ `captioning_result_*.json` を `Results` フォルダへ出力）・フェーズ12（`DataPage` を `Results` フォルダの `captioning_result_*.json` 一覧表示に変更し、直近 1 件のみだった `CaptioningRunResultStore` 方式を廃止）・フェーズ13（画像とタグ一覧をカード表示する新規 `GalleryPage` の新設）・フェーズ14（`GalleryPage` へのタグ編集機能（追加・削除、カード単位＋一括操作）の追加）・フェーズ15（`GalleryPage` のタグ編集を `captioning_config_result.json` へ反映）・フェーズ16（`ReportViewModel` のタグ集計レポート生成ロジックを `Services/TagReportGenerator.cs` へ抽出）・フェーズ17（`GalleryPage` の一括タグ操作入力欄を `ui:AutoSuggestBox` 化し、`TagReportGenerator` から取得したタグ一覧を候補表示する `TagList` を追加）・フェーズ18（`GalleryPage` カード単位のタグ追加入力欄に「先頭に追加」ボタンを追加）・フェーズ19（`GalleryPage` 一括タグ操作にも「先頭に追加」ボタンを追加）・フェーズ20（`MainPage` タグフィルタへの他 captioning_config.json からのタグインポート機能追加）・フェーズ21（`GalleryPage` カード単位のタグ一覧をクリップボードへコピーするボタンを追加）・フェーズ22（`GalleryPage` カード単位のタグ一覧をトグルボタン化し、選択タグの一括削除ボタンを追加）・フェーズ23（`GalleryPage` を画像タイル一覧＋選択画像のタグ編集パネルの2ペイン構成に変更）・フェーズ24（`ReportPage` にタグ名でのインタラクティブフィルタリング機能を追加）・フェーズ25（publish 設定の整備）・フェーズ26（`GalleryPage` 右ペインの選択タグ並び替え機能を追加）・フェーズ27（`GalleryPage` のタグ操作作業ログ `gallery_edit_log.jsonl` 出力）が実装完了。テンプレート由来のサンプル実装は残っていない。

`ComfyUILibs`（別リポジトリ）は Python版 `run_workflow` 相当のロジック（`WorkflowRunner` / `ConfigLoader` / `WorkflowBuilder` / `ComfyUIClient` / `Wd14TaggerRunner` / `PreviewImageCacheService` / `CaptioningService` 等）を実装済み・master マージ済み。詳細は `ComfyUILibs/.claude/implementation_status.md` を参照。

`ViewModels/Windows/MainWindowViewModel.cs` の `ApplicationTitle` がテンプレート由来の `"ComfyUIRunWorkflow"` になっていた不整合は修正済み。

### 多言語化（実装ロードマップに先行して着手・完了）

`ComfyUIRunWorkflow` の仕組み（`LocalizationManager` + `Strings.resx`/`Strings.en.resx`）を移植し、日本語（既定）／英語の切り替えに対応した。

- `Helpers/LocalizationManager.cs`、`Resources/Strings.resx`・`Strings.en.resx`・`Strings.cs` を新設。
- `Services/ApplicationHostService.cs` で起動時に `Config.Data.Language` から表示言語を適用。
- `ViewModels/Pages/SettingsViewModel.cs` の言語切替（`OnSelectedLanguageChanged`）を有効化（コメントアウト解除）し、再起動不要で即時反映。
- `ViewModels/Windows/MainWindowViewModel.cs` のナビゲーションメニュー項目（`MenuItems`/`FooterMenuItems`/`TrayMenuItems`）を `BuildMenuItems()` で `LocalizationManager` から構築し、言語切替時に再構築するよう変更。
- `Views/Pages/SettingsPage.xaml` のラベルを `LocalizationManager` へのインデクサーバインディングに置き換え。
- 翻訳キーは `MainWindow_*`・`Settings_*`・`Common_*`・`Main_*`（フェーズ2で追加したディレクトリ一括タグ付けページの文言）・`Data_*`（フェーズ4で追加した実行結果・タグ集計レポートページの文言）を用意済み。
- `ComfyUICaptioningToolTests`（xUnit）プロジェクトを新設し、`ComfyUICaptioningTool.sln` に追加。`Helpers/LocalizationManagerTests.cs` でカルチャ切替・キー解決・フォールバックを検証済み（全件パス）。

## 移植元（Python版 captioning_tool）

`E:\Python_project\comfyui_tools\captioning_tool\`（詳細仕様: `captioning_tool/doc/SPEC.md`）。

指定ディレクトリ内の画像を WD Timm Tagger でタグ付けし、同名の `.txt` キャプションファイルをバッチ生成する CLI ツール。主な機能:

- ディレクトリ内画像の一括タグ付け（`.jpg` `.jpeg` `.png` `.webp`、`--recursive` でサブディレクトリ対応）
- 既存 `.txt` のスキップ / 上書き（`--overwrite`）
- 冒頭追記タグ（`--prepend`）・除外タグ（`--exclude`）によるタグフィルタ
- タグ集計レポート生成（`--report` → `tags_report.txt`）
- `config.json`（`comfyui_url` / `wd14_tagger.model_name` / `general_threshold` / `character_threshold` / `prepend_tags` / `exclude_tags`）

## 実装ロードマップ

フェーズ1〜4は実装完了・マージ済み。以降のフェーズに着手する場合も、方針が固まっていない点があれば実装開始前に確認すること。

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
- **ComfyUI 接続設定の配線**: `CaptioningService` は `Wd14TaggerRunner` を必要とするため、`ComfyUIRunWorkflow` と同じ方式（外部 `captioning_config.json` をパスで指定し、GUI 内では値を直接編集しない）を採用した。`AppConfig.ConfigPath` を新設し、`SettingsPage` にファイル選択ダイアログのカードを追加（`SettingsViewModel.BrowseConfigPathCommand`）。`MainPageViewModel` はページ遷移のたびに `Config.Data.ConfigPath` から `Wd14TaggerRunner` を再読み込みする（`TaggerViewModel`（ComfyUIRunWorkflow）と同じパターン）
- **テスト容易性のための境界**: `Wd14TaggerRunner`/`CaptioningService` は内部コンストラクターがテストプロジェクトから不可視（`InternalsVisibleTo` は `ComfyUILibsTests` のみに付与）なので、ネットワーク通信を伴う `CaptioningService` をテストから差し替えられるよう、本プロジェクト側に `Services/ICaptioningService.cs`（`ProcessDirectoryAsync`/`GenerateReportAsync` のみを抜き出したインターフェース）と `Services/CaptioningServiceAdapter.cs`（実装ラッパー）を新設した。`MainPageViewModel` はこのファクトリーを DI コンストラクター引数（既定値あり）として受け取る
- `System.Progress<T>` は `SynchronizationContext` 経由でコールバックを非同期配送するためテストが不安定になる。`MainPageViewModel` 内に同期的にコールバックを呼ぶ `SynchronousProgress<T>`（private nested class）を定義して使用している（本 ViewModel の `await` はすべて UI スレッドのコンテキストを捕捉して再開するため、同期呼び出しでも実害はない）
- `ComfyUICaptioningToolTests`（`ViewModels/Pages/MainPageViewModelTests.cs` 全面書き換え、`Fakes/FakeCaptioningService.cs` 新設、`Models/AppConfigTests.cs` に `ConfigPath` のテスト追加）で計86件、全件パス確認済み。スナックバー表示（`SymbolIcon` 生成）を伴うテストは STA スレッドが必要なため `RunOnSta`（`MainWindowViewModelTests` の非同期版）でラップしている
- 実アプリを起動してスクリーンショットで見た目を確認済み（`実行` ボタンは `ConfigPath` 未設定・ディレクトリ未選択の状態では無効化される。進捗バーは `ProgressTotal` が 0 のままだと `Maximum=0` により満杯表示になってしまうため、`HasProgress`（`ProgressTotal > 0`）で実行前は非表示にする対応を追加した）

### フェーズ3: SettingsPage の拡張（実装完了）

- ComfyUI URL・WD14 モデル名・しきい値は、フェーズ2で採用した外部 `captioning_config.json` 方式（`AppConfig.ConfigPath` + ファイル選択ダイアログ）により GUI 内での直接編集は行わない方針としたため、本フェーズの対象外（**この方針はフェーズ8で転換し、`ConfigPage` から直接編集できるようにした。詳細は下記フェーズ8を参照**）
- `AppConfig` に `DefaultPrependTags`/`DefaultExcludeTags`（カンマ区切り文字列、既定は空文字）を追加し、`SettingsPage` に「タグフィルタの既定値」カード（`MainPage` のタグ入力カードと同じ見た目の 2 つの `ui:TextBox`、`Config.Data.DefaultPrependTags`/`DefaultExcludeTags` へ直接 TwoWay バインド）を新設した。`ConfigPath` と同様、`SettingsViewModel` 側に専用プロパティ・`OnChanged` は設けず単純バインディングのみ
- `MainPageViewModel.RunAsync` で `MergeTags(既定値, MainPage 入力値)` を呼び出し、既定値を先頭にした union（大文字小文字無視で重複排除）を `CaptioningService`（`ICaptioningService` ファクトリー経由）に渡すよう変更した。同じタグが既定値と入力値の両方にある場合でも、タグフィルタ適用後の出力に二重挿入されない
- `ComfyUICaptioningToolTests`: `Models/AppConfigTests.cs` に `DefaultPrependTags`/`DefaultExcludeTags` のデフォルト値・`PropertyChanged` テストを追加、`ViewModels/Pages/MainPageViewModelTests.cs` に union の順序・重複排除を検証するテストを追加。計92件、全件パス確認済み
- 実アプリで `MainPage` の表示を再確認済み（進捗バー等に回帰なし）。`SettingsPage` は座標指定でのクリック操作が別ウィンドウを誤操作してしまう問題が2度発生したため実画面確認は行わず、既に動作確認済みの `ConfigPath` カードと同一の XAML 構造であることのコードレビューで代替した
- **本節の `AppConfig.DefaultPrependTags`/`DefaultExcludeTags` はフェーズ6で廃止済み。** 既定 prepend/exclude タグの保持先は `captioning_config.json`（`ComfyUILibs.Models.WorkflowConfig.PrependTags`/`ExcludeTags`）に一本化された。詳細は下記フェーズ6を参照

### フェーズ4: 処理結果・タグ集計レポート表示（`DataPage` を置換、実装完了）

方針検討の結果、「バッチ実行結果の一覧表示」は直近の実行結果のみを対象とし（過去実行の永続化・履歴一覧は対象外）、タグ集計レポートは `DataPage` で対象ディレクトリを選択して生成・表示する方式とした。

- **結果共有の仕組み**: `Models/CaptioningRunResult.cs`（実行結果スナップショットの positional record）と `Services/CaptioningRunResultStore.cs`（`LastResult` を保持する `ObservableObject` の DI シングルトン、`App.xaml.cs` に登録）を新設。`MainPageViewModel.RunAsync` は実行成功時（例外発生時は更新しない）に `_resultStore.LastResult` を更新する。`DataViewModel` はコンストラクターで `ResultStore.PropertyChanged` を購読し、`OnPropertyChanged(string.Empty)` で `HasLastResult`/`LastResultDirectory`/`LastResultTimestampText`/`LastResultSummary`/`LastResultLogEntries` などの導出プロパティを一括再通知する（MainPage で実行するたびに DataPage 側の表示も自動的に最新化される）
- **タグ集計レポート**: `DataPage` に対象ディレクトリ選択（`OpenFolderDialog`）・再帰オプション・「生成 / 更新」ボタンを配置。`DataViewModel` は `MainPageViewModel` と同じパターンで `Config.Data.ConfigPath` から `Wd14TaggerRunner` を読み込み（`ICaptioningService` ファクトリー経由、prepend/exclude タグは常に空リスト）、`GenerateReportAsync` 実行後に `tags_report.txt` を読み込んで `Models/TagCountEntry.cs`（`Tag`/`Count` の positional record）のリストへ変換し `ListView` で表示する。行の解析には正規表出現 `^(.*): (\d+)$` を使用し、`rating:general` のようにタグ名自体にコロンを含むケースでも末尾の出現回数だけを安全に切り出せるようにした
- `CaptioningService` の `GenerateReportAsync` は `Wd14TaggerRunner` を必要としない（ファイル I/O のみ）が、コンストラクター引数として必須なため、`DataPage` 単体でのレポート生成にも `ConfigPath` の設定が前提となる（`ComfyUILibs` 側の API 制約であり、本フェーズでは変更しない）
- テンプレート由来の `Models/DataColor.cs` は不要になったため削除
- `ComfyUICaptioningToolTests`: `ViewModels/Pages/DataViewModelTests.cs` を全面書き換え、`Services/CaptioningRunResultStoreTests.cs` を新設、`Fakes/FakeCaptioningService.cs` に `ThrowOnGenerateReport` を追加、`MainPageViewModelTests.cs` に `ResultStore` 更新の検証を追加。計111件、全件パス確認済み
- **テストインフラの修正**: `DataViewModel.GenerateReportAsync` は `File.ReadAllLinesAsync` という実際に非同期完了する I/O を含むため、`SynchronizationContext` を持たない素の STA スレッドで実行すると `await` の継続がスレッドプール（MTA）へ流れてしまい、その後の `SymbolIcon` 生成が失敗する不具合がテストで発生した（実際の WPF アプリの UI スレッドには `DispatcherSynchronizationContext` が存在するため本番コードには影響しない、テスト環境特有の問題）。`ComfyUICaptioningToolTests/TestSupport/StaTestRunner.cs` を新設し、`DispatcherSynchronizationContext` + `Dispatcher.PushFrame` によるメッセージポンプで継続を STA スレッドへ戻すよう修正。`MainPageViewModelTests`/`DataViewModelTests` の `RunOnSta` はこれに委譲する形に統一した。あわせて `TestSupport/StaThreadGate.cs`（テストクラス間で共有する lock）も導入
- 実アプリで `MainPage` の表示を再確認済み（回帰なし）。`DataPage` は座標指定でのクリック操作によるページ遷移の自動確認が3回連続で失敗（他ウィンドウを誤操作）したため実画面確認を断念し、ユニットテストとコードレビュー（`MainPage`/`SettingsPage` と同一の XAML パターン）で代替した

### フェーズ5: 多言語化（実装ロードマップに先行して完了済み）

- `.resx` + `LocalizationManager` の仕組みは移植・接続済み（詳細は上記「多言語化」節を参照）。
- フェーズ2〜4 で新設する画面・ViewModel の文言は、実装のたびに `Strings.resx`/`Strings.en.resx` へキーを追加していくこと。

### フェーズ6: 既定 prepend/exclude タグの保持先を captioning_config.json に一本化（`feature/prepend-exclude-tags-from-config` ブランチ、実装完了）

フェーズ3で `AppConfig.DefaultPrependTags`/`DefaultExcludeTags`（本プロジェクト側、カンマ区切り文字列）として持たせていた既定 prepend/exclude タグを廃止し、`comfyui_url`・`wd14_tagger` と同様に外部 `captioning_config.json`（`ConfigPath` が指すファイル）側の `prepend_tags`/`exclude_tags` キーで保持する方式に統一した。同ファイルには元々このキーが存在していたが、`ComfyUILibs.Models.WorkflowConfig` に対応するプロパティが無く読み込まれていなかった。

- **`ComfyUILibs`（別リポジトリ、`../ComfyUIRunWorkflow/ComfyUILibs/` 側の実体、`feature/prepend-exclude-tags-in-config` ブランチ）**:
  - `Models/WorkflowConfig.cs` に `PrependTags`/`ExcludeTags`（`List<string>?`、JSON プロパティ名 `prepend_tags`/`exclude_tags`）を追加。バリデーション対象外
  - `Services/Wd14TaggerRunner.cs` に `PrependTags`/`ExcludeTags`（`IReadOnlyList<string>`）を公開プロパティとして追加。キー欠落時は空リストを返す
  - `ComfyUILibsTests/Services/Wd14TaggerRunnerTests.cs` に4件追加（値あり/キー欠落 × PrependTags/ExcludeTags）、全件パス確認済み（合計179件）
  - `README.md`/`doc/README_english.md`/`doc/class_diagram.md`/`.claude/implementation_status.md` を更新（フェーズ4として記録）
- **`ComfyUICaptioningTool`（本プロジェクト）**:
  - `Models/AppConfig.cs` から `DefaultPrependTags`/`DefaultExcludeTags` を削除
  - `MainPageViewModel.RunAsync` の `MergeTags` 呼び出し元を `Config.Data.Default*Tags`（文字列）から `_taggerRunner.PrependTags`/`ExcludeTags`（`IReadOnlyList<string>`）に変更。`MergeTags` のシグネチャも `(string defaultsText, string extraText)` から `(IReadOnlyList<string> defaults, string extraText)` に変更した。MainPage 実行時の入力欄（`PrependTagsText`/`ExcludeTagsText`）は維持し、union（既定値を先頭、大文字小文字無視で重複排除）する挙動は変えていない
  - `SettingsPage.xaml` の「タグフィルタの既定値」カードを削除し、対応する `Strings.resx`/`Strings.en.resx` のキー（`Settings_TagFilterSectionLabel`/`Settings_DefaultPrependTagsLabel`/`Settings_DefaultExcludeTagsLabel`）も削除した
  - `DataPage`（タグ集計レポート生成）は今回の変更対象外。従来通り prepend/exclude タグは常に空リストでフィルタ無しの集計のまま
  - `ComfyUICaptioningToolTests`: `Models/AppConfigTests.cs` から `DefaultPrependTags`/`DefaultExcludeTags` 関連のテストを削除。`ViewModels/Pages/MainPageViewModelTests.cs` に `WriteConfigFileWithTags`（prepend_tags/exclude_tags を含む captioning_config.json を書き出すヘルパー）を追加し、union・重複排除を検証する2件のテストを config ファイル経由の検証に書き換えた。計107件、全件パス確認済み
- 本プロジェクト側の `ComfyUILibs` submodule のポインタ更新は、`ComfyUILibs` 側のブランチが実際にマージ・push されてから行う想定（ビルドには影響しない。詳細は `.claude/tech_stack.md` の「ComfyUILibs の参照経路に注意」を参照）

### フェーズ7: ComfyUILibs 参照パスの修正・Wd14TaggerRunner タグ取得リトライ（`fix/comfyuilibs-reference-path` ブランチ、実装完了）

`ComfyUICaptioningTool.csproj`/`ComfyUICaptioningTool.sln` の `ProjectReference` が、本プロジェクトが Git submodule として持つ `ComfyUICaptioningTool/ComfyUILibs/` ではなく、隣接する別リポジトリ `../ComfyUIRunWorkflow/ComfyUILibs/` を参照していた誤りを修正した（`.claude/tech_stack.md` の「ComfyUILibs の参照経路に注意」節も実態に合わせて更新済み）。あわせて、ディレクトリ一括タグ付け実行時に数枚に1枚程度の頻度で `Wd14TaggerRunner_OutputNotFound` エラーが発生する不具合（`ComfyUIClient.MonitorAsync` の完了検知直後は ComfyUI 側の history 反映が間に合わないことがある競合状態）を `ComfyUILibs` 側で修正し（[PR #17](https://github.com/satoru634/ComfyUILibs/pull/17) にてマージ済み）、本プロジェクトの submodule ポインタを更新した。

- [x] `ComfyUICaptioningTool.csproj` の `ProjectReference` を `..\..\ComfyUIRunWorkflow\ComfyUILibs\ComfyUILibs\ComfyUILibs.csproj` から `..\ComfyUILibs\ComfyUILibs\ComfyUILibs.csproj` に修正
- [x] `ComfyUILibs`（本プロジェクトの submodule）を `ffc9a86` → `f3e9a8c`（`Wd14TaggerRunner.ExtractTagsAsync` にリトライ処理を追加したコミット）に更新。詳細は `ComfyUILibs/.claude/implementation_status.md` のフェーズ5を参照
- [x] `dotnet build ComfyUICaptioningTool.sln` 成功（修正後の参照パス経由でビルドできることを確認済み）
- 上記の参照パス誤りにより、これまで本プロジェクトのビルドは `../ComfyUIRunWorkflow/ComfyUILibs/`（隣接する別リポジトリ内の実体）を参照していた。フェーズ1・4・6 の `ComfyUILibs` 側の変更（`CaptioningService` 新設・`WorkflowConfig.PrependTags`/`ExcludeTags` 追加等）自体は本プロジェクトの submodule（`ComfyUICaptioningTool/ComfyUILibs/`、`ffc9a86` まで）にも同内容が反映されていたため機能面の実害はなかったが、実際のビルドに使われていたのは常に隣接リポジトリ側の実体だった。今回のフェーズ7の不具合修正（Wd14TaggerRunner のタグ取得リトライ）は先に隣接リポジトリ側にのみ実装してしまっていたため、本プロジェクトの submodule 側にも同内容を反映した上でマージした（詳細は上記 PR #17 参照）

### フェーズ8: captioning_config.json 編集ページの新設（`feature/config-edit-page` ブランチ、実装完了）

フェーズ2/3で「comfyui_url・WD14 モデル名・しきい値・既定 prepend/exclude タグは GUI 内で直接編集せず、外部ファイルへのパス指定のみ行う」とした方針を転換し、`captioning_config.json` の内容を GUI から直接編集できる新規ページ `ConfigPage` を追加した。ナビゲーションメニューには「データ」と「設定」の間に独立ページとして追加した（`SettingsPage` への統合はしない）。

- `ViewModels/Pages/ConfigViewModel.cs` を新設。`Config.Data.ConfigPath` から `System.Text.Json` で直接 `ComfyUILibs.Models.WorkflowConfig` を読み書きする
  - **設計判断**: `ConfigViewModel` は ComfyUI との通信を一切行わないファイル I/O のみの処理のため、`MainPageViewModel`/`DataViewModel` が採用している `ICaptioningService` ファクトリー（ネットワーク通信テスト差し替え用の境界）は不要と判断し、導入していない
  - 読み込み: `Config.Data.ConfigPath` が空なら `Common_ConfigPathNotSet` でエラー表示。ファイルが存在しない場合はエラーにせず `new WorkflowConfig()`（空の新規設定）としてフォームへ反映し、`Config_NewFileNotice`（保存時に新規作成される旨）を表示する。JSON 不正時のみ `IsConfigLoaded=false` として保存を無効化する（`PropertyNameCaseInsensitive = true` で読み込み、`ComfyUILibs.Services.ConfigLoader` と同じ大文字小文字無視の挙動に合わせた）
  - 保存: `ComfyUiUrl` 空文字チェック後、`ComfyUILibs.Services.ConfigLoader.ValidateWd14TaggerConfig` でモデル名・しきい値（0.0〜1.0）を検証してから書き込む。シリアライズは `JsonIgnoreCondition.WhenWritingNull` を指定し、本ページで扱わない `default_workflow`/`workflows`（`WorkflowConfig` の他プロパティ、常に null）が出力に含まれないようにして、既存の `captioning_config.json` のフォーマット（`comfyui_url`/`wd14_tagger`/`prepend_tags`/`exclude_tags` のみ）を維持している
  - prepend/exclude タグは `MainPage` と同じカンマ区切りテキスト入力・分割ロジック（trim・空要素除去）で編集する。`MainPageViewModel.MergeTags` のような union 処理は不要（本ページが唯一の既定値の書き込み元のため）
- `Views/Pages/ConfigPage.xaml(.cs)` を新設。`SettingsPage`/`MainPage` と同じ Border カード + `ui:TextBox` の見た目、しきい値は `ui:NumberBox`（`Minimum="0"`/`Maximum="1"`、DashboardPage.xaml [ComfyUIRunWorkflow] で実績のあるプロパティのみ使用）
- `ViewModels/Windows/MainWindowViewModel.cs` の `BuildMenuItems()` に `MainWindow_MenuConfig` ナビゲーション項目（`DocumentSettings20` アイコン）を追加。`MenuItems`（データの次）に配置
- `App.xaml.cs` に `ConfigPage`/`ConfigViewModel` をシングルトン登録
- `Resources/Strings.resx`/`Strings.en.resx` に `MainWindow_MenuConfig`・`Config_*` キーを追加（`Main_TagsSectionLabel`/`Main_PrependTagsLabel`/`Main_ExcludeTagsLabel`/`Main_TagsPlaceholder`/`Settings_ComfyUISectionLabel`/`Common_ConfigPathNotSet` はページ間で再利用）
- `ComfyUICaptioningToolTests`: `ViewModels/Pages/ConfigViewModelTests.cs`（18件、ConfigPath 空/ファイル未存在/JSON不正時の挙動・SaveCommand の CanExecute・保存時バリデーション・null プロパティが出力されないことの検証）を新規作成。`ViewModels/Windows/MainWindowViewModelTests.cs` に Config ナビゲーション項目のテストを2件追加。計127件、全件パス確認済み
- 実アプリ起動を試みたが、この環境ではウィンドウハンドル取得後のスクリーンショットが無関係な別ウィンドウを捉えてしまう事象が発生した（`SettingsPage`/`DataPage` のフェーズ3/4で記録済みの「座標指定でのクリック操作が別ウィンドウを誤操作する」問題と同種の環境依存の制約）。そのため実画面での目視確認は断念し、`SettingsPage`/`MainPage` と同一の XAML カード構造であることのコードレビューとユニットテストで代替した

### フェーズ9: 実行結果設定 JSON（captioning_config_result.json）の出力（`feature/output-used-config-json` ブランチ、実装完了）

`MainPage` でのディレクトリ一括タグ付けが成功した際に、そのとき実際に使用した設定（`captioning_config.json` の内容 + `MainPage` 入力欄とのマージ後の prepend/exclude タグ）を対象ディレクトリ直下に記録として出力する機能を追加した。

- `ViewModels/Pages/MainPageViewModel.cs` に `SaveExecutedConfigAsync` を追加。`ProcessDirectoryAsync` 成功後（`GenerateReport` のチェック状態に関わらず常に）、`Config.Data.ConfigPath` の `captioning_config.json` を `System.Text.Json` で読み込み、`prepend_tags`/`exclude_tags` のみ `RunAsync` で計算済みの union 結果（`MergeTags` の戻り値）に差し替えて、対象ディレクトリ直下に固定ファイル名 `captioning_config_result.json` として書き出す
  - **設計判断**: `ConfigViewModel` と同じ理由（ComfyUI との通信を伴わないファイル I/O のみ）で `ICaptioningService` ファクトリー境界は使わず、`MainPageViewModel` 内で直接 `System.Text.Json` を扱う。読み込み/書き込みオプション（`PropertyNameCaseInsensitive`・`DefaultIgnoreCondition = WhenWritingNull`）も `ConfigViewModel` と同一にし、`default_workflow`/`workflows` 等の未使用フィールドを出力しない
  - 書き込み完了後、`LogEntries` に `Main_ConfigResultSavedFormat`（保存先パスを含む）のログ行を追加する（`tags_report.txt` 保存時の `Main_ReportSavedFormat` と同じパターン）
  - `Resources/Strings.resx`/`Strings.en.resx` に `Main_ConfigResultSavedFormat` を追加
- `ComfyUICaptioningToolTests`: `ViewModels/Pages/MainPageViewModelTests.cs` に、captioning_config.json の他フィールド（comfyui_url・wd14_tagger）がそのままコピーされ prepend_tags/exclude_tags のみマージ結果に差し替わることの検証・ログ行追加の検証・処理失敗時は出力されないことの検証を追加（3件）。既存の `RunCommand_Execute_ReportsProgressToLogEntries`（ログ件数の厳密一致を検証していたテスト）は新しいログ行が1件増える影響を受けるため件数期待値を修正した。計135件、全件パス確認済み

### フェーズ10: DataPage/ReportPage の分離（`feature/split-data-report-pages` ブランチ、実装完了）

`DataPage` が「直近の実行結果」と「タグ集計レポート」の 2 機能を1ページに同居させていたのを分割した。`DataPage` は実行結果表示専用、新設 `ReportPage` がタグ集計レポートの生成・表示を担当する。

- `ViewModels/Pages/DataViewModel.cs` を大幅に簡素化。`ResultStore`（`CaptioningRunResultStore`）から導出する `HasLastResult`/`LastResultDirectory`/`LastResultTimestampText`/`LastResultSummary`/`LastResultLogEntries` のみを保持する。レポート生成に必要だった `Config`・`ISnackbarService`・`ICaptioningService` ファクトリー・`Wd14TaggerRunner` の読み込み・`INavigationAware`（`OnNavigatedToAsync`/`OnNavigatedFromAsync`）はすべて `ReportViewModel` 側に移管したため削除した（ページ遷移時に行う処理がなくなったため `INavigationAware` は非実装）
- `ViewModels/Pages/ReportViewModel.cs` を新設。旧 `DataViewModel` のレポート関連ロジック（`TryLoadRunner`・`BrowseReportDirectoryCommand`・`GenerateReportCommand`/`GenerateReportAsync`・`ReportDirectory`/`ReportRecursive`/`IsGeneratingReport`/`ReportStatusText`/`ReportEntries`）をそのまま移設した
- `Views/Pages/DataPage.xaml` からタグ集計レポートのカードを削除し、直近の実行結果カードのみを残した
- `Views/Pages/ReportPage.xaml(.cs)` を新設。旧 `DataPage.xaml` のタグ集計レポートカードをそのまま移設し、`Report_Title` をページタイトルとして追加した
- `ViewModels/Windows/MainWindowViewModel.cs` の `BuildMenuItems()` に `MainWindow_MenuReport` ナビゲーション項目（`DataHistogram24` アイコン）を追加。`MenuItems` の「データ」の次に配置。「データ」項目のアイコンは実行結果専用になった意味合いに合わせて `History24` に変更した
- `App.xaml.cs` に `ReportPage`/`ReportViewModel` をシングルトン登録
- `Resources/Strings.resx`/`Strings.en.resx`: `MainWindow_MenuReport` を追加。`Data_Title` の文言を「実行結果・タグ集計レポート」→「実行結果」に変更。`Data_ReportSectionLabel`/`Data_GenerateReportButton`/`Data_ReportGeneratedFormat`/`Data_TagColumnHeader`/`Data_CountColumnHeader` は `Report_Title`/`Report_GenerateReportButton`/`Report_ReportGeneratedFormat`/`Report_TagColumnHeader`/`Report_CountColumnHeader` にリネームして `Report_*` セクションへ移設した（`Main_DirectoryLabel` 等の共有キーは変更なし）
- `ComfyUICaptioningToolTests`: `ViewModels/Pages/DataViewModelTests.cs` を実行結果関連のテストのみに全面書き換え（4件）。旧 `DataViewModelTests.cs` のレポート関連テストを `ViewModels/Pages/ReportViewModelTests.cs`（新設）へ移設。`ViewModels/Windows/MainWindowViewModelTests.cs` に Report ナビゲーション項目のテストを2件追加。計137件、全件パス確認済み

### フェーズ11: 実行結果ログ（captioning_result_*.json）の Results フォルダ出力（`feature/merged-result-log` ブランチ、実装完了）

`MainPage` の `LogEntries`（1 ファイルごとの処理結果ログ）と、フェーズ9で導入した実行結果設定 JSON（`captioning_config_result.json`、対象ディレクトリ直下に出力される「使用した設定」の記録）を1つにマージした結果ログを、`ComfyUIRunWorkflow` の `result_*.json`/`tag_result_*.json` と同じ方式で `Results` フォルダへ出力する機能を追加した。

- **出力先フォルダ**: `ComfyUIRunWorkflow` の `AppConfig.ResultsFolder` と同じ方式で `Models/AppConfig.cs` に `ResultsFolder`（既定値: `Directory.GetCurrentDirectory()` 直下の `Results`）を新設し、`SettingsPage` にフォルダ選択ダイアログのカード（`SettingsViewModel.BrowseResultsFolderCommand`）を追加した。`ConfigPath` カードと同じ見た目・パターンで実装（`Settings_OutputSectionLabel`「出力」セクションに配置）
- **出力タイミング**: `captioning_config_result.json`（成功時のみ、対象ディレクトリ直下）とは異なり、本機能は **成功・失敗どちらの場合も出力する**。`MainPageViewModel.RunAsync` を try/catch/finally に再構成し、`finally` から `SaveResultLogAsync` を呼び出す（`status`/`processed`/`skipped`/`errors`/`errorMessage`/`usedConfig` を try 内で更新しつつ finally で参照する構造）。`ComfyUIException` 以外の予期しない例外が発生した場合も finally は実行されるため結果ログは出力されるが、`errorMessage` は `ComfyUIException` 経由でのみ設定される（既存の仕様上の制約、フェーズ9以前から同様）
- **出力ファイル名**: `captioning_result_{yyyyMMdd_HHmmss}.json`（`ComfyUIRunWorkflow` の命名規則を踏襲）
- **JSON 構造**: `Models/CaptioningResultLog.cs`（positional record）を新設。`status`/`timestamp`/`directory`/`recursive`/`processed`/`skipped`/`errors`/`error`/`log_entries`（実行ログのスナップショット）に加え、`config`（`ComfyUILibs.Models.WorkflowConfig`、`captioning_config.json` の内容をベースに `prepend_tags`/`exclude_tags` のみ今回の実行で実際に使用したマージ結果へ差し替えたもの）をネストして持つ。JSON プロパティ名は `ComfyUILibs.Models.WorkflowResult`/`TagResult` と同じ snake_case（`[property: JsonPropertyName("...")]`）
  - `captioning_config.json` の読み込み・タグマージ処理は `MainPageViewModel.LoadConfigWithMergedTagsAsync`（新設）に切り出し、`SaveExecutedConfigAsync`（captioning_config_result.json 出力）と `SaveResultLogAsync`（本機能）の両方から同じ `WorkflowConfig` インスタンスを共有する（ファイル二重読み込みを避けるため、`RunAsync` の冒頭でこれを呼び出す形に変更した）
- **保存失敗時の扱い**: `ResultsFolder` が空文字の場合は `SaveResultLogAsync` は何もしない（`ComfyUIRunWorkflow` の `TrySaveResultAsync` と同じ方針）。保存処理自体が例外を投げた場合も `catch { }` で握りつぶし、実行結果（スナックバー表示・`IsRunning` 等）には影響させない
- `Resources/Strings.resx`/`Strings.en.resx` に `Settings_OutputSectionLabel`/`Settings_ResultsFolderLabel`/`Settings_ResultsFolderDialogTitle`/`Main_ResultLogSavedFormat` を追加
- `ComfyUICaptioningToolTests`: `Models/AppConfigTests.cs` に `ResultsFolder` の既定値・`PropertyChanged` テストを追加。`ViewModels/Pages/MainPageViewModelTests.cs` にテスト用 `CreateSetting()` ヘルパーが隔離した一時フォルダを `ResultsFolder` の既定値として設定するよう変更（実 CWD を汚さないため）。成功時に `captioning_result_*.json` が正しい内容（ネストした `config` を含む）で出力されること・保存ログ行が追加されること・`ThrowOnProcessDirectory` 時も `status="error"`/`error` メッセージ付きで出力されること・`ResultsFolder` 空文字時は例外にならず何も出力しないことを検証する4件を新規追加。既存の `RunCommand_Execute_ReportsProgressToLogEntries`（ログ件数の厳密一致を検証していたテスト）は新しいログ行が1件増える影響を受けるため件数期待値を修正した（フェーズ9と同様の対応）。計143件、全件パス確認済み

### フェーズ12: DataPage の実行結果ログ一覧表示化（`feature/data-page-result-list` ブランチ、実装完了）

フェーズ4以来 `DataPage` は「直近 1 件の実行結果」のみを `CaptioningRunResultStore`（`MainPage`/`DataPage` 間の DI シングルトン）経由で表示していたが、フェーズ11で `Results` フォルダへ実行結果ログ（`captioning_result_*.json`）が永続化されるようになったため、`ComfyUIRunWorkflow` の `DataPage`（`result_*.json`/`tag_result_*.json` をフォルダから読み込んで一覧表示する方式）に倣い、`AppConfig.ResultsFolder` 配下の `captioning_result_*.json` を新しい順に読み込んで複数件リスト表示する方式に変更した。

- **`CaptioningRunResultStore` の廃止**: `DataPage` がファイルから直接読み込むようになったことで、`MainPage`→`DataPage` 間の状態橋渡し役だった `Services/CaptioningRunResultStore.cs`・`Models/CaptioningRunResult.cs` は不要になったため削除した。`App.xaml.cs` の DI 登録、`MainPageViewModel` のコンストラクター引数・`RunAsync` 内の `_resultStore.LastResult` 更新処理、`ComfyUICaptioningToolTests/Services/CaptioningRunResultStoreTests.cs` も合わせて削除した
- **`ViewModels/Pages/DataViewModel.cs` の全面書き換え**: `INavigationAware` を実装し（`OnNavigatedToAsync` でページ遷移のたびに再読み込み）、`RefreshCommand`（更新ボタン）・`Results`（`ObservableCollection<CaptioningResultLogPreview>`、新しい順）・`StatusMessage`（結果フォルダ未設定/未存在/結果 0 件のいずれかの案内文言、正常時は空文字）・`IsLoading` を持つ。`ReportViewModel`/ComfyUIRunWorkflow の `DataViewModel.LoadResultsAsync` と同じパターンで、`Directory.GetFiles(folder, "captioning_result_*.json").OrderByDescending(f => f)`（ファイル名に埋め込まれたタイムスタンプの降順 = 新しい順）でファイル一覧を取得し、1 件ずつ `JsonSerializer.Deserialize<CaptioningResultLog>` で読み込む。個々のファイルの読み込み・パース失敗は `catch { }` でスキップし、一覧表示全体には影響させない（ComfyUIRunWorkflow と同じ方針）
- **`Models/CaptioningResultLogPreview.cs`（新設）**: `CaptioningResultLog`（読み込んだ生データ）と、一覧表示用に整形済みの `TimestampText`（`Data_LastRunTimestampFormat`）・`SummaryText`（成功時は `Main_SummaryFormat`、失敗時は `Log.Error`）をまとめた positional record。`IsSuccess`（`Log.Status == "success"`）も公開する
- **`Views/Pages/DataPage.xaml` の全面書き換え**: ヘッダー（タイトル + 更新ボタン）・読み込み中インジケーター（`ProgressBar IsIndeterminate`、標準の `BooleanToVisibilityConverter` を使用）・状態メッセージ・`ItemsControl` によるカード一覧（`ComfyUIRunWorkflow` の `DataPage.xaml` と同じカードデザイン: 成功/失敗のステータスアイコン・ディレクトリ・日時・サマリ/エラーメッセージ）で構成する。詳細ダイアログは新設せず、各カードに 1 ファイルごとの処理結果ログ（`Log.LogEntries`）もそのまま `MaxHeight="160"` のスクロール可能な `ListBox` として表示し、クリックなしで全情報を確認できるようにした（方針は実装前にユーザーに確認して決定）
- `Resources/Strings.resx`/`Strings.en.resx`: `Data_LastResultSectionLabel`/`Data_NoResultYet` を削除し、`Data_RefreshButtonContent`/`Data_ResultsFolderNotSet`/`Data_FolderNotFound_Format`/`Data_NoResults`（`ComfyUIRunWorkflow` の同名キーの文言を踏襲）を追加。`Data_LastRunTimestampFormat`/`Main_SummaryFormat` は一覧の各カードの表示にそのまま再利用した。`Data_Title` の英語訳を "Last Run Result" → "Run Results" に変更（日本語は「実行結果」のまま変更なし）
- `ComfyUICaptioningToolTests`: `ViewModels/Pages/DataViewModelTests.cs` を全面書き換え（`ResultsFolder` 未設定/未存在/結果 0 件時の状態メッセージ・`captioning_result_*.json` の新しい順読み込みと成功/失敗の表示文字列・不正な JSON ファイルのスキップ・`RefreshCommand` による再読み込みを検証、計9件）。`ViewModels/Pages/MainPageViewModelTests.cs` から `CaptioningRunResultStore` 関連の2件（`RunCommand_Execute_Success_UpdatesResultStoreLastResult`/`RunCommand_Execute_ServiceThrows_DoesNotUpdateResultStore`）を削除し、コンストラクター呼び出しから `_resultStore` 引数を除去した。計142件、全件パス確認済み

### フェーズ13: 画像・タグ一覧ページ（GalleryPage）の新設（`feature/gallery-page` ブランチ、実装完了）

「タグ付け対象の画像と、その画像に付けられたタグ一覧を並べたカードを表示するページが欲しい」というユーザー要望を受けて新設した。既存の `DataPage`（実行結果ログ一覧）・`ReportPage`（タグ集計レポート）はいずれもタグ本体（画像 1 枚ごとの実際のタグ文字列）を画像と並べて見せる機能を持たず、`CaptioningResultLog`/`CaptioningProgress` にもタグ本体は記録されていない。そのため対象ディレクトリ内の画像と同名の `.txt` をその場で読み込んで表示する方式とした。実装前にユーザーへ以下を確認して方針を確定した: (1) 対象ディレクトリは `MainPage`/`ReportPage` と同様に都度フォルダ選択ダイアログで指定する（DataPage の実行結果一覧との連携は今回は行わない）、(2) `.txt` が存在しない（未タグ付け）画像も一覧に含め「タグ未生成」と表示する、(3) タグはカード上では読み取り専用（編集は対象外）、(4) ナビゲーションメニュー上は「データ」と「レポート」の間に配置する。

- `Models/GalleryImageEntry.cs`（新設）: `FileName`/`FullPath`/`Tags`（`IReadOnlyList<string>`）/`Thumbnail`（`BitmapImage?`）の positional record。`HasTags`（`Tags.Count > 0`）を派生プロパティとして持つ
- `ViewModels/Pages/GalleryViewModel.cs`（新設）: `TargetDirectory`/`Recursive`/`IsLoading`/`StatusMessage`/`Images`（`ObservableCollection<GalleryImageEntry>`）を持つ。`BrowseDirectoryCommand`（`MainPageViewModel.BrowseDirectory`/`ReportViewModel.BrowseReportDirectory` と同じ `OpenFolderDialog` パターン）・`LoadCommand`（`CanExecute`: `!IsLoading && TargetDirectory` 未空）を実装
  - **`ICaptioningService` ファクトリー境界は導入していない**: 本ページは ComfyUI との通信を一切行わない（ファイルシステムの走査のみ）ため、`ConfigPage`/`ReportViewModel` が採用している「ネットワーク通信を伴う処理のみテスト用境界を設ける」という既存方針により不要と判断した。`Wd14TaggerRunner`/`Config.Data.ConfigPath` にも依存しない
  - **画像収集ロジック**（`LoadAsync`→`CollectEntries`、`Task.Run` でバックグラウンド実行）: 対応拡張子 `.jpg`/`.jpeg`/`.png`/`.webp` を `Recursive` に応じて収集しファイル名昇順でソート（`ComfyUILibs.Services.CaptioningService.CollectImageFiles` と同じ順序）。この拡張子一覧は `CaptioningService` 側が `internal` のため参照できず、GUI 側の表示専用ロジックとして `GalleryViewModel` 内に複製している（`ConfigViewModel` が `ConfigLoader` とは別に自前の JSON オプションを持つのと同じ「ComfyUILibs 側を変更しない・GUI 固有の表示ロジックは GUI 側に閉じる」という既存方針に合わせた）。同様に、同名 `.txt` の内容をカンマ区切りで分割・trim・空要素除去するロジック（`SplitTags`）も `MainPageViewModel.SplitTags` と同一の実装を複製している（重複除去はしない仕様。`.txt` が存在しない画像は空リストとして扱う）
  - **サムネイル生成**: `File.ReadAllBytes` → `MemoryStream` → `BitmapImage`（`DecodePixelWidth=200`・`CacheOption=OnLoad`）を生成して `Freeze()` してから返す（Freeze 済みならバックグラウンドスレッドで生成した `BitmapImage` を UI スレッドへ安全に渡せる）。デコードに失敗した場合（画像として不正なファイル等）は `catch` して `Thumbnail=null` とし、そのエントリ自体は一覧に残したまま処理を継続する（`CaptioningService.ProcessImageAsync` の「1 件の失敗で全体を止めない」という既存方針と同じ考え方）
  - スナックバー（`ISnackbarService`）は使用せず、`DataViewModel` と同じ `StatusMessage`/`IsLoading` によるインライン表示のみで完結させている（`SymbolIcon` 生成を伴わないため xUnit テストに STA スレッドが不要という副次的な利点もある）
- `Views/Pages/GalleryPage.xaml(.cs)`（新設）: `ReportPage.xaml`（ディレクトリ選択カード部分）と `DataPage.xaml`（`ProgressBar IsIndeterminate`・`StatusMessage`・`ItemsControl` カード一覧部分）の構成を踏襲。画像カード一覧は `WrapPanel` を `ItemsPanelTemplate` に指定して折り返し表示し、各カードにサムネイル（`Thumbnail` が null の場合は `SymbolIcon Image24` のプレースホルダーに切り替え、null 判定は `DataTrigger Binding="{Binding Thumbnail}" Value="{x:Null}"` で行う）・ファイル名・タグ一覧（`HasTags` に応じてチップ風 `ItemsControl` または `Gallery_NoTagsLabel`（「タグ未生成」）を表示）を配置した
- ナビゲーション登録: `ViewModels/Windows/MainWindowViewModel.cs` の `BuildMenuItems()` に `MainWindow_MenuGallery` ナビゲーション項目（アイコン `SymbolRegular.ImageMultiple24`）を「データ」の次・「タグ集計レポート」の前に追加。`App.xaml.cs` に `GalleryPage`/`GalleryViewModel` をシングルトン登録
- `Resources/Strings.resx`/`Strings.en.resx` に `MainWindow_MenuGallery`・`Gallery_Title`/`Gallery_LoadButtonContent`/`Gallery_FolderNotFound_Format`/`Gallery_NoImages`/`Gallery_NoTagsLabel` を追加（`Main_DirectoryLabel`/`Main_DirectoryPlaceholder`/`Main_DirectoryDialogTitle`/`Main_RecursiveLabel`/`Common_BrowseButtonTooltip` は既存キーを再利用）
- `ComfyUICaptioningToolTests`: `ViewModels/Pages/GalleryViewModelTests.cs`（新設、13件。初期状態・`LoadCommand` の `CanExecute`・ディレクトリ未存在/画像0件時のメッセージ・タグの trim/空要素除去（重複は保持したままであることも含む）・`.txt` なし画像の `HasTags=false`・非対応拡張子の除外・`Recursive` の有無によるサブディレクトリ画像の包含/除外・ファイル名昇順ソート・不正な画像バイト列でも `Thumbnail=null` のままエントリが一覧に残ることを検証）。`ViewModels/Windows/MainWindowViewModelTests.cs` に `MenuItems_Contains_GalleryItem`/`MenuItems_GalleryItem_TargetPage_IsGalleryPage` の2件を追加。計157件、全件パス確認済み
- 実アプリでの目視確認は、この環境で過去のフェーズ（3・4・8・10）から繰り返し発生している「座標指定でのクリック操作・スクリーンショットが無関係な別ウィンドウを誤操作/誤取得する」という既知の環境依存の制約により今回も断念し、`DataPage.xaml`/`ReportPage.xaml` と同一の XAML パターンであることのコードレビューとユニットテストで代替した

### フェーズ14: GalleryPage へのタグ編集機能（追加・削除）の追加（`feature/gallery-tag-edit` ブランチ、実装完了）

「タグの編集機能（追加と削除）を追加したい」というユーザー要望を受けて追加した。実装前に候補（`GalleryPage` を拡張する／新規専用ページを作る／`MainPage` に統合する）を提示して確認し、既に画像とタグをカードで並べて表示している `GalleryPage`（フェーズ13で「タグは読み取り専用」として実装）を拡張する方針を採用した。あわせて「カード単位のインライン編集」に加えて「読み込み済み全画像に対する一括タグ追加・削除」も欲しいとの要望があったため両方実装し、変更はいずれも即時に同名 `.txt` へ保存する方式とした。

- **`Models/GalleryImageEntry.cs` を positional record から `ObservableObject` 継承の `partial class` に変更**: `Tags` を `IReadOnlyList<string>`（イミュータブル）から `ObservableCollection<string>` に変更し、`CollectionChanged` を購読して `HasTags` の変更通知を行う。カード単位のタグ追加・削除・同名 `.txt` への保存をこのクラス自身に持たせた
  - `AddTag(string tag)`: trim 後に空文字なら追加しない。既存タグと大文字小文字無視で重複する場合も追加しない（`MainPageViewModel.MergeTags` が採用している `StringComparer.OrdinalIgnoreCase` の重複排除方針に合わせた）。追加後は即座に `SaveTags()` を呼ぶ
  - `RemoveTag(string tag)`（`[RelayCommand]`、カードのタグチップ削除ボタン用。`GalleryViewModel` の一括削除からも直接呼び出す）: `Tags.Remove(tag)` が成功した場合のみ `SaveTags()` を呼ぶ
  - `AddNewTag()`（`[RelayCommand]`、private。カードの「タグを追加」入力欄用）: `NewTagInput`（新設 `[ObservableProperty]`）の内容を `AddTag` に渡してから入力欄をクリアする
  - `SaveTags()`: `Tags` をカンマ区切りで同名 `.txt` へ書き込む。**`Tags.Count == 0` になった場合は `.txt` ファイル自体を削除する**（空ファイルとして残さない設計判断。削除後は `HasTags=false` となり「タグ未生成」表示に戻る）
- **`ViewModels/Pages/GalleryViewModel.cs` に一括タグ操作を追加**: `BulkTagInput`（`[ObservableProperty]`、一括操作対象のタグ入力欄）・`BulkAddTagCommand`/`BulkRemoveTagCommand`（`CanExecute`: `Images.Count > 0 && !string.IsNullOrWhiteSpace(BulkTagInput)`）を新設
  - `BulkAddTagCommand`: 読み込み済み `Images` の全エントリに対して `entry.AddTag(BulkTagInput)` を呼ぶ（重複排除・trim は `GalleryImageEntry.AddTag` 側の挙動にそのまま従う）
  - `BulkRemoveTagCommand`: 各エントリの `Tags` から `BulkTagInput` と大文字小文字無視で一致する要素を**すべて**列挙してから `entry.RemoveTag` する（`Tags` は重複除去しない仕様のため、同じタグが複数あるケースを考慮して一致分を全件削除する。1 件のみ削除すると「消したつもりのタグが残る」という直感に反する挙動になるため）
  - 実行後はいずれも `BulkTagInput` をクリアする
- **`Views/Pages/GalleryPage.xaml` の変更**:
  - ページ上部（対象ディレクトリ選択カードの下）に「一括タグ操作」カードを新設（テキストボックス + 「全てに追加」/「全てから削除」ボタン、`ViewModel.BulkTagInput`/`BulkAddTagCommand`/`BulkRemoveTagCommand` にバインド）。既存の各 `Grid.Row` インデックスは 1 つずつ繰り下げた
  - 各画像カードのタグチップ（`Border` + `TextBlock`）に削除ボタン（`×` の `ui:Button`、`Appearance="Transparent"`）を追加。`ItemsControl.ItemTemplate` 内から親 `ItemsControl`（`DataContext` が `GalleryImageEntry`）の `RemoveTagCommand` を `RelativeSource={RelativeSource AncestorType=ItemsControl}` で参照し、`CommandParameter` にタグ文字列自身を渡す
  - タグ一覧の `ItemsControl` を `ScrollViewer` で包み、カード下部に「タグを追加」用の `ui:TextBox`（`NewTagInput` にバインド、`Enter` キーで `AddNewTagCommand` を実行する `KeyBinding` 付き）+ 追加ボタンを新設。タグ一覧が `Grid.RowSpan="2"` で専有していた領域を Row0（タグ一覧）/Row1（追加欄）に分割したため、カード全体の `Height` を `248` → `296` に拡張した
- `Resources/Strings.resx`/`Strings.en.resx` に `Gallery_BulkTagSectionLabel`/`Gallery_BulkTagPlaceholder`/`Gallery_BulkAddButtonContent`/`Gallery_BulkRemoveButtonContent`/`Gallery_AddTagPlaceholder`/`Gallery_RemoveTagTooltip` を追加
- `ComfyUICaptioningToolTests`:
  - `Models/GalleryImageEntryTests.cs`（新設、11件）: コンストラクターの初期状態、`AddTag`（trim・重複排除・大文字小文字無視・既存タグへの追記・空文字無視）と対応する `.txt` 書き込み内容の検証、`RemoveTag`（存在しないタグの無視・存在するタグの削除・最後の1件削除時に `.txt` 自体が削除されること）、`AddNewTagCommand` 実行時に `NewTagInput` が反映されクリアされることを検証
  - `ViewModels/Pages/GalleryViewModelTests.cs` に一括操作のテストを追加（6件）: `BulkAddTagCommand`/`BulkRemoveTagCommand` の `CanExecute`（`Images` 空・`BulkTagInput` 空それぞれで false になること）、一括追加が全エントリに反映され入力欄がクリアされること、一括削除が大文字小文字無視で全エントリから一致タグを取り除くこと
  - 計172件、全件パス確認済み（`ComfyUICaptioningToolTests.exe` 直接実行で確認。`dotnet test` がテストを検出できない既知の環境依存事象は今回も発生した）
- 実アプリでの目視確認は、この環境で過去のフェーズ（3・4・8・10・13）から繰り返し発生している「座標指定でのクリック操作・スクリーンショットが無関係な別ウィンドウを誤操作/誤取得する」という既知の環境依存の制約により今回も断念し、`GalleryPage.xaml` の既存パターン（`ReportPage.xaml`/`DataPage.xaml` と共通のカードデザイン）を踏襲していることのコードレビューとユニットテストで代替した

### フェーズ15: GalleryPage のタグ編集を captioning_config_result.json へ反映（`feature/gallery-tag-config-result-sync` ブランチ、実装完了）

「GalleryPage でタグ編集した際に、captioning_config_result.json（フェーズ9で `MainPage` 実行成功時に対象ディレクトリ直下へ出力される、今回使用した設定の記録ファイル）にもタグを反映し、次回の一括タグ付け実行（`ConfigPath` として同ファイルを指定するケース）に手動編集内容を引き継ぎたい」というユーザー要望を受けて追加した。タグ追加時は `prepend_tags` に、削除時は `exclude_tags` に反映する（ユーザー指定の対応関係）。

- `Models/GalleryImageEntry.cs` の `AddTag`/`RemoveTag`（フェーズ14で追加済み、同名 `.txt` への即時保存を行うメソッド）に、新設 `UpdateConfigResult(Action<WorkflowConfig> update)` の呼び出しを追加した
  - `AddTag`: `config.PrependTags` にタグを追加（大文字小文字無視で重複排除）。矛盾を避けるため `config.ExcludeTags` から同じタグを削除する
  - `RemoveTag`: `config.ExcludeTags` にタグを追加（同様に重複排除）。`config.PrependTags` から同じタグを削除する
  - `UpdateConfigResult`: 画像と同じディレクトリの `captioning_config_result.json`（`MainPageViewModel.SaveExecutedConfigAsync` と同じファイル名・`System.Text.Json` オプション。`PropertyNameCaseInsensitive` で読み込み、`JsonIgnoreCondition.WhenWritingNull` で null プロパティを出力しない）を読み込み（存在しなければ `new WorkflowConfig()`）、コールバックで書き換えてから保存する。読み込み・保存に失敗した場合は `catch { }` で握りつぶし、タグ本体の `.txt` 保存自体（`SaveTags()`）には影響させない（`MainPageViewModel.SaveResultLogAsync` と同じ「記録ファイルへの反映失敗は主処理に影響させない」方針）
  - `GalleryImageEntry` は `ConfigViewModel`/`MainPageViewModel` と同様、ComfyUI との通信を伴わないファイル I/O のみのため `ICaptioningService` ファクトリー境界は導入していない
- `ComfyUICaptioningToolTests`: `Models/GalleryImageEntryTests.cs` に8件追加（`captioning_config_result.json` 未存在時の新規作成・空文字入力時は作成しないこと・既存ファイルの他フィールド維持と `prepend_tags`/`exclude_tags` への追記・大文字小文字無視の重複排除・追加時に `exclude_tags` から同じタグを取り除くこと・削除時に `prepend_tags` から同じタグを取り除くこと・存在しないタグの削除時はファイルを作成しないこと）。計180件、全件パス確認済み
- 実アプリでの目視確認は、過去のフェーズ（3・4・8・10・13・14）から繰り返し発生している環境依存の制約（座標指定でのクリック操作・スクリーンショットが無関係な別ウィンドウを誤操作/誤取得する）により今回も断念し、ユニットテストとコードレビューで代替した

### フェーズ16: タグ集計レポート生成ロジックの抽出（`feature/extract-tag-report-generator` ブランチ、実装完了）

「`ReportViewModel.GenerateReportAsync` にあるタグ集計レポート生成ロジックを、`GalleryViewModel` でもタグ一覧を扱う際に再利用できるよう別クラスへ分離してほしい」というユーザー要望を受けて、`ICaptioningService.GenerateReportAsync` の呼び出し + `tags_report.txt` の読み込み・行解析（`"タグ名: 出現回数"` → `TagCountEntry`）を `Services/TagReportGenerator.cs`（新設、`static` クラス）へ抽出した。

- `TagReportGenerator.GenerateAsync(ICaptioningService service, string directory, bool recursive)`: `service.GenerateReportAsync` → `tags_report.txt` 読み込み → 正規表現 `^(.*): (\d+)$` での行解析（`ReportViewModel` から移設したものと同一の正規表現、`rating:general` のようにタグ名自体にコロンを含むケースも安全に解析できる）を行い `List<TagCountEntry>` を返す静的メソッド。`ComfyUIException` はそのまま呼び出し元に伝播させる（`ReportViewModel` 側の catch で処理する既存の方針を変えない）
- `ReportViewModel.GenerateReportAsync` は上記メソッドを呼び出すよう書き換え、`ReportEntries` への追加・レポート生成完了メッセージの組み立てのみを担うようにした。挙動・エラーハンドリング・スナックバー表示は変更していない
- **本フェーズでは `GalleryViewModel` 側への組み込みは行っていない**（ユーザーからの依頼は「分離」のみで、`GalleryViewModel` での具体的な利用方法（タグ一覧の表示・既存タグとの重複排除サジェスト等）は未確定のため、対象外とした。`GalleryViewModel` から利用する場合は `ReportViewModel` と同様に `Wd14TaggerRunner`/`ICaptioningService` ファクトリー境界（`Config.Data.ConfigPath` からの読み込み）を別途追加する必要がある）
- `ComfyUICaptioningToolTests`: `Services/TagReportGeneratorTests.cs`（新設、4件。`ICaptioningService` 呼び出し引数の検証・行解析（複数件・コロンを含むタグ名）・例外伝播を検証）。計184件、全件パス確認済み

### フェーズ17: GalleryPage 一括タグ操作の AutoSuggestBox 化・TagList 追加（`feature/gallery-taglist-autosuggest` ブランチ、実装完了）

ユーザーが `GalleryPage.xaml` の一括タグ操作入力欄を `ui:TextBox` から `ui:AutoSuggestBox`（`OriginalItemsSource` に `GalleryViewModel.TagList` をバインドする形）へ変更済みだったのを受けて、フェーズ16で抽出した `TagReportGenerator` を使って `TagList` の中身（対象ディレクトリの tags_report.txt 由来のタグ一覧）を実装し、タグが編集されるたびに更新されるようにした。

- **`GalleryViewModel` に `ICaptioningService`/`Wd14TaggerRunner` 依存を追加**: `TagList` の取得のみに使うため、`ReportViewModel` と同じ形（コンストラクター引数のファクトリー・`INavigationAware.OnNavigatedToAsync` での `Wd14TaggerRunner` 読み込み）を追加した。ただし本ページはこれまでスナックバー（`ISnackbarService`）に依存しない方針（フェーズ13）だったため、`ConfigPath` 未設定・読み込み失敗時もエラー表示は行わず、`TagList` の更新を静かにスキップするだけに留めている（画像・タグ一覧表示という主機能には一切影響させない）
- **`RefreshTagListAsync`（新設 private メソッド）**: `Wd14TaggerRunner` 未読み込み・対象ディレクトリ未設定/未存在の場合は何もしない。それ以外は `TagReportGenerator.GenerateAsync` を呼び出し、返ってきた `TagCountEntry` のタグ名だけを `TagList` へ反映する。例外は握りつぶし、失敗時は `TagList` を直前の内容のまま保持する（`GalleryImageEntry.UpdateConfigResult` と同じ「補助機能の失敗は主機能に影響させない」方針）
- **呼び出しタイミング**: (1) `LoadCommand` 実行後（画像一覧読み込み後に一度呼び出し、初期の候補一覧を構築）。(2) `BulkAddTagCommand`/`BulkRemoveTagCommand`（`private void` → `private async Task` に変更、`BulkAddTagAsync`/`BulkRemoveTagAsync`。`[RelayCommand]` のコマンド名は `Async` サフィックスが自動的に除去されるため `BulkAddTagCommand`/`BulkRemoveTagCommand` のまま維持され、XAML 側の変更は不要）実行後に一度だけ呼び出す。(3) カード単位のタグ編集（`GalleryImageEntry.AddNewTagCommand`/`RemoveTagCommand`）実行後
- **カード単位の編集からの通知経路（`Models/GalleryImageEntry.cs`）**: `GalleryImageEntry` のコンストラクターに `Func<Task>? onTagsChangedAsync = null` を追加し、`GalleryViewModel.CollectEntries` がエントリ生成時に `RefreshTagListAsync` を渡す。`AddTag`/`RemoveTag`（既存の同名 .txt 保存ロジック本体）は `bool`（実際に追加/削除できたか）を返すよう変更した上で従来どおり `public` のまま維持し（`BulkAddTagCommand`/`BulkRemoveTagCommand` が直接呼ぶ経路はコールバックを経由しない設計のため、一括操作では画像 1 枚ごとに `TagList` を再構築しない）、新設した `[RelayCommand] private async Task AddNewTagAsync()`/`RemoveTagAsync(string tag)`（生成されるコマンド名はいずれも `Async` サフィックス除去により `AddNewTagCommand`/`RemoveTagCommand` のまま、XAML 側の変更は不要）が `AddTag`/`RemoveTag` 呼び出し後、実際に変更が起きた場合のみコールバックを await する
- 一括操作でエントリ 1 件ごとにコールバックを発火させず `BulkAddTagCommand`/`BulkRemoveTagCommand` の最後に 1 回だけ `TagList` を更新する設計にしたのは、多数の画像に対する一括編集のたびに対象ディレクトリ全体を毎回スキャンする `TagReportGenerator`（内部で `CaptioningService.GenerateReportAsync` が全 `.txt` を読み直す）が N 回呼ばれる非効率を避けるため
- `ComfyUICaptioningToolTests`:
  - `Models/GalleryImageEntryTests.cs` に4件追加（`AddNewTagCommand`/`RemoveTagCommand` 実行時、実際にタグが追加/削除された場合のみコールバックが呼ばれること・空文字入力や存在しないタグ指定時はコールバックが呼ばれないこと）
  - `ViewModels/Pages/GalleryViewModelTests.cs` に6件追加（初期状態で `TagList` が空・`ConfigPath` 未設定時は `LoadCommand` 実行後も空のまま・有効な `ConfigPath` + tags_report.txt から `TagList` が反映されること・レポート生成失敗時は `TagList` が空のまま影響を受けないこと・`BulkAddTagCommand` 実行後に `TagList` が再構築されること・カード単位の `AddNewTagCommand` 実行後にも `TagList` が再構築されること）
  - 計194件、全件パス確認済み（`ComfyUICaptioningToolTests.exe` 直接実行で確認）
- 実アプリでの目視確認は、過去のフェーズ（3・4・8・10・13・14・15）から繰り返し発生している環境依存の制約（座標指定でのクリック操作・スクリーンショットが無関係な別ウィンドウを誤操作/誤取得する）により今回も断念し、ユニットテストとコードレビューで代替した

### フェーズ18: GalleryPage カード単位のタグ追加に「先頭に追加」ボタンを追加（実装完了）

「GalleryPage の画像カードにあるタグ追加入力欄は末尾に追加するボタンしか無いので、先頭に追加できるボタンも追加してほしい」というユーザー要望を受けて追加した。

- `Models/GalleryImageEntry.cs`: `AddTag(string tag, bool prepend = false)` に `prepend` パラメーターを追加し、`true` の場合は `Tags.Insert(0, trimmed)` で先頭に挿入する（`false`（既定）は従来どおり末尾追加、`BulkAddTagCommand`（`GalleryViewModel`）等の既存呼び出し元は変更不要）。カード上の「タグを追加」入力欄用に、末尾追加の `AddNewTagCommand`（既存）と対になる `[RelayCommand] AddNewTagToStartAsync`（生成コマンド名 `AddNewTagToStartCommand`）を新設した。実装・コールバック呼び出し（`_onTagsChangedAsync` による `GalleryViewModel.TagList` 再構築）のパターンは `AddNewTagAsync` と同一
- `Views/Pages/GalleryPage.xaml`: 各カードのタグ追加入力欄のボタン列を1列（末尾追加のみ）から2列に変更し、先頭追加ボタン（`AddNewTagToStartCommand`、アイコン `ArrowUp24`）を入力欄側（左）・既存の末尾追加ボタン（`AddNewTagCommand`、アイコン `AddCircle24`）を右側に配置した。両ボタンの意味の違いが分かるよう、双方に `ToolTip`（`Gallery_AddTagToStartTooltip`/`Gallery_AddTagToEndTooltip`）を新設して付与した（既存の末尾追加ボタンにはこれまで ToolTip が無かったため今回追加）
- `Resources/Strings.resx`/`Strings.en.resx` に `Gallery_AddTagToStartTooltip`/`Gallery_AddTagToEndTooltip` を追加
- `ComfyUICaptioningToolTests`: `Models/GalleryImageEntryTests.cs` に5件追加（`AddTag(prepend: true)` が先頭挿入・カンマ区切り書き込みを行うことと重複排除の挙動、`AddNewTagToStartCommand` 実行時に入力欄の内容が先頭に追加されクリアされること、実際に追加できた場合のみ TagList 更新コールバックが呼ばれ空文字入力時は呼ばれないこと）。計199件、全件パス確認済み（`ComfyUICaptioningToolTests.exe` 直接実行で確認。`dotnet test` がテストを検出できない既知の環境依存事象は今回も発生した）
- 実アプリでの目視確認は、過去のフェーズから繰り返し発生している環境依存の制約（座標指定でのクリック操作・スクリーンショットが無関係な別ウィンドウを誤操作/誤取得する）により今回も断念し、ユニットテストとコードレビュー（既存の `AddNewTagCommand`/`AddNewTagAsync` と対称な実装であること）で代替した

### フェーズ19: GalleryPage 一括タグ操作にも「先頭に追加」ボタンを追加（実装完了）

フェーズ18でカード単位のタグ追加入力欄に「先頭に追加」ボタンを追加したのに続き、「一括タグ操作の部分にも同様に、先頭にタグを追加するボタンを実装してほしい」というユーザー要望を受けて追加した。着手前に、ユーザーが `GalleryPage.xaml` の一括タグ操作ボタン（`BulkAddTagCommand`/`BulkRemoveTagCommand`）を `Content` 文字列表示から `Icon`（`AddSquare24`/`Delete24`）+ `ToolTip` 表示へ変更済みだったため、これを踏襲する形で実装した。

- `ViewModels/Pages/GalleryViewModel.cs`: 既存の `BulkAddTagAsync`（`BulkTagInput` を全画像の末尾に追加）と対になる `[RelayCommand(CanExecute = nameof(CanBulkEditTag))] BulkAddTagToStartAsync`（生成コマンド名 `BulkAddTagToStartCommand`）を新設。`GalleryImageEntry.AddTag(BulkTagInput, prepend: true)` を全画像に対して呼び出し、完了後に `BulkTagInput` をクリアして `RefreshTagListAsync` を呼ぶ（`BulkAddTagAsync` と同一パターン）。`Images`/`BulkTagInput` の `[NotifyCanExecuteChangedFor]` にも `BulkAddTagToStartCommand` を追加した
- `Views/Pages/GalleryPage.xaml`: 一括タグ操作カードの `Grid.ColumnDefinitions` を3列から4列に拡張し、`AutoSuggestBox` の直後（末尾追加ボタンの左）に先頭追加ボタン（`BulkAddTagToStartCommand`、アイコン `ArrowUp24`、カード単位の先頭追加ボタンと同じアイコン）を配置した
- `Resources/Strings.resx`/`Strings.en.resx`: 新設 `Gallery_BulkAddToStartButtonContent`（「全ての先頭に追加」/"Add to start of all"）を追加。既存 `Gallery_BulkAddButtonContent` は先頭追加ボタンとの区別のため文言を「全てに追加」→「全ての末尾に追加」（"Add to all"→"Add to end of all"）に変更した
- `ComfyUICaptioningToolTests`: `ViewModels/Pages/GalleryViewModelTests.cs` に、既存の `BulkAddTagCommand` 系テスト（`CanExecute`・全画像への反映・入力欄クリア・`TagList` 再構築）と対称な `BulkAddTagToStartCommand` のテストを追加（`CanExecute` の false/true 判定に `BulkAddTagToStartCommand` の検証を追加、`BulkAddTagToStartCommand_Execute_InsertsTagAtStartOfAllImages_AndClearsInput`・`BulkAddTagToStartCommand_Execute_RefreshesTagListFromReportFile` を新規追加）。計201件、全件パス確認済み（`ComfyUICaptioningToolTests.exe` 直接実行で確認）
- 実アプリでの目視確認は、過去のフェーズから繰り返し発生している環境依存の制約（座標指定でのクリック操作・スクリーンショットが無関係な別ウィンドウを誤操作/誤取得する）により今回も断念し、ユニットテストとコードレビュー（既存の `BulkAddTagCommand`/`BulkAddTagAsync` と対称な実装であること）で代替した

### フェーズ20: MainPage タグフィルタへの他 captioning_config.json からのタグインポート機能追加（`feature/import-tags-from-config` ブランチ、実装完了）

「別の captioning_config.json から prepend_tags/exclude_tags をインポートしたい」というユーザー要望を受けて、`MainPage` のタグフィルタ（先頭に追加するタグ/除外するタグ）セクションにインポート機能を追加した。着手前に (1) インポートしたタグを既存の入力欄内容に対してどう反映するか、(2) 「簡易的なバリデーション」の範囲、の2点をユーザーに確認し、(1) 既存の入力欄の内容に追記（マージ、大文字小文字無視で重複排除）する、(2) JSON 構文チェックのみ（`ConfigLoader.ValidateWd14TaggerConfig` のような厳密な必須項目チェックは行わない）、の方針で実装した。

- `ViewModels/Pages/MainPageViewModel.cs`:
  - `[RelayCommand] ImportTagsFromConfig()`（生成コマンド名 `ImportTagsFromConfigCommand`、`BrowseDirectoryCommand` と同じくファイルダイアログを開くだけの薄いラッパー）を新設。`Microsoft.Win32.OpenFileDialog`（JSON フィルタ付き）でインポート元ファイルを選択させ、選択されたら `ImportTagsFromFile(path)` を呼び出す
  - `public void ImportTagsFromFile(string path)`（新設）: 実際のインポート処理本体。ファイルダイアログの操作を伴わずユニットテストできるよう、コマンド本体から分離した公開メソッドとした（`ICaptioningService` ファクトリーのような DI 境界ではなく、`GalleryImageEntry.AddTag` 等と同様のシンプルな公開メソッドによる分離）
    - 読み込み: `File.ReadAllText` → `JsonSerializer.Deserialize<WorkflowConfig>`（`ConfigReadOptions`、`PropertyNameCaseInsensitive`）。JSON 構文が不正な場合（`JsonException`）はエラースナックバーを表示し、入力欄は変更しない（簡易的なバリデーションの範囲。`comfyui_url`/`wd14_tagger` 等の必須項目チェックは行わないため、prepend_tags/exclude_tags のみを持つ最小限の JSON でもインポート可能）
    - 反映: インポートした `config.PrependTags`/`config.ExcludeTags`（キー欠落時は空リスト）を、現在の `PrependTagsText`/`ExcludeTagsText` の末尾に追記する（`MergeTagLists`、大文字小文字無視で重複排除、先に現れた方＝既存入力欄の内容を残す）。成功時は成功スナックバーを表示する
  - 既存の `MergeTags(IReadOnlyList<string> defaults, string extraText)`（既定タグとテキスト入力の union）を、共通ヘルパー `MergeTagLists(IReadOnlyList<string> first, IReadOnlyList<string> second)`（2 つのタグリストを順番通り連結し重複排除するだけの汎用版）に委譲するようリファクタリングした。`ImportTagsFromFile` からも同じ `MergeTagLists` を利用する
- `Views/Pages/MainPage.xaml`: タグフィルタセクションのラベル行（Grid.Row=4）を `TextBlock` 単体から `Grid`（ラベル + インポートボタン）に変更し、右端に `ArrowImport16` アイコンの `ui:Button`（`ImportTagsFromConfigCommand` にバインド、ToolTip 付き）を追加した
- `Resources/Strings.resx`/`Strings.en.resx` に `Main_ImportConfigButtonTooltip`/`Main_ImportConfigDialogTitle`/`Main_ImportConfigSuccessFormat`/`Main_ImportConfigParseErrorFormat`/`Main_ImportConfigReadErrorFormat` を追加
- `ComfyUICaptioningToolTests`: `ViewModels/Pages/MainPageViewModelTests.cs` に `ImportTagsFromFile` のテストを7件追加（空の入力欄への反映・既存入力欄への追記・大文字小文字無視の重複排除・prepend_tags/exclude_tags キー欠落時に入力欄が変化しないこと・成功時の成功スナックバー表示・JSON 構文不正時のエラースナックバー表示と入力欄が変化しないこと）。計220件、全件パス確認済み（`ComfyUICaptioningToolTests.exe` 直接実行で確認）
  - **既知の事象**: 本フェーズの実装・テストとは無関係に、テストスイート全体を実行すると `GalleryViewModelTests`/`DataViewModelTests` 等の英語文言を検証するテストが低頻度で「日本語文言が返る」形で失敗することがある（`LocalizationManager.CurrentCulture` の setter が `CultureInfo.DefaultThreadCurrentUICulture`（プロセス全体の既定値、スレッドプールが新規スレッド生成時にのみ参照）を書き換える一方、既存のカルチャ切替テスト（`LocalizationManagerTests`/`SettingsViewModelTests`）は該当スレッド上でのみ `try/finally` で元に戻しているため、xUnit v3 の並列実行でスレッドプールのスレッドがまたがって再利用されると、別テストの実行中スレッドに一時的な言語設定が残留することがある）。ベースライン（本フェーズの変更前）でも複数回実行のうち発生することを確認しており、本フェーズで新設したテストが原因ではないテスト基盤側の既知の flaky な事象と判断した（本フェーズの対応範囲外）

### フェーズ21: GalleryPage カード単位のタグをクリップボードへコピーするボタンの追加（実装完了）

ユーザーが `GalleryPage.xaml` の各画像カードの「タグを追加」入力欄の左に、`Copy24` アイコンのボタン（コマンド未配線）を追加済みだったのを受けて、押下時にそのカードのタグ一覧をクリップボードへコピーする機能を実装した。

- `Models/GalleryImageEntry.cs`: `[RelayCommand] CopyTagsToClipboard`（生成コマンド名 `CopyTagsToClipboardCommand`）を新設。`Tags` を `SaveTags()`/`AddTag` 系メソッドと同じ `", "` 区切りで連結し `System.Windows.Clipboard.SetText` へ渡す。`Tags.Count == 0` の場合は何もしない。クリップボードアクセスの失敗（他アプリによる一時的なロック等）は `catch { }` で握りつぶし、`UpdateConfigResult` と同じ「補助機能の失敗は主機能に影響させない」方針を踏襲した
- `Views/Pages/GalleryPage.xaml`: 追加済みのコピーボタンに `Command="{Binding CopyTagsToClipboardCommand}"` と `ToolTip`（`Gallery_CopyTagsTooltip`）を配線した
- `Resources/Strings.resx`/`Strings.en.resx` に `Gallery_CopyTagsTooltip`（「タグをコピー」/"Copy tags"）を追加
- `ComfyUICaptioningToolTests`: `Models/GalleryImageEntryTests.cs` に2件追加。`Clipboard` 操作は STA スレッドが必要なため、`MainWindowViewModelTests.RunOnSta` と同じパターンの同期版 `RunOnSta` をテストクラス内に追加し、(1) タグがカンマ区切りでクリップボードへコピーされること、(2) タグ 0 件時はクリップボードの内容が変化しないこと、を検証した。計222件、全件パス確認済み（`ComfyUICaptioningToolTests.exe` 直接実行で確認）
- 実アプリでの目視確認は、過去のフェーズから繰り返し発生している環境依存の制約（座標指定でのクリック操作・スクリーンショットが無関係な別ウィンドウを誤操作/誤取得する）により今回も断念し、ユニットテストとコードレビュー（既存の `RemoveTagCommand`/`AddNewTagCommand` と同様の `[RelayCommand]` 実装パターンであること）で代替した

### フェーズ22: GalleryPage カード単位のタグ一覧をトグルボタン化・選択タグの一括削除ボタンを追加（実装完了）

「タグ名+×削除ボタン」だったカード単位のタグ表示を、タグ名を表記するトグルボタンのみの構成に変更し、選択したタグをまとめて削除できるボタンをタグ追加入力欄に追加してほしいというユーザー要望を受けて実装した。着手前に (1) トグルボタンは複数同時選択を許可するか、(2) 削除後の選択状態をどうするか、の2点をユーザーに確認し、(1) 複数選択可、(2) 削除後は特に選択状態を整理しない（削除されたタグ自体は一覧から消えるため、そのタグのトグルボタンも表示されなくなる）、の方針で実装した。

- `Models/GalleryImageEntry.cs`:
  - `SelectedTags`（`ObservableCollection<string>`、選択中のタグ一覧）・`HasSelectedTags`（`SelectedTags.Count > 0`）を新設。`SelectedTags.CollectionChanged` で `HasSelectedTags` の変更通知に加えて `SelectedTags` 自体の変更通知（`OnPropertyChanged(nameof(SelectedTags))`）も発火する。**設計判断**: `SelectedTags` は get-only の固定インスタンスのため、内容が変わっても既定では XAML 側の `MultiBinding`（各タグボタンの `IsChecked`）が再評価されない（`ObservableCollection` の `CollectionChanged` は `ItemsControl.ItemsSource` のような特別な購読先でしか自動的に反映されず、通常の `Binding`/`MultiBinding` はプロパティ自体の `PropertyChanged` を必要とする）ため、明示的に同名プロパティの変更通知を発火する対応を追加した
  - `[RelayCommand] ToggleTagSelection(string tag)`（生成コマンド名 `ToggleTagSelectionCommand`）: `SelectedTags` に対するタグの追加・削除をトグルする（複数選択可）
  - `[RelayCommand(CanExecute = nameof(HasSelectedTags))] RemoveSelectedTagsAsync`（生成コマンド名 `RemoveSelectedTagsCommand`）: `SelectedTags` の全タグを `RemoveTag` で削除する。1 件以上実際に削除できた場合のみ `_onTagsChangedAsync`（`GalleryViewModel.TagList` 更新用コールバック）を呼び出す。削除後、残った `SelectedTags` の内容は特に整理しない（削除されたタグに対応するトグルボタン自体が一覧から消えるため、視覚的な不整合は生じない）
- `Views/Pages/GalleryPage.xaml`:
  - `Page.Resources` に `TagToggleButtonStyle`（`ToggleButton` 用、チップ風の角丸 `ControlTemplate`。未選択時は既存のタグチップと同じ `ControlFillColorSecondaryBrush`、選択時（`IsChecked=True`）は `AccentFillColorDefaultBrush`/`TextOnAccentFillColorPrimaryBrush` で強調表示する。両ブラシキーは参照 NuGet パッケージ（`wpf-ui` 4.3.0）の `Wpf.Ui.dll` に実在することを事前に確認済み）を新設
  - タグ一覧の `ItemsControl.ItemTemplate` を、`TextBlock`（タグ名）+ `×` ボタン（削除）の `StackPanel` から、`TagToggleButtonStyle` を適用した単一の `ToggleButton`（`Content` にタグ名を表示）に置き換えた。`Command="{Binding DataContext.ToggleTagSelectionCommand, RelativeSource={RelativeSource AncestorType=ItemsControl}}"`・`CommandParameter="{Binding}"`（既存の `RemoveTagCommand` 配線と同じ `RelativeSource` パターン）。`IsChecked` は新設の `Helpers/TagInCollectionConverter.cs`（後述）を使った `MultiBinding`（`Mode="OneWay"`、タグ文字列 + `SelectedTags`）で「このタグが `SelectedTags` に含まれるか」を判定する
  - タグ追加入力欄の `Grid`（カード下部、コピー/先頭追加/末尾追加ボタンの並び）に 5 列目を追加し、`RemoveSelectedTagsCommand` にバインドした削除ボタン（アイコン `Delete24`、`ToolTip` は既存の `Gallery_RemoveTagTooltip`（「タグを削除」）を再利用。`IsEnabled` は `[RelayCommand(CanExecute=...)]` が自動生成する `ICommand.CanExecuteChanged` にボタンが標準で追従するため、明示的なバインディングは不要）を配置した
- `Helpers/TagInCollectionConverter.cs`（新設）: `IMultiValueConverter`。タグ文字列がコレクションに含まれるかどうかだけを判定する（大文字小文字無視）。**実装時の不具合と修正**: 当初は既存の `TagExistsToBoolean`（`AddNewTagToStartCommand`/`AddNewTagCommand` の `IsEnabled` 判定で使用）を流用していたが、このコンバーターは対象リストが空の場合に `isInversion` を反転した値を返す仕様（「タグ未入力時はボタンを活性化する」用途向けの既存挙動）のため、`SelectedTags` が空（初期状態＝全て未選択であるべき）でも `IsChecked=true` になってしまう不具合が発生した。ユーザー報告を受けて、単純な「リストに含まれるか」のみを判定する本コンバーターに差し替えて解決した
- `Resources/Strings.resx`/`Strings.en.resx` の変更なし（`Gallery_RemoveTagTooltip` を流用したため新規キー追加は不要）
- `ComfyUICaptioningToolTests`: `Models/GalleryImageEntryTests.cs` に8件追加（`ToggleTagSelectionCommand` による選択/解除・複数タグの同時選択、`RemoveSelectedTagsCommand` の `CanExecute`（選択なし/ありでの false/true）・選択中の全タグ削除と同名 `.txt` への反映・削除成功時のコールバック呼び出し・選択なし時は `CanExecute=false` のままタグが変化しないこと）。`Helpers/TagInCollectionConverterTests.cs`（新設、9件。values null/要素数不正・tagList 空/null・tag 空文字・含まれる/含まれない・大文字小文字無視・ConvertBack 未実装を検証。特に「tagList が空の場合は tag の内容に関わらず false」を明示的に検証し、上記不具合の再発防止としている）。計239件、全件パス確認済み（`ComfyUICaptioningToolTests.exe` 直接実行で確認。実行毎に低頻度で `DataViewModelTests` 等の英語文言検証テストが失敗することがあるが、フェーズ20で記録済みの `LocalizationManager` の既知の flaky な事象であり、再実行で解消することを確認済み。本フェーズの変更とは無関係）
- 実アプリでの目視確認は、過去のフェーズから繰り返し発生している環境依存の制約（座標指定でのクリック操作・スクリーンショットが無関係な別ウィンドウを誤操作/誤取得する）により今回も断念し、ユニットテストとコードレビュー（`ToggleTagSelectionCommand`/`RemoveSelectedTagsCommand` は既存の `RemoveTagCommand`/`AddNewTagCommand` と同様の `[RelayCommand]` 実装パターンであること、`TagToggleButtonStyle` が使用するブラシキーが参照パッケージに実在すること）で代替した

### フェーズ23: GalleryPage を画像タイル一覧＋選択画像のタグ編集パネルの2ペイン構成に変更（`feature/gallery-two-pane-layout` ブランチ、実装完了）

「画像・タグ一覧・タグ編集がすべて1枚のカードにまとまっている現状の一覧表示を、画像タイル一覧と選択画像のタグ編集パネルに分けたい」というユーザー要望を受けて、`GalleryPage` のカード一覧を全面的に構成し直した。

- **画像カードの簡素化**: これまで「サムネイル＋ファイル名＋タグ一覧＋タグ編集入力欄」を1枚に詰め込んでいたカードを、サムネイル＋ファイル名のみのタイルに簡素化した
- **左右2ペイン構成**: `GalleryPage.xaml` の画像・タグ一覧表示領域（旧 `Grid.Row="5"` の `ScrollViewer`）を、左ペイン（画像＋ファイル名のタイル一覧、`WrapPanel` で折り返し表示）と右ペイン（選択中の画像のタグ一覧・編集 UI、固定幅 `360`）の2列 `Grid` に変更した
  - **左ペインの選択実装**: 当初 `ListBox`（`SelectedItem` を双方向バインド）で実装したが、`ListBox` は既定のテンプレートが内部 `ScrollViewer`（`HorizontalScrollBarVisibility="Auto"`）を持つため `WrapPanel` に無限の水平幅が与えられてしまい、タイルが折り返さず水平1列に並んでしまう不具合が判明した。`ItemsControl`（`ItemsPanel` に `WrapPanel`）へ差し替え、選択状態は各タイルを `ToggleButton`（新設 `ImageTileToggleButtonStyle`、選択中は `IsChecked=True` トリガーでボーダー色をアクセントカラー・太さ2pxに変更）にして実現し直した。`IsChecked` は新設 `Helpers/ObjectEqualsConverter.cs`（`IMultiValueConverter`、2値の `Equals` 判定のみを行う汎用コンバーター）を使った `MultiBinding`（`Mode="OneWay"`、タイル自身 + `GalleryViewModel.SelectedImage`）でバインドし、`Command`（新設 `GalleryViewModel.SelectImageCommand`、`CommandParameter` にタイル自身を渡し `SelectedImage` へ代入するだけ）でクリック時に選択を切り替える。この構成はタグ選択トグルボタン（`TagInCollectionConverter`/`ToggleTagSelectionCommand`、フェーズ22）と同じパターンである
  - 右ペインは `Border` カードの中に `DataContext="{Binding ViewModel.SelectedImage}"` を設定し、旧カードのタグ一覧（トグルボタン、フェーズ22実装分をそのまま移設）・タグ追加入力欄（コピー/先頭に追加/末尾に追加/選択タグを削除の4ボタン、フェーズ14〜22実装分をそのまま移設）を配置した。`SelectedImage` が `null`（未選択）の場合は新設 `Gallery_SelectImagePrompt`（「画像を選択するとタグが表示されます」）のプレースホルダーメッセージを表示し、選択済みの場合は詳細 `Grid` を表示する（`DataTrigger Binding="{Binding}" Value="{x:Null}"` によるトグル、`GalleryImageEntry.Thumbnail`/`HasTags` の null/false 判定と同じ既存パターン）
- **`ViewModels/Pages/GalleryViewModel.cs`**: `SelectedImage`（`[ObservableProperty]`）・`SelectImageCommand`（`[RelayCommand]`、`SelectedImage = entry` を代入するだけ）を新設。`LoadAsync` 冒頭（`Images` クリア時）で `SelectedImage = null` にリセットする（再読み込み後は旧 `Images` のインスタンスが失われるため、選択状態を持ち越さない）。一括タグ操作（`BulkAddTagAsync`/`BulkAddTagToStartAsync`/`BulkRemoveTagAsync`）・カード単位のタグ編集は引き続き同じ `GalleryImageEntry` インスタンスを直接書き換えるため、`SelectedImage` が指すエントリの `Tags`/`HasTags` は右ペインへ自動的に反映される（追加のイベント配線は不要）
- `Models/GalleryImageEntry.cs`・`Services/TagReportGenerator.cs` 等の変更は行っていない（純粋に `GalleryPage.xaml` の表示構成と `GalleryViewModel` の選択状態管理のみの変更）
- `Resources/Strings.resx`/`Strings.en.resx` に `Gallery_SelectImagePrompt` を追加
- `ComfyUICaptioningToolTests`: `ViewModels/Pages/GalleryViewModelTests.cs` に、初期状態で `SelectedImage` が `null` であることの検証（既存の `Constructor_InitialState_IsEmpty` に追加）・`LoadCommand` の再実行で `SelectedImage` が `null` にリセットされることの検証（`LoadCommand_Execute_Reload_ResetsSelectedImageToNull`）・`SelectImageCommand` 実行で `SelectedImage` が更新されることの検証（`SelectImageCommand_Execute_SetsSelectedImage`）を追加。`Helpers/ObjectEqualsConverterTests.cs`（新設、7件。values null/要素数不正・同一インスタンス/異なるインスタンス・両方 null/片方のみ null・ConvertBack 未実装を検証）。計248件、全件パス確認済み（`ComfyUICaptioningToolTests.exe` 直接実行で確認）
- 実アプリでの目視確認は、過去のフェーズから繰り返し発生している環境依存の制約（座標指定でのクリック操作・スクリーンショットが無関係な別ウィンドウを誤操作/誤取得する）により今回も断念し、ユニットテストとコードレビュー（既存のタグ一覧・タグ編集 UI 部分は変更前と同一の XAML 断片をそのまま移設していること、タイル選択の実装パターンがタグ選択トグルボタンと対称であること）で代替した

### フェーズ24: ReportPage へのタグ名インタラクティブフィルタリング機能の追加（`feature/report-page-tag-filter` ブランチ、実装完了）

「`ReportPage` のタグ集計レポート一覧を、`ui:AutoSuggestBox` へのテキスト入力でインタラクティブに絞り込みたい」というユーザー要望を受けて追加した。着手前に (1) マッチ方式（部分一致・大文字小文字区別なし／前方一致・大文字小文字区別なし）、(2) AutoSuggestBox のサジェスト候補（生成済みレポートのタグ一覧を候補表示／サジェストなしの単純入力欄）の2点を確認し、いずれも推奨側（部分一致・大文字小文字区別なし、生成済みタグ一覧を候補表示）を採用した。

- `ViewModels/Pages/ReportViewModel.cs`:
  - `GenerateReportAsync` が生成した全件を保持する `private List<TagCountEntry> _allReportEntries`（フィルタ適用前）と、`ui:AutoSuggestBox` の `OriginalItemsSource` にバインドする `public ObservableCollection<string> TagList`（生成済みレポートのタグ名一覧）を新設。既存の `ReportEntries`（`ListView` 表示用）は「`_allReportEntries` のうち `FilterText` によるフィルタ適用後」の内容を保持する役割に変更した
  - `FilterText`（`[ObservableProperty]`）を新設し、`partial void OnFilterTextChanged` で `ApplyFilter()`（`_allReportEntries` から `Tag.Contains(FilterText, StringComparison.OrdinalIgnoreCase)` で絞り込み `ReportEntries` へ反映）を呼び出す。入力のたびに即座に絞り込まれる（`UpdateSourceTrigger=PropertyChanged` で XAML 側からバインド）
  - `GenerateReportAsync` は新しいレポート生成のたびに `_allReportEntries`/`TagList`/`FilterText` をリセットしてから生成結果を反映する（前回のフィルタ条件を持ち越さない）。`ReportStatusText` の「{0} 件のタグを集計しました」は `FilterText` リセット直後に `ApplyFilter()`（全件が反映された状態）を呼んでから `ReportEntries.Count` を参照するため、フィルタ適用後の件数ではなく常に全件数を表示する
- `Views/Pages/ReportPage.xaml`: レポート生成ステータス表示（`Grid.Row="3"`）とタグ/出現回数の列見出し（旧 `Grid.Row="4"`）の間に `ui:AutoSuggestBox`（新設 `Grid.Row="4"`、以降の行は1つずつ繰り下げ）を追加。`Text` を `ViewModel.FilterText` に `UpdateSourceTrigger=PropertyChanged` で双方向バインドし、`OriginalItemsSource` を `ViewModel.TagList` にバインドしてサジェスト候補として使う（`GalleryPage.xaml` の一括タグ操作入力欄と同じ `ui:AutoSuggestBox` 構成）
- `Resources/Strings.resx`/`Strings.en.resx` に `Report_FilterPlaceholder`（「タグ名でフィルタ」/"Filter by tag name"）を追加
- `ComfyUICaptioningToolTests`: `ViewModels/Pages/ReportViewModelTests.cs` に5件追加（`GenerateReportCommand` 実行後に `TagList` がタグ名一覧で反映されること、`FilterText` の部分一致・大文字小文字無視でのフィルタリング、`FilterText` を空文字に戻すと全件表示に戻ること、一致なし時は `ReportEntries` が空になること、`GenerateReportCommand` 再実行時に前回の `FilterText` がリセットされること）。計253件、全件パス確認済み（`ComfyUICaptioningToolTests.exe` 直接実行で確認）
- 実アプリでの目視確認は、過去のフェーズから繰り返し発生している環境依存の制約（座標指定でのクリック操作・スクリーンショットが無関係な別ウィンドウを誤操作/誤取得する）により今回も断念し、ユニットテストとコードレビュー（`GalleryPage.xaml` の `ui:AutoSuggestBox` 構成と同一パターンであること）で代替した

### フェーズ25: publish 設定の整備（`feature/publish-settings` ブランチ、実装完了）

「アプリ配布のための準備をしたい」というユーザー要望を受けて、`ComfyUIRunWorkflow` と比較し `publish`（`dotnet publish`）のために不足していた `csproj` のプロパティと発行プロファイルを整備した。`templates/` フォルダ・`captioning_config.json` の `Content` 配置・アイコン設定・`ComfyUILibs` のパッケージバージョン・`app.manifest` は既に `ComfyUIRunWorkflow` と同等の状態だったため対象外。

- `ComfyUICaptioningTool.csproj` に `Version`（`1.0.0`。初回リリースとして設定、以降のバージョン管理はリリースのたびに更新する想定）・`RuntimeIdentifier`（`win-x64`）・`PublishSingleFile`（`true`）・`EnableCompressionInSingleFile`（`true`）を追加した。`SelfContained` は明示していない（`ComfyUIRunWorkflow` と同様、既定の `false`＝フレームワーク依存の単一 exe になる。実行環境に .NET 8 Desktop Runtime のインストールが前提）
- `Properties/PublishProfiles/FolderProfile.pubxml`（新設）: `ComfyUIRunWorkflow` と同一内容（`Configuration=Release`・`PublishDir=bin\Release\net8.0-windows7.0\publish\`・`PublishProtocol=FileSystem`）。Visual Studio の発行ウィザードや `dotnet publish -p:PublishProfile=FolderProfile` から参照できる
- `dotnet publish ComfyUICaptioningTool/ComfyUICaptioningTool.csproj -c Release` を実行し、`bin/Release/net8.0-windows7.0/win-x64/publish/` に単一 exe（`ComfyUICaptioningTool.exe`）・必要なネイティブ DLL（`D3DCompiler_47_cor3.dll` 等）・`templates/`・`captioning_config.json` が正しく出力されることを確認済み
- 本フェーズはビルド設定のみの変更でありクラスの追加・変更を伴わないため、ユニットテストの追加・実行は対象外とした（`dotnet build ComfyUICaptioningTool.sln` の成功のみ確認）
- **対象外とした点**: `ComfyUIRunWorkflow` 側の publish 出力には `.pdb`（デバッグシンボル）が含まれていなかったが、`csproj` に `DebugType` 等の明示的な設定は無く発行時のオプション差と推測されるため、本フェーズでは追随しなかった（配布時に気になる場合は `dotnet publish` に `-p:DebugType=none` を付与するか、`csproj` に追加で設定する）

### フェーズ26: GalleryPage 右ペインの選択タグ並び替え機能の追加（`feature/gallery-tag-reorder` ブランチ、実装完了）

ユーザーが `GalleryPage.xaml` 右ペインのタグ追加入力欄に、コマンド未配線の4ボタン（`ArrowUpload24`/`ArrowUp24`/`ArrowDown24`/`ArrowDownload24`）を追加済みだったのを受けて、選択中のタグ（フェーズ22の `SelectedTags`、トグルボタンで複数選択可）の順序を入れ替える機能を実装した。

- `Models/GalleryImageEntry.cs` に4つの `[RelayCommand(CanExecute = nameof(HasSelectedTags))]` メソッドを追加。いずれも処理後に `SaveTags()`（同名 .txt への即時保存）を呼ぶ。順序変更のみでタグの追加・削除を伴わないため、`captioning_config_result.json`（`UpdateConfigResult`）・TagList 更新コールバック（`_onTagsChangedAsync`）はいずれも呼び出さない
  - `MoveSelectedTagsToStartCommand`/`MoveSelectedTagsToEndCommand`: `Tags` を現在の並び順のまま選択タグのみ抽出（相対順序を保つ）→ 一旦すべて `Remove` →先頭/末尾へ再 `Insert`/`Add` する、というシンプルな実装
  - `MoveSelectedTagsUpCommand`/`MoveSelectedTagsDownCommand`: `Tags` を先頭（末尾）から順に走査し、「自身が選択中 かつ 直前（直後）の要素が非選択」の場合のみ `ObservableCollection<T>.Move` で1つ前（後ろ）へ移動する。選択済みタグの判定は `SelectedTags`（文字列コレクション）への `Contains` を都度その場（ライブな現在位置）で行うため、複数の選択タグが連続している場合でもブロックとして一体で1つ前/後ろへ移動する（走査開始前にインデックスをスナップショットする実装だと、ブロック内の2番目以降のタグが「直前は元々選択されていた」と誤判定されて移動できなくなる不具合があったため、値ベースの判定に変更した）
  - `Views/Pages/GalleryPage.xaml`: 4ボタンに `Command`（`MoveSelectedTagsToStartCommand`/`MoveSelectedTagsUpCommand`/`MoveSelectedTagsDownCommand`/`MoveSelectedTagsToEndCommand`）と `ToolTip`（新設 `Gallery_MoveTagsToStartTooltip`/`Gallery_MoveTagsUpTooltip`/`Gallery_MoveTagsDownTooltip`/`Gallery_MoveTagsToEndTooltip`）を配線した
- `Resources/Strings.resx`/`Strings.en.resx` に上記4つの ToolTip キーを追加
- `ComfyUICaptioningToolTests`: `Models/GalleryImageEntryTests.cs` に11件追加（`MoveSelectedTagsToStartCommand`/`MoveSelectedTagsToEndCommand` の `CanExecute`・相対順序を保った移動と .txt への反映、`MoveSelectedTagsUpCommand`/`MoveSelectedTagsDownCommand` の単一選択時の隣接要素とのスワップ・境界（先頭/末尾）到達時は変化しないこと・連続選択タグがブロックとして一体で移動することを検証）。計264件、全件パス確認済み（`ComfyUICaptioningToolTests.exe` 直接実行で確認）
- 実アプリでの目視確認は、過去のフェーズから繰り返し発生している環境依存の制約（座標指定でのクリック操作・スクリーンショットが無関係な別ウィンドウを誤操作/誤取得する）により今回も断念し、ユニットテストとコードレビュー（`RemoveSelectedTagsCommand` と同様の `[RelayCommand(CanExecute = nameof(HasSelectedTags))]` 実装パターンであること）で代替した

### フェーズ27: GalleryPage のタグ操作作業ログ（gallery_edit_log.jsonl）出力（`feature/gallery-edit-log` ブランチ、実装完了）

「`GalleryPage` でタグの操作（追加・削除・順序変更）が行われた時に、作業ログを残したい」というユーザー要望を受けて追加した。着手前に (1) 保存先（画像と同じディレクトリに1ファイル／`AppConfig.ResultsFolder` 配下に集約）、(2) 記録形式（JSON Lines・追記型／単一 JSON 配列を都度読み書き）、(3) 対象操作の範囲（カード単位・一括操作・並び替えすべて／カード単位の追加・削除・並び替えのみ）の3点を確認し、いずれも推奨側（画像と同じディレクトリに1ファイル、JSON Lines 追記型、一括操作を含むすべての操作が対象）を採用した。

- `Models/GalleryEditLogEntry.cs`（新設）: 作業ログ1件分を表す positional record（`Timestamp`/`FileName`/`Operation`/`Tags`、JSON プロパティ名は snake_case）。`Operation` は `"add_start"`（先頭に追加）/ `"add_end"`（末尾に追加）/ `"remove"`（削除）/ `"reorder_to_start"`/`"reorder_to_end"`/`"reorder_up"`/`"reorder_down"`（並び替え）のいずれか
- `Models/GalleryImageEntry.cs`: 新設 private メソッド `LogEdit(string operation, IReadOnlyList<string> tags)` が、画像と同じディレクトリの `gallery_edit_log.jsonl` へ `GalleryEditLogEntry` を 1 行（コンパクトな JSON、`WriteIndented` なし）追記する。`UpdateConfigResult` と同じく書き込み失敗（ディレクトリ取得失敗・ファイル I/O 例外）は握りつぶし、タグ編集本体（`.txt` 保存・`captioning_config_result.json` 反映）には影響させない
  - `AddTag`: 実際に追加できた場合（末尾に `return true` する直前）に `LogEdit(prepend ? "add_start" : "add_end", new[] { trimmed })` を呼ぶ。これにより **カード単位の追加（`AddNewTagCommand`/`AddNewTagToStartCommand`）・一括追加（`GalleryViewModel.BulkAddTagCommand`/`BulkAddTagToStartCommand`）のいずれも自動的にログ対象になる**（一括操作は画像ごとに `AddTag` を直接呼ぶ実装のため、`GalleryViewModel` 側の変更は不要だった）
  - `RemoveTag`: 実際に削除できた場合に `LogEdit("remove", new[] { tag })` を呼ぶ。同様にカード単位の削除（`RemoveTagCommand`/`RemoveSelectedTagsCommand`）・一括削除（`GalleryViewModel.BulkRemoveTagCommand`）のいずれも自動的にログ対象になる
  - `MoveSelectedTagsToStart`/`MoveSelectedTagsToEnd`/`MoveSelectedTagsUp`/`MoveSelectedTagsDown`（フェーズ26で追加済みの並び替え4コマンド）: いずれも移動処理前に `Tags` のスナップショット（`before`）を取り、`SaveTags()` 呼び出し後に `before` と現在の `Tags` を比較して**実際に順序が変化した場合のみ** `LogEdit` を呼ぶ（既に先頭/末尾にある選択タグを移動しようとした場合など、実質的な変化がない操作はログに残さない）。`ToStart`/`ToEnd` はすでに計算済みの `selected`（移動対象タグ、相対順序を保持）をそのまま `Tags` として渡し、`Up`/`Down` は移動後の `Tags` から `SelectedTags` に含まれる要素を抽出したもの（`Tags` 内での現在の並び順）を渡す
- クラス先頭の XML ドキュメントコメントに、作業ログ出力についての説明を追記した
- `ComfyUICaptioningToolTests`: `Models/GalleryImageEntryTests.cs` に15件追加（`gallery_edit_log.jsonl` 読み込みヘルパー `ReadEditLog` を新設。`AddTag` の末尾追加/先頭追加それぞれで `add_end`/`add_start` エントリが記録されること・空文字/重複タグでは記録されないこと、`RemoveTag` の記録・存在しないタグでは記録されないこと、複数操作が順番通り追記されること、`MoveSelectedTagsToStart`/`ToEnd`/`Up`/`Down` それぞれの `reorder_*` エントリの記録と、既に境界にあり実質変化がない場合は記録されないことを検証）。計284件、うち282件パス確認済み（`ComfyUICaptioningToolTests.exe` 直接実行で確認。このマシンでは `CopyTagsToClipboardCommand` 関連の2件がクリップボードアクセス不可（`OpenClipboard` 失敗、`0x800401D0`）により失敗するが、変更前の `master` ブランチでも同一の失敗が再現することを確認済みのため、本フェーズの変更とは無関係な環境依存の既存事象と判断した）
- 実アプリでの目視確認は、過去のフェーズから繰り返し発生している環境依存の制約（座標指定でのクリック操作・スクリーンショットが無関係な別ウィンドウを誤操作/誤取得する）により今回も断念し、ユニットテストとコードレビュー（`UpdateConfigResult` と同様の「握りつぶし・主処理に影響させない」実装パターンであること）で代替した

### 将来的な拡張

- `doc/` ディレクトリ（使い方ドキュメント・クラス図）の整備
