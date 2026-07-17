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
                                                captioning_config.json のパスを保持する ConfigPath、
                                                実行結果ログ（captioning_result_*.json）の出力先フォルダを
                                                保持する ResultsFolder（既定: カレントディレクトリ直下の
                                                Results。ComfyUIRunWorkflow と同じ方式）を追加済み。
                                                既定 prepend/exclude タグは captioning_config.json 側の
                                                prepend_tags/exclude_tags で保持する方式に一本化したため、
                                                本クラスには持たない）
      CaptioningResultLog.cs                <- 実行ログ（1 ファイルごとの処理結果・成功/失敗ステータス）と
                                                今回使用した設定（WorkflowConfig、prepend/exclude タグはマージ後）
                                                をマージした結果ログ（positional record）。AppConfig.ResultsFolder
                                                配下へ captioning_result_{timestamp}.json として出力される
      CaptioningResultLogPreview.cs         <- DataPage の一覧表示用に CaptioningResultLog と整形済み表示文字列
                                                （日時・サマリ/エラーメッセージ）をまとめた positional record
      TagCountEntry.cs                      <- tags_report.txt の 1 行（Tag/Count）を表す positional record
      GalleryImageEntry.cs                  <- GalleryPage の一覧表示用に、画像 1 枚とその同名 .txt から
                                                読み込んだタグ・サムネイル（BitmapImage?）をまとめた
                                                ObservableObject（HasTags 派生プロパティを持つ）。
                                                Tags は ObservableCollection&lt;string&gt; で、AddTag/RemoveTag
                                                （フェーズ14で追加、カード単位のタグ追加・削除。実際に
                                                追加/削除できたかを bool で返す）呼び出しのたびに即座に
                                                同名 .txt へ保存する（0 件になった場合は .txt 自体を削除する）。
                                                あわせて画像と同じディレクトリの captioning_config_result.json
                                                へも反映する（フェーズ15で追加。追加タグは prepend_tags、
                                                削除タグは exclude_tags へ、矛盾する側は取り除いた上で追記。
                                                読み込み・保存失敗時は握りつぶし .txt 保存自体には影響させない）。
                                                コンストラクターに Func&lt;Task&gt;? onTagsChangedAsync を
                                                受け取り（フェーズ17で追加）、カード単位のコマンド
                                                AddNewTagCommand/RemoveTagCommand（実体は AddNewTagAsync/
                                                RemoveTagAsync。[RelayCommand] の Async サフィックス除去に
                                                より生成コマンド名は変わらない）が実際にタグを変更できた
                                                場合のみこのコールバックを呼び出す（GalleryViewModel の
                                                TagList 更新用。一括操作からは AddTag/RemoveTag を直接
                                                呼ぶためコールバックを経由しない）
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
      TagReportGenerator.cs                 <- ICaptioningService.GenerateReportAsync を呼び出し、
                                                tags_report.txt を読み込んで TagCountEntry のリストへ
                                                変換する静的クラス（フェーズ16で ReportViewModel から
                                                抽出。GalleryViewModel 等の別 ViewModel でも
                                                タグ一覧取得に再利用できるようにしたもの）
    ViewModels/Pages/
      MainPageViewModel.cs                  <- ディレクトリ一括タグ付け実行ページの VM。ConfigPath から
                                                Wd14TaggerRunner を読み込み、ICaptioningService 経由で
                                                ProcessDirectoryAsync/GenerateReportAsync を実行する。
                                                captioning_config.json をベースに prepend_tags/exclude_tags を
                                                マージ結果へ差し替えた captioning_config_result.json を
                                                対象ディレクトリ直下へ出力する（SaveExecutedConfigAsync）。
                                                さらに実行ログ + 使用した設定をマージした CaptioningResultLog を
                                                AppConfig.ResultsFolder 配下へ captioning_result_{timestamp}.json
                                                として出力する（SaveResultLogAsync、成功・失敗どちらの場合も
                                                RunAsync の finally から呼び出す）
      DataViewModel.cs                      <- 実行結果表示ページの VM。AppConfig.ResultsFolder 配下の
                                                captioning_result_*.json を新しい順に読み込んで一覧表示する
                                                （RefreshCommand・ページ遷移のたびに再読み込みする
                                                OnNavigatedToAsync を実装）
      GalleryViewModel.cs                   <- 画像・タグ一覧ページの VM。対象ディレクトリ内の画像を収集し、
                                                同名 .txt からタグを読み込んでサムネイルと共に一覧表示する
                                                （LoadCommand）。画像・タグ一覧本体の表示は ComfyUI と通信
                                                しないため ICaptioningService ファクトリー境界・
                                                Wd14TaggerRunner には依存しないが、一括タグ操作の
                                                AutoSuggestBox 候補一覧 TagList（フェーズ17で追加）の取得
                                                （TagReportGenerator 経由で tags_report.txt を生成・解析）
                                                にのみこれらに依存する（INavigationAware.OnNavigatedToAsync
                                                で ConfigPath から Wd14TaggerRunner を読み込む。失敗しても
                                                TagList 更新を静かにスキップするのみでエラー表示はしない）。
                                                読み込み済み全画像に対する一括タグ追加・削除
                                                （BulkAddTagCommand/BulkRemoveTagCommand、フェーズ14で追加。
                                                実体は BulkAddTagAsync/BulkRemoveTagAsync で完了後に一度
                                                だけ TagList を再構築する）も持つ。LoadCommand 完了後・
                                                カード単位のタグ編集完了後にも TagList を再構築する
      ReportViewModel.cs                    <- タグ集計レポート表示ページの VM。ConfigPath から
                                                Wd14TaggerRunner を読み込み、対象ディレクトリを選択して
                                                タグ集計レポート（tags_report.txt）を生成・一覧表示する
                                                （旧 DataViewModel から分離）。レポート生成・解析本体は
                                                Services/TagReportGenerator.cs へ抽出済み（フェーズ16）
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
      DataPage.xaml(.cs)                    <- 実行結果表示画面（AppConfig.ResultsFolder 配下の
                                                captioning_result_*.json を新しい順に一覧表示。各カードに
                                                ディレクトリ/日時/サマリ・エラーメッセージ/1 ファイルごとの
                                                処理結果ログを表示し、更新ボタンで再スキャンする）
      GalleryPage.xaml(.cs)                 <- 画像・タグ一覧画面（対象ディレクトリ選択・再帰オプション・
                                                読み込みボタン、一括タグ操作カード（フェーズ14で追加。
                                                入力欄はフェーズ17で ui:TextBox から ui:AutoSuggestBox へ
                                                変更し、OriginalItemsSource を ViewModel.TagList へバインド
                                                してタグ候補を表示する）、WrapPanel によるカード折り返し
                                                表示。各カードに
                                                サムネイル（読み込み失敗時は SymbolIcon プレースホルダー）・
                                                ファイル名・タグ一覧（チップ表示、削除ボタン付き、.txt 未存在時は
                                                「タグ未生成」表示）・タグ追加入力欄を表示する）。
                                                各カードのタグ一覧は独自にスクロール可能な入れ子 ScrollViewer
                                                だが、素の入れ子のままだと画像・タグ一覧全体を包む外側の
                                                ScrollViewer までマウスホイールイベントがバブルせず外側の
                                                スクロールが機能しなくなるため、GalleryPage.xaml.cs に
                                                TagsScrollViewer_PreviewMouseWheel（内側がスクロール端に
                                                達している場合のみイベントを親要素へ手動転送する）を実装し、
                                                XAML 側から PreviewMouseWheel でアタッチしている
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
      GalleryImageEntryTests.cs             <- GalleryImageEntry のテスト（フェーズ14で新設。AddTag の
                                                trim/重複排除（大文字小文字無視）/既存タグへの追記と
                                                対応する .txt 書き込み内容、RemoveTag の存在有無別の挙動
                                                （最後の1件削除時は .txt 自体が削除されること）、
                                                AddNewTagCommand 実行時の NewTagInput 反映・クリアを検証。
                                                フェーズ17で、onTagsChangedAsync コールバックが
                                                AddNewTagCommand/RemoveTagCommand 実行時に実際にタグを
                                                変更できた場合のみ呼ばれ、空文字入力や存在しないタグ
                                                指定時は呼ばれないことを検証するテストを追加）
    Services/
      TagReportGeneratorTests.cs            <- TagReportGenerator のテスト（フェーズ16で新設。
                                                ICaptioningService 呼び出し引数の検証・レポート行の解析
                                                （複数件・コロンを含むタグ名）・例外伝播を検証）
    TestSupport/
      StaThreadGate.cs                      <- STA スレッドで WPF オブジェクトを生成するテスト同士を直列化する共有 lock
      StaTestRunner.cs                      <- 非同期 ViewModel メソッドを STA スレッド上で実行するヘルパー。
                                                DispatcherSynchronizationContext + Dispatcher.PushFrame で
                                                実際の非同期 I/O（File.ReadAllLinesAsync 等）の継続もこのスレッドへ戻す
    ViewModels/Pages/
      MainPageViewModelTests.cs             <- MainPageViewModel のテスト（ConfigPath 読み込み成否・
                                                RunCommand の CanExecute/実行・進捗/ログ/サマリ・エラーハンドリング・
                                                既定/入力タグの union と重複排除・
                                                ResultsFolder への captioning_result_*.json 出力（成功/失敗
                                                双方のステータス・ResultsFolder 未設定時のスキップ））。
                                                SymbolIcon 生成を伴うテストは STA スレッドが必要なため
                                                RunOnSta（TestSupport.StaTestRunner に委譲）でラップ
      DataViewModelTests.cs                 <- DataViewModel のテスト（ResultsFolder 未設定/未存在/結果なし時の
                                                状態メッセージ・captioning_result_*.json の新しい順読み込みと
                                                成功/失敗の表示文字列・不正な JSON ファイルのスキップ・
                                                RefreshCommand による再読み込み）
      GalleryViewModelTests.cs              <- GalleryViewModel のテスト（初期状態・LoadCommand の CanExecute・
                                                ディレクトリ未存在/画像0件時のメッセージ・タグの trim/空要素除去・
                                                .txt なし画像の HasTags=false・非対応拡張子の除外・Recursive
                                                の有無・ファイル名昇順ソート・不正な画像バイト列でも
                                                Thumbnail=null のままエントリが残ることを検証。フェーズ14で
                                                BulkAddTagCommand/BulkRemoveTagCommand の CanExecute・
                                                全画像への一括追加/削除（大文字小文字無視）のテストを追加。
                                                フェーズ17で、TagList の初期状態が空であること・ConfigPath
                                                未設定時は LoadCommand 実行後も空のままであること・有効な
                                                ConfigPath + tags_report.txt から TagList が反映されること・
                                                レポート生成失敗時は TagList が空のまま影響を受けないこと・
                                                BulkAddTagCommand/カード単位の AddNewTagCommand 実行後に
                                                TagList が再構築されることを検証するテストを追加。
                                                ReportViewModelTests と同様の Wd14TaggerRunner テンプレート
                                                ファイル配置・captioning_config.json 書き出しヘルパーを
                                                CreateReadyVmAsync として追加した）
      ReportViewModelTests.cs               <- ReportViewModel のテスト（ConfigPath 読み込み成否・
                                                GenerateReportCommand の CanExecute/実行・レポート行の解析
                                                （コロンを含むタグ名を含む）・エラーハンドリング。
                                                旧 DataViewModelTests から分離。レポート行解析自体の
                                                詳細な検証は Services/TagReportGeneratorTests.cs 側にも
                                                持つ（フェーズ16でロジックを抽出したため重複気味だが、
                                                ReportViewModel 側は「サービス呼び出し～画面表示」の
                                                結合的な検証として残している）
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
