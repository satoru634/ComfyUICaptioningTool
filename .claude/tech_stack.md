# 技術スタック

## ComfyUILibs

別リポジトリ（サブモジュール）のため、技術スタックは `ComfyUILibs/.claude/tech_stack.md` を参照。
ただし本プロジェクトのビルドで実際に参照されるのは `ComfyUICaptioningTool/ComfyUILibs/` 側（詳細は CLAUDE.md の「ComfyUILibs の参照経路に注意」を参照）。

## ComfyUICaptioningTool

- .NET 8 / WPF（`net8.0-windows7.0`）
- [Wpf.Ui](https://github.com/lepoco/wpfui)（WPF-UI）v4.3.0 — UI フレームワーク
- WPF-UI.DependencyInjection v4.3.0 — DI コンテナ連携（`AddNavigationViewPageProvider` 等）
- CommunityToolkit.Mvvm v8.4.2 — MVVM（`[ObservableProperty]`/`[RelayCommand]`）
- Microsoft.Extensions.Hosting v10.0.1 — DI・ライフサイクル管理（.NET Generic Host）
- Microsoft.Xaml.Behaviors.Wpf v1.1.142
- System.Text.Json v10.0.9

## アーキテクチャ

### ComfyUILibs の参照経路に注意

- 本リポジトリは `ComfyUILibs` を Git submodule として `ComfyUICaptioningTool/ComfyUILibs/` 配下に持つ（`.gitmodules` 参照）。
- `ComfyUILibs` 自体の開発ルール・技術スタック・クラス図は本 CLAUDE.md ではなく `ComfyUILibs/CLAUDE.md` および `ComfyUILibs/.claude/` 配下（`comfyuilibs_common.md` / `directory_structure.md` / `implementation_status.md` / `tech_stack.md`）に従う。

### wdv3-timm サブモジュール

- 本リポジトリは `wdv3-timm`（[satoru634/wdv3-timm](https://github.com/satoru634/wdv3-timm)、`timm` ライブラリで WD Tagger V3 を実行する単一スクリプトのリポジトリ）を Git submodule としてルート直下 `wdv3-timm/` に持つ（`.gitmodules` 参照）。ComfyUILibs の `WdV3TimmTaggerRunner` が ComfyUI を経由せずローカルでタグ付けを行う際に、この `wdv3-timm/wdv3_timm.exe`（`--serve` 常駐サーバーモード）をサブプロセスとして起動する想定
- C# プロジェクトからの `ProjectReference` は無い（Python スクリプトのため）。`WdV3TimmConfig.ExePath`（`captioning_config.json`）に実行ファイルパスを設定して利用する
- `wdv3-timm` 自体の開発ルール・使い方・アーキテクチャは `wdv3-timm/CLAUDE.md` および `wdv3-timm/.claude/`（`setup.md` / `usage.md` / `architecture.md`）に従う
- 本フェーズ（サブモジュール追加）の時点では GUI 側の配線（`ConfigPage` でのバックエンド選択・`wdv3_timm` セクション編集等）は未実装。GUI 配線は別タスクで行う想定

### 責務の分離

| プロジェクト | 責務 |
|---|---|
| `ComfyUILibs`（submodule、別リポジトリ） | ComfyUI API 通信・ワークフロー制御・設定管理などのビジネスロジック全般。UI・プレゼンテーション層のコードは含まない |
| `ComfyUICaptioningTool`（本プロジェクト） | GUI のみ。View・ViewModel・UI ヘルパーに限定し、ComfyUI API を直接呼び出さず `ComfyUILibs` の `Services/` を DI 経由で利用する |

ディレクトリ一括処理・タグフィルタ（prepend/exclude）・タグ集計レポートといったロジックは、UI に依存しないビジネスロジックのため `ComfyUILibs`（`Services/CaptioningService.cs`）側に配置した（フェーズ1で実装完了、詳細は `.claude/implementation_status.md` を参照）。`CaptioningService` は設定ファイルを自前で読み込まず、`Wd14TaggerRunner` と prepend/exclude タグを呼び出し側（GUI）から受け取る設計のため、`MainPageViewModel`（フェーズ2）が `AppConfig.ConfigPath` から `Wd14TaggerRunner` を構築し、`Services/ICaptioningService.cs`/`CaptioningServiceAdapter.cs`（本プロジェクト側の薄いラッパー、テスト容易性のための境界）経由で呼び出す配線を行った。

### comfyui_url・WD14 設定の扱い

`AppConfig.ConfigPath` は外部 `captioning_config.json` ファイルへのパスのみを保持し（`SettingsPage` のファイル選択ダイアログでパスを指定）、`Wd14TaggerRunner`/`ComfyUILibs.Services.ConfigLoader` がそのファイルを読み込む、という構造自体は `ComfyUIRunWorkflow` と同じ方式を踏襲している。`comfyui_url`・`wd14_tagger`（model_name・threshold）・`prepend_tags`/`exclude_tags`（既定 prepend/exclude タグ）は当初（フェーズ2/3）GUI 内での直接編集を避ける方針だったが、フェーズ8で `ConfigPage`（`ViewModels/Pages/ConfigViewModel.cs`）を新設し、`ConfigPath` が指すファイルの中身を GUI から直接編集・保存できるようにした（詳細は `.claude/implementation_status.md` のフェーズ8を参照）。`prepend_tags`/`exclude_tags` は `WorkflowConfig`（`ComfyUILibs.Models`）のプロパティとして config ファイル側で保持し、`Wd14TaggerRunner.PrependTags`/`ExcludeTags`（`IReadOnlyList<string>`、キー欠落時は空リスト）経由で参照する（`AppConfig` 側には持たない）。`MainPageViewModel` がこれらと `MainPage` 実行時の入力値を union（既定値を先頭、大文字小文字無視で重複排除）してから `CaptioningService` に渡す（`MainPageViewModel.MergeTags` を参照）。一方 `ConfigViewModel` は既定値そのものを書き込む唯一の場所のため union 処理は行わない。

### 起動・DI 構成（`App.xaml.cs`）

- .NET Generic Host（`Host.CreateDefaultBuilder()`）で DI コンテナを構築し、`IHostedService` 実装の `ApplicationHostService` がメインウィンドウ表示を担当する。
- アプリ設定は `ComfyUILibs.Common.Setting<AppConfig>` を DI にシングルトン登録し、実行ディレクトリ直下の `ComfyUICaptioningTool_setting.json` に永続化する（`Models/AppConfig.cs`）。
- WPF-UI が提供する `IThemeService` / `ITaskBarService` / `ISnackbarService` / `INavigationService` をシングルトン登録し、`MainWindow` が `INavigationWindow` を実装してナビゲーションホストとなる。
- 各ページ（`MainPage` / `DataPage` / `ConfigPage` / `SettingsPage`）とその ViewModel はいずれもシングルトン登録。ページ側で状態を持たせる場合は `INavigationAware`（`OnNavigatedToAsync` / `OnNavigatedFromAsync`）で初期化・保存タイミングを制御する（`SettingsViewModel` が実装例）。

### ViewModel の実装パターン

- MVVM は `CommunityToolkit.Mvvm`（`ObservableObject` / `[ObservableProperty]` / `[RelayCommand]`）。
- `Setting<AppConfig>` はコンストラクター経由で各 ViewModel に注入し、`Config.Data` を直接読み書きする（`MainPageViewModel` / `SettingsViewModel` を参照）。
- 設定値の変更を即時反映したい場合は `partial void On<プロパティ名>Changed(...)` で購読し、`Config.Data` への書き戻しと副作用（テーマ適用など）を行う（`SettingsViewModel.OnSelectedThemeChanged` / `OnSelectedLanguageChanged` が実装例）。
- 多言語化は `ComfyUIRunWorkflow` 側の仕組み（`Helpers/LocalizationManager.cs` + `Resources/Strings.resx` / `Strings.en.resx`）をそのまま移植済み。
  - `LocalizationManager.Instance` はシングルトンで、`CurrentCulture`（`ja` / `en`）の変更時に `PropertyChanged("Item[]")` を発火する。
  - XAML からは `{Binding Source={x:Static helpers:LocalizationManager.Instance}, Path=[キー]}` のインデクサーバインディングで文言を取得する（`SettingsPage.xaml` が実装例）。
  - `ObservableCollection` で構築するナビゲーションメニュー項目（`MainWindowViewModel.MenuItems` 等）は文言バインディングができないため、`LocalizationManager.Instance.PropertyChanged` を購読して `BuildMenuItems()` で再構築する方式を取る（STA スレッドでのみ再構築するガード付き）。
  - 起動時は `ApplicationHostService.StartAsync` が `Config.Data.Language`（既定 `"ja"`）を `LocalizationManager.Instance.CurrentCulture` に適用する。OS ロケールに関わらず既定値に固定するための明示的な処理。
  - `SettingsViewModel.OnSelectedLanguageChanged` で言語切替時に `Config.Data.Language` への保存と `LocalizationManager.Instance.CurrentCulture` の即時反映（再起動不要）を行う。
  - `Resources/Translations.cs` はテンプレート由来の未使用スタブ（`ComfyUIRunWorkflow` 側にも同名の未使用ファイルが残っている）。
- `MainPageViewModel` は `INavigationAware.OnNavigatedToAsync` のたびに `AppConfig.ConfigPath` から `Wd14TaggerRunner` を再読み込みする（`ComfyUIRunWorkflow` の `TaggerViewModel.TryLoadRunner` と同じパターン）。読み込み失敗時は `ISnackbarService` で Danger 表示し、実行コマンドの `CanExecute` を false にする。
- ネットワーク通信を伴うサービス（`CaptioningService` 等）をテストから差し替えたい場合は、本プロジェクト側に薄いインターフェース＋アダプター（例: `Services/ICaptioningService.cs` / `CaptioningServiceAdapter.cs`）を新設し、ViewModel のコンストラクターにファクトリー（`Func<...>`、既定値あり）として注入する。`ComfyUILibs` 側の内部コンストラクター（`InternalsVisibleTo` は `ComfyUILibsTests` のみ）は本プロジェクトのテストから使えないため、この境界が必要になる（`MainPageViewModel`/`DataViewModel` が実装例）。逆に、ファイル I/O のみでネットワーク通信を伴わない処理（設定ファイルの読み書きなど）はテストから実ファイルパス（一時ディレクトリ）でそのまま検証できるため、このファクトリー境界は不要（`ConfigViewModel` が実装例。`System.Text.Json` を直接使い、`ComfyUILibs.Services.ConfigLoader.ValidateWd14TaggerConfig` を保存前検証にのみ利用する）。
- `IProgress<T>` を ViewModel 内で使う場合、`System.Progress<T>` は `SynchronizationContext.Post` 経由でコールバックを非同期配送するためテストが不安定になりやすい。`await` がすべて UI スレッドのコンテキストを捕捉して再開する前提が成り立つなら、同期的に呼び出す自前の `IProgress<T>` 実装（`MainPageViewModel.SynchronousProgress<T>` が実装例）を使うとテストが決定的になる。
- ページをまたいで状態を共有したい場合、DI シングルトンの共有ストアで ViewModel 間を直接橋渡しする方法と、双方が同じ永続化ファイル（設定ファイル・出力ログ等）を参照する方法がありうる。本プロジェクトではフェーズ4で前者（`CaptioningRunResultStore`）を採用したが、フェーズ11で `MainPageViewModel` の実行結果が `AppConfig.ResultsFolder` 配下へ `captioning_result_*.json` として永続化されるようになったため、フェーズ12で `DataViewModel` 側を「ページ遷移のたびに `ResultsFolder` をスキャンして一覧表示する」（`ComfyUIRunWorkflow` の `DataViewModel.LoadResultsAsync` と同じパターン）方式に変更し、`CaptioningRunResultStore` は不要になったため削除した。ページ間の状態共有が「直近 1 件のみ・かつ永続化不要」なら DI シングルトンストア、「複数件の履歴・かつ既に永続化している」ならファイルスキャン方式が適する。

## テスト

- `ComfyUICaptioningToolTests`（xUnit、`ComfyUIRunWorkflowTests` を参考に新設、`ComfyUICaptioningTool.sln` に追加済み）。
- `Models/AppConfigTests.cs`・`ViewModels/Pages/MainPageViewModelTests.cs`・`ViewModels/Pages/DataViewModelTests.cs`・`ViewModels/Pages/ConfigViewModelTests.cs`・`ViewModels/Pages/SettingsViewModelTests.cs`・`ViewModels/Windows/MainWindowViewModelTests.cs`・`Helpers/LocalizationManagerTests.cs` が存在する（`Fakes/` に `FakeSnackbarService`・`FakeCaptioningService` を用意）。
- WPF の `FrameworkElement`（`SymbolIcon` 等）を生成するコード経路（スナックバー表示を伴う処理）のテストは STA スレッドが必要。`TestSupport/StaTestRunner.cs`（`DispatcherSynchronizationContext` + `Dispatcher.PushFrame` によるメッセージポンプ）と `TestSupport/StaThreadGate.cs`（テストクラス間で共有する lock）を参照。各テストクラスの `RunOnSta` はこれに委譲する。
  - 単に `new Thread(...)` に `ApartmentState.STA` を設定するだけでは不十分な場合がある: `SynchronizationContext` を持たない素の STA スレッドで、実際に非同期完了する I/O（`File.ReadAllLinesAsync` 等）を `await` すると、継続がスレッドプール（MTA）へ流れてしまい、その後の WPF オブジェクト生成が失敗する（本番コードは UI スレッドの `DispatcherSynchronizationContext` により正しく動作するため、これはテスト環境特有の問題）。`StaTestRunner` はこれを回避する。
- 実行環境では `dotnet test` が xunit.v3 のテストを検出できない場合がある（`ComfyUILibsTests` でも同様の事象が発生する既知の環境依存事象）。その場合はテストプロジェクトの `bin/**/net8.0-windows7.0/*.exe` を直接実行する（xunit.v3 の in-process ランナー）ことで代替確認できる。
