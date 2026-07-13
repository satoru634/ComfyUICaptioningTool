# 実装状況

## 現在の状態（2026-07-11 時点）

フェーズ1（`ComfyUILibs` への `CaptioningService` 新設）・フェーズ2（`MainPage` のディレクトリ一括タグ付け実行ページへの置換）・フェーズ3（`SettingsPage` へのデフォルト prepend/exclude タグ追加、フェーズ6で廃止）・フェーズ4（`DataPage` の実行結果・タグ集計レポート表示ページへの置換）・フェーズ6（既定 prepend/exclude タグの保持先を `captioning_config.json` に一本化）・フェーズ8（`ConfigPage` による captioning_config.json 直接編集）・フェーズ9（`MainPage` 実行成功時の実行結果設定 JSON `captioning_config_result.json` 出力）が実装完了。テンプレート由来のサンプル実装は残っていない。

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

### 将来的な拡張

- `doc/` ディレクトリ（使い方ドキュメント・クラス図）の整備
