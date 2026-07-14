# ディレクトリ構成

```
ComfyUICaptioningTool/                      <- ソリューションルート
  ComfyUILibs/                              <- Git submodule（.gitmodules 参照）。
                                                ただしビルドで実際に使われる実体ではない
                                                （CLAUDE.md の「ComfyUILibs の参照経路に注意」を参照）
  ComfyUICaptioningTool/                    <- メイン WPF プロジェクト（GUI のみ）
    App.xaml / App.xaml.cs                  <- DI・ホスト設定（ComfyUIRunWorkflow から流用）。
                                                CaptioningRunResultStore をシングルトン登録
    AssemblyInfo.cs
    app.manifest
    wpfui-icon.ico
    Assets/
      wpfui-icon-256.png / wpfui-icon-1024.png
    Models/
      AppConfig.cs                          <- アプリ設定ルート（WindowSettingData・Language に加え、
                                                captioning_config.json のパスを保持する ConfigPath、
                                                実行結果ログ（captioning_result_*.json）の出力先フォルダを
                                                保持する ResultsFolder（既定: カレントディレクトリ直下の
                                                Results。ComfyUIRunWorkflow と同じ方式）を追加済み。
                                                既定 prepend/exclude タグは captioning_config.json 側の
                                                prepend_tags/exclude_tags で保持する方式に一本化したため、
                                                本クラスには持たない）
      CaptioningRunResult.cs                <- MainPageViewModel の実行結果スナップショット（positional record）。
                                                CaptioningRunResultStore 経由で DataPage と共有する
      CaptioningResultLog.cs                <- 実行ログ（1 ファイルごとの処理結果・成功/失敗ステータス）と
                                                今回使用した設定（WorkflowConfig、prepend/exclude タグはマージ後）
                                                をマージした結果ログ（positional record）。AppConfig.ResultsFolder
                                                配下へ captioning_result_{timestamp}.json として出力される
      TagCountEntry.cs                      <- tags_report.txt の 1 行（Tag/Count）を表す positional record
      LanguageOption.cs                     <- 言語選択コンボボックスの1項目（Key/Label レコード）
    Helpers/
      EnumToBooleanConverter.cs             <- テーマ切り替え用列挙型コンバーター（テンプレート由来、流用可）
      LocalizationManager.cs                <- 表示文言解決用シングルトン（ComfyUIRunWorkflow から移植）。
                                                Strings.resx/.en.resx を CurrentCulture で参照し、
                                                XAML からインデクサーバインディングで利用する
    Resources/
      Strings.resx                          <- 既定（日本語）の表示文言リソース
      Strings.en.resx                       <- 英語の表示文言リソース（culture=en の satellite resource）
      Strings.cs                            <- 上記 .resx を参照する ResourceManager の公開ラッパー
      Translations.cs                       <- テンプレート由来の未使用スタブ（ComfyUIRunWorkflow 側にも同じものが残存）
    Services/
      ApplicationHostService.cs             <- 起動時ウィンドウ表示。起動時に Config.Data.Language から
                                                LocalizationManager.Instance.CurrentCulture を適用する
      ICaptioningService.cs                 <- ComfyUILibs.Services.CaptioningService の公開メソッドのうち
                                                MainPageViewModel/DataViewModel が使う部分だけを抜き出した
                                                インターフェース（テスト時にネットワーク通信を伴う実装を
                                                差し替えるための境界）
      CaptioningServiceAdapter.cs           <- ICaptioningService の既定実装（実 CaptioningService をラップ）
      CaptioningRunResultStore.cs           <- 直近の実行結果（CaptioningRunResult）を保持する ObservableObject の
                                                DI シングルトン。MainPageViewModel が更新し、DataViewModel が購読する
    ViewModels/Pages/
      MainPageViewModel.cs                  <- ディレクトリ一括タグ付け実行ページの VM。ConfigPath から
                                                Wd14TaggerRunner を読み込み、ICaptioningService 経由で
                                                ProcessDirectoryAsync/GenerateReportAsync を実行する。
                                                実行成功時に CaptioningRunResultStore.LastResult を更新し、
                                                captioning_config.json をベースに prepend_tags/exclude_tags を
                                                マージ結果へ差し替えた captioning_config_result.json を
                                                対象ディレクトリ直下へ出力する（SaveExecutedConfigAsync）。
                                                さらに実行ログ + 使用した設定をマージした CaptioningResultLog を
                                                AppConfig.ResultsFolder 配下へ captioning_result_{timestamp}.json
                                                として出力する（SaveResultLogAsync、成功・失敗どちらの場合も
                                                RunAsync の finally から呼び出す）
      DataViewModel.cs                      <- 実行結果表示ページの VM。CaptioningRunResultStore.LastResult
                                                を購読して直近の実行結果（ディレクトリ/日時/サマリ/ログ）を
                                                表示するのみ（ページ遷移時の処理がないため INavigationAware
                                                は非実装）
      ReportViewModel.cs                    <- タグ集計レポート表示ページの VM。ConfigPath から
                                                Wd14TaggerRunner を読み込み、対象ディレクトリを選択して
                                                タグ集計レポート（tags_report.txt）を生成・一覧表示する
                                                （旧 DataViewModel から分離）
      SettingsViewModel.cs                  <- 設定 VM。テーマ・言語切り替え、captioning_config.json の
                                                パス選択（BrowseConfigPathCommand）、実行結果ログ出力先
                                                ResultsFolder の選択（BrowseResultsFolderCommand）を実装済み
      ConfigViewModel.cs                    <- captioning_config.json 編集ページの VM。ConfigPath が指す
                                                ファイルを System.Text.Json で直接読み書きする（ComfyUI との
                                                通信は行わないため ICaptioningService 経由のファクトリー境界は
                                                不要。ConfigLoader.ValidateWd14TaggerConfig で保存前検証のみ行う）
    ViewModels/Windows/
      MainWindowViewModel.cs                <- ナビゲーション定義・ウィンドウ状態保存。メニュー項目は
                                                BuildMenuItems() で LocalizationManager から都度構築し、
                                                言語切替時（PropertyChanged）に再構築する
    Views/Pages/
      MainPage.xaml(.cs)                    <- ディレクトリ一括タグ付け実行画面（対象ディレクトリ選択・
                                                再帰/上書き/レポート生成オプション・prepend/exclude タグ入力・
                                                進捗バー/ログ・完了サマリ）
      DataPage.xaml(.cs)                    <- 実行結果表示画面（直近の実行結果のディレクトリ/日時/
                                                サマリ/ログ表示のみ）
      ReportPage.xaml(.cs)                  <- タグ集計レポート表示画面（対象ディレクトリ選択・再帰
                                                オプション・生成・タグ/出現回数の一覧表示。旧 DataPage から分離）
      ConfigPage.xaml(.cs)                   <- captioning_config.json 編集画面（comfyui_url・WD14 モデル名・
                                                しきい値・prepend/exclude タグ既定値の編集、保存ボタン）
      SettingsPage.xaml(.cs)                <- 設定画面（テーマ・言語切り替え、captioning_config.json パス選択、
                                                実行結果ログ出力先 ResultsFolder のフォルダ選択）。
                                                ラベルは LocalizationManager バインディング
    Views/Windows/
      MainWindow.xaml(.cs)                  <- ナビゲーションホスト
    Usings.cs
  ComfyUICaptioningToolTests/                <- xUnit テストプロジェクト（ComfyUIRunWorkflowTests を参考に新設）
    Fakes/
      FakeSnackbarService.cs                <- ISnackbarService のテスト用スタブ（Show 呼び出し履歴を記録）
      FakeCaptioningService.cs              <- ICaptioningService のテスト用スタブ（進捗・結果・例外発生を
                                                あらかじめ設定可能。ProcessDirectoryAsync/GenerateReportAsync
                                                それぞれ個別に例外を発生させられる）
    Helpers/
      LocalizationManagerTests.cs           <- LocalizationManager のカルチャ切替・フォールバック挙動のテスト
    Models/
      AppConfigTests.cs                     <- AppConfig/WindowSettingData のデフォルト値・PropertyChanged のテスト
    Services/
      CaptioningRunResultStoreTests.cs      <- CaptioningRunResultStore の初期値・PropertyChanged のテスト
    TestSupport/
      StaThreadGate.cs                      <- STA スレッドで WPF オブジェクトを生成するテスト同士を直列化する共有 lock
      StaTestRunner.cs                      <- 非同期 ViewModel メソッドを STA スレッド上で実行するヘルパー。
                                                DispatcherSynchronizationContext + Dispatcher.PushFrame で
                                                実際の非同期 I/O（File.ReadAllLinesAsync 等）の継続もこのスレッドへ戻す
    ViewModels/Pages/
      MainPageViewModelTests.cs             <- MainPageViewModel のテスト（ConfigPath 読み込み成否・
                                                RunCommand の CanExecute/実行・進捗/ログ/サマリ・エラーハンドリング・
                                                既定/入力タグの union と重複排除・ResultStore への反映・
                                                ResultsFolder への captioning_result_*.json 出力（成功/失敗
                                                双方のステータス・ResultsFolder 未設定時のスキップ））。
                                                SymbolIcon 生成を伴うテストは STA スレッドが必要なため
                                                RunOnSta（TestSupport.StaTestRunner に委譲）でラップ
      DataViewModelTests.cs                 <- DataViewModel のテスト（ResultStore 購読による
                                                導出プロパティ更新のみ）
      ReportViewModelTests.cs               <- ReportViewModel のテスト（ConfigPath 読み込み成否・
                                                GenerateReportCommand の CanExecute/実行・レポート行の解析
                                                （コロンを含むタグ名を含む）・エラーハンドリング。
                                                旧 DataViewModelTests から分離）
      SettingsViewModelTests.cs             <- SettingsViewModel のテスト（テーマ・言語切り替え等）
      ConfigViewModelTests.cs               <- ConfigViewModel のテスト（ConfigPath 読み込み成否・
                                                ファイル未存在時の新規作成扱い・SaveCommand の CanExecute/実行・
                                                保存前バリデーション・タグ既定値の union なしの単純反映）
    ViewModels/Windows/
      MainWindowViewModelTests.cs           <- MainWindowViewModel のテスト（メニュー項目構築・ウィンドウクローズ時保存等）
```

## 現時点で存在しないもの（ComfyUIRunWorkflow との差分）

- `doc/` ディレクトリ（使い方ドキュメント・クラス図など）
- `templates/` ディレクトリ（WD14 Tagger 用ワークフローテンプレートは `ComfyUILibs` 側の `template_wd14_tagger.json` を利用する想定だが、本プロジェクト側への配置は未着手。実行時は `captioning_config.json` と同様、実行ファイルと同階層の `templates/` に配置する必要がある）
