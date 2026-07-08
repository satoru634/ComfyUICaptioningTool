# ディレクトリ構成

```
ComfyUICaptioningTool/                      <- ソリューションルート
  ComfyUILibs/                              <- Git submodule（.gitmodules 参照）。
                                                ただしビルドで実際に使われる実体ではない
                                                （CLAUDE.md の「ComfyUILibs の参照経路に注意」を参照）
  ComfyUICaptioningTool/                    <- メイン WPF プロジェクト（GUI のみ）
    App.xaml / App.xaml.cs                  <- DI・ホスト設定（ComfyUIRunWorkflow から流用）
    AssemblyInfo.cs
    app.manifest
    wpfui-icon.ico
    Assets/
      wpfui-icon-256.png / wpfui-icon-1024.png
    Models/
      AppConfig.cs                          <- アプリ設定ルート（WindowSettingData・Language に加え、
                                                workflow_config.json のパスを保持する ConfigPath、
                                                既定の prepend/exclude タグ（カンマ区切り文字列）を保持する
                                                DefaultPrependTags/DefaultExcludeTags を追加済み）
      DataColor.cs                          <- テンプレート由来のサンプルモデル（DataPage のランダムカラー表示用。未使用に置換予定）
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
                                                MainPageViewModel が使う部分だけを抜き出したインターフェース
                                                （テスト時にネットワーク通信を伴う実装を差し替えるための境界）
      CaptioningServiceAdapter.cs           <- ICaptioningService の既定実装（実 CaptioningService をラップ）
    ViewModels/Pages/
      MainPageViewModel.cs                  <- ディレクトリ一括タグ付け実行ページの VM。ConfigPath から
                                                Wd14TaggerRunner を読み込み、ICaptioningService 経由で
                                                ProcessDirectoryAsync/GenerateReportAsync を実行する
      DataViewModel.cs                      <- テンプレート由来のランダムカラー一覧デモ（→ 処理結果・タグ集計レポート表示 VM に置換予定）
      SettingsViewModel.cs                  <- 設定 VM。テーマ・言語切り替え、workflow_config.json の
                                                パス選択（BrowseConfigPathCommand）に加え、既定の
                                                prepend/exclude タグ（Config.Data への直接バインディングのみ、
                                                専用プロパティ・OnChanged なし）を実装済み
    ViewModels/Windows/
      MainWindowViewModel.cs                <- ナビゲーション定義・ウィンドウ状態保存。メニュー項目は
                                                BuildMenuItems() で LocalizationManager から都度構築し、
                                                言語切替時（PropertyChanged）に再構築する
    Views/Pages/
      MainPage.xaml(.cs)                    <- ディレクトリ一括タグ付け実行画面（対象ディレクトリ選択・
                                                再帰/上書き/レポート生成オプション・prepend/exclude タグ入力・
                                                進捗バー/ログ・完了サマリ）
      DataPage.xaml(.cs)                    <- テンプレート由来のランダムカラー一覧画面
      SettingsPage.xaml(.cs)                <- 設定画面（テーマ・言語切り替え、workflow_config.json パス選択、
                                                既定 prepend/exclude タグ入力）。ラベルは LocalizationManager バインディング
    Views/Windows/
      MainWindow.xaml(.cs)                  <- ナビゲーションホスト
    Usings.cs
  ComfyUICaptioningToolTests/                <- xUnit テストプロジェクト（ComfyUIRunWorkflowTests を参考に新設）
    Fakes/
      FakeSnackbarService.cs                <- ISnackbarService のテスト用スタブ（Show 呼び出し履歴を記録）
      FakeCaptioningService.cs              <- ICaptioningService のテスト用スタブ（進捗・結果をあらかじめ設定可能）
    Helpers/
      LocalizationManagerTests.cs           <- LocalizationManager のカルチャ切替・フォールバック挙動のテスト
    Models/
      AppConfigTests.cs                     <- AppConfig/WindowSettingData のデフォルト値・PropertyChanged のテスト
    ViewModels/Pages/
      MainPageViewModelTests.cs             <- MainPageViewModel のテスト（ConfigPath 読み込み成否・
                                                RunCommand の CanExecute/実行・進捗/ログ/サマリ・エラーハンドリング・
                                                既定/入力タグの union と重複排除）。SymbolIcon 生成を伴うテストは
                                                STA スレッドが必要なため RunOnSta でラップ
      SettingsViewModelTests.cs             <- SettingsViewModel のテスト（テーマ・言語切り替え等）
    ViewModels/Windows/
      MainWindowViewModelTests.cs           <- MainWindowViewModel のテスト（メニュー項目構築・ウィンドウクローズ時保存等）
```

## 現時点で存在しないもの（ComfyUIRunWorkflow との差分）

- `doc/` ディレクトリ（使い方ドキュメント・クラス図など）
- `templates/` ディレクトリ（WD14 Tagger 用ワークフローテンプレートは `ComfyUILibs` 側の `template_wd14_tagger.json` を利用する想定だが、本プロジェクト側への配置は未着手。実行時は `workflow_config.json` と同様、実行ファイルと同階層の `templates/` に配置する必要がある）
- キャプショニング機能固有の Model / View 一式（`DataPage` 側、フェーズ4で対応）
