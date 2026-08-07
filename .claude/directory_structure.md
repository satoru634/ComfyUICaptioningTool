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
    Properties/
      PublishProfiles/
        FolderProfile.pubxml                <- dotnet publish / Visual Studio 発行ウィザード用の
                                                フォルダー発行プロファイル（ComfyUIRunWorkflow と同一設定。
                                                フェーズ25で追加）
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
      GalleryEditLogEntry.cs                <- GalleryPage でのタグ操作（追加・削除・並び替え）1 件分の
                                                作業ログエントリ（positional record、JSON プロパティ名は
                                                snake_case）。画像と同じディレクトリの gallery_edit_log.jsonl
                                                へ 1 行 1 エントリの JSON Lines 形式で追記される（フェーズ27）
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
                                                AddNewTagCommand/AddNewTagToStartCommand/RemoveTagCommand
                                                （実体は AddNewTagAsync/AddNewTagToStartAsync/RemoveTagAsync。
                                                [RelayCommand] の Async サフィックス除去により生成コマンド名は
                                                変わらない）が実際にタグを変更できた場合のみこのコールバックを
                                                呼び出す（GalleryViewModel の TagList 更新用。一括操作からは
                                                AddTag/RemoveTag を直接呼ぶためコールバックを経由しない）。
                                                AddTag は prepend 引数（既定 false）を持ち、true の場合は
                                                先頭に挿入する（フェーズ18で追加。AddNewTagToStartCommand が
                                                これを利用してカード上の「先頭に追加」ボタンを実装する）。
                                                SelectedTags（ObservableCollection&lt;string&gt;、複数選択可）・
                                                HasSelectedTags 派生プロパティ・ToggleTagSelectionCommand
                                                （選択のトグル）・RemoveSelectedTagsCommand（選択中の全タグを
                                                削除。CanExecute=HasSelectedTags）をフェーズ22で追加し、
                                                カード単位のタグ表示をトグルボタン化した。フェーズ26で、
                                                選択中のタグの並び替えコマンド
                                                MoveSelectedTagsToStartCommand/MoveSelectedTagsUpCommand/
                                                MoveSelectedTagsDownCommand/MoveSelectedTagsToEndCommand
                                                （いずれも CanExecute=HasSelectedTags）を追加した。
                                                ToStart/ToEnd は選択タグを相対順序を保ったまま抽出して
                                                Remove→先頭/末尾へ再挿入、Up/Down は Tags を先頭/末尾から
                                                走査し「自身が選択中かつ隣接要素が非選択」の場合のみ
                                                ObservableCollection&lt;T&gt;.Move で1つ移動する（隣接判定は
                                                走査時点のライブな SelectedTags.Contains によるため、
                                                連続選択タグはブロックとして一体で移動する）。いずれも
                                                順序変更のみで .txt への即時保存（SaveTags）は行うが、
                                                タグの追加・削除を伴わないため captioning_config_result.json
                                                （UpdateConfigResult）・TagList 更新コールバック
                                                （onTagsChangedAsync）は呼び出さない。
                                                フェーズ27で、AddTag/RemoveTag/MoveSelectedTagsToStart/
                                                ToEnd/Up/Down の各操作（実際に変更が生じた場合のみ）で
                                                LogEdit を呼び出し、画像と同じディレクトリの
                                                gallery_edit_log.jsonl（GalleryEditLogEntry を JSON Lines
                                                形式で1行追記）へ作業ログを記録するようにした。書き込み
                                                失敗時は握りつぶし、タグ編集本体には影響させない。
                                                フェーズ29で、AddNewTagAsync/AddNewTagToStartAsync
                                                （AddNewTagCommand/AddNewTagToStartCommand の実体）の冒頭に
                                                private bool SelectExistingTag(string input) の呼び出しを
                                                追加した。入力（trim 済み）が Tags と大文字小文字無視で
                                                一致する場合、AddTag は呼ばず（.txt/
                                                captioning_config_result.json/gallery_edit_log.jsonl への
                                                書き込みは発生しない）、代わりに一致した既存タグを
                                                SelectedTags へ追加する（既に選択済みなら何もしない、
                                                ToggleTagSelection のようなトグルではなく冪等な「選択オン」）。
                                                削除・並び替えボタン（RemoveSelectedTagsCommand/
                                                MoveSelectedTags*Command、いずれも CanExecute=HasSelectedTags）
                                                は SelectedTags.CollectionChanged 経由で既に
                                                NotifyCanExecuteChanged される実装（フェーズ22/26）のため、
                                                この変更のみで自動的に活性化する
      LanguageOption.cs                     <- 言語選択コンボボックスの1項目（Key/Label レコード）
    Helpers/
      EnumToBooleanConverter.cs             <- テーマ切り替え用列挙型コンバーター（テンプレート由来、流用可）
      LocalizationManager.cs                <- 表示文言解決用シングルトン（ComfyUIRunWorkflow から移植）。
                                                Strings.resx/.en.resx を CurrentCulture で参照し、
                                                XAML からインデクサーバインディングで利用する
      TagInCollectionConverter.cs           <- タグ文字列がコレクションに含まれるかどうかだけを判定する
                                                IMultiValueConverter（フェーズ22で新設）。GalleryPage の
                                                タグ選択トグルボタンの IsChecked 判定に使う。既存の
                                                TagExistsToBoolean は対象リストが空の場合に isInversion を
                                                反転した値を返す仕様のため、SelectedTags が空（初期状態）でも
                                                true になってしまう不具合があり、単純な包含判定のみを行う
                                                本クラスに差し替えて解決した
      ObjectEqualsConverter.cs               <- 2値が等しいか（Equals）だけを判定する汎用
                                                IMultiValueConverter（フェーズ23で新設）。GalleryPage の
                                                画像タイル選択状態（ToggleButton.IsChecked、タイル自身と
                                                GalleryViewModel.SelectedImage を比較）の判定に使う
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
                                                RunAsync の finally から呼び出す）。フェーズ20で、別の
                                                captioning_config.json から prepend_tags/exclude_tags を
                                                インポートし PrependTagsText/ExcludeTagsText へ追記する
                                                ImportTagsFromConfigCommand（実体は OpenFileDialog を開く
                                                ImportTagsFromConfig と、ダイアログ操作を伴わずテストできる
                                                よう分離した公開メソッド ImportTagsFromFile(string path)）を追加
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
                                                （BulkAddTagCommand/BulkAddTagToStartCommand/
                                                BulkRemoveTagCommand、フェーズ14で追加、先頭追加は
                                                フェーズ19で追加。実体は BulkAddTagAsync/
                                                BulkAddTagToStartAsync/BulkRemoveTagAsync で完了後に一度
                                                だけ TagList を再構築する）も持つ。LoadCommand 完了後・
                                                カード単位のタグ編集完了後にも TagList を再構築する。
                                                フェーズ23で、画像タイル一覧（左ペイン）で選択中の画像を
                                                保持する SelectedImage（GalleryImageEntry?）・タイル
                                                クリックで選択する SelectImageCommand（SelectedImage に
                                                代入するだけ）を追加した。LoadCommand 実行時
                                                （Images クリア時）に SelectedImage を null へリセットする
      ReportViewModel.cs                    <- タグ集計レポート表示ページの VM。ConfigPath から
                                                Wd14TaggerRunner を読み込み、対象ディレクトリを選択して
                                                タグ集計レポート（tags_report.txt）を生成・一覧表示する
                                                （旧 DataViewModel から分離）。レポート生成・解析本体は
                                                Services/TagReportGenerator.cs へ抽出済み（フェーズ16）。
                                                フェーズ24で、生成済みレポート全件を保持する
                                                _allReportEntries と、タグ名でのフィルタ入力
                                                FilterText（入力のたびに部分一致・大文字小文字無視で
                                                ReportEntries を絞り込む ApplyFilter を呼び出す）、
                                                ui:AutoSuggestBox のサジェスト候補用 TagList
                                                （生成済みレポートのタグ名一覧）を追加した。フェーズ28で、
                                                ListView で選択中のタグ SelectedTag（TagCountEntry?）と、
                                                それを使用している画像のファイル名一覧 TagUsageImages
                                                （ObservableCollection&lt;string&gt;、ファイル名昇順）を
                                                追加した。SelectedTag の変更は partial void
                                                OnSelectedTagChanged から LoadTagUsageImagesAsync を
                                                fire-and-forget で起動し、対象ディレクトリ内の画像
                                                （対応拡張子は GalleryViewModel と同じ .jpg/.jpeg/.png/.webp）
                                                の同名 .txt を読み、選択タグを含む（大文字小文字無視）
                                                画像のファイル名を収集する（画像収集ロジックは
                                                GalleryViewModel.CollectEntries/SplitTags と同内容を複製）。
                                                起動したタスクは TagUsageLoadTask（public Task）へ保持し、
                                                テストから完了を await で待てるようにしている。
                                                GenerateReportCommand 実行時は SelectedTag/TagUsageImages
                                                もリセットする
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
                                                してタグ候補を表示する。先頭に追加/末尾に追加/削除の
                                                3 ボタンをアイコン+ToolTip 表示で並べる。先頭追加ボタンは
                                                フェーズ19で追加））。
                                                フェーズ23で、画像・タグ一覧表示領域を左右2ペインの
                                                Grid に変更した。左ペインは ItemsControl
                                                （ItemsPanel=WrapPanel）による画像タイル一覧で、各タイルは
                                                サムネイル（読み込み失敗時は SymbolIcon プレースホルダー）＋
                                                ファイル名のみを表示する（タグ本体・編集 UI は持たない）。
                                                ListBox（SelectedItem バインド）は既定テンプレートの内部
                                                ScrollViewer が WrapPanel に無限の水平幅を与えてしまい
                                                タイルが折り返さず1列に並ぶ不具合があったため採用せず、
                                                各タイルを ToggleButton（新設 ImageTileToggleButtonStyle）
                                                にして選択を実現している。IsChecked は新設
                                                Helpers/ObjectEqualsConverter.cs（2値の Equals 判定のみを
                                                行う汎用 IMultiValueConverter）を使った MultiBinding
                                                （タイル自身 + ViewModel.SelectedImage）でバインドし、
                                                Command（GalleryViewModel.SelectImageCommand）でクリック時に
                                                選択を切り替える（タグ選択トグルボタン＝
                                                TagInCollectionConverter/ToggleTagSelectionCommand と同じ
                                                パターン）。選択中のタイルは IsChecked=True トリガーで
                                                ボーダーをアクセントカラー・太さ2pxに変更して強調表示する。
                                                右ペインは DataContext を ViewModel.SelectedImage にバインド
                                                したカードで、未選択時（null）は Gallery_SelectImagePrompt の
                                                プレースホルダーメッセージを表示し、選択時は旧カードが
                                                持っていたタグ一覧（フェーズ22でタグ名+×削除ボタンの構成から、
                                                タグ名を表記するトグルボタン（TagToggleButtonStyle、複数選択可、
                                                選択中はアクセントカラーで強調表示）のみの構成に変更。
                                                .txt 未存在時は「タグ未生成」表示）・タグ追加入力欄
                                                （コピー/先頭に追加/末尾に追加/選択タグを削除の4ボタン。
                                                先頭追加ボタンはフェーズ18、削除ボタン（RemoveSelectedTagsCommand、
                                                選択中のタグが1件以上ある場合のみ活性化）はフェーズ22で追加）と、
                                                選択タグの並び替え4ボタン（先頭へ/1つ前へ/1つ後ろへ/最後尾へ、
                                                MoveSelectedTagsToStart/Up/Down/ToEndCommand にバインド。
                                                フェーズ26で追加）を表示する。タグ追加入力欄はフェーズ30で
                                                ui:TextBox から ui:AutoSuggestBox へ変更し、
                                                OriginalItemsSource を Tags（DataContext である選択中の
                                                GalleryImageEntry 自身の Tags、= 選択中の画像に既に存在する
                                                タグ一覧）へバインドして候補表示する（一括タグ操作カードの
                                                ui:AutoSuggestBox が ViewModel.TagList を候補に使うのとは異なり、
                                                こちらは「選択中の画像 1 枚の既存タグ」のみが候補になる）。
                                                右ペインのタグ一覧は独自にスクロール可能な ScrollViewer
                                                だが、素のままだと画像・タグ一覧全体を包む外側の
                                                ScrollViewer までマウスホイールイベントがバブルせず外側の
                                                スクロールが機能しなくなるため、GalleryPage.xaml.cs に
                                                TagsScrollViewer_PreviewMouseWheel（内側がスクロール端に
                                                達している場合のみイベントを親要素へ手動転送する）を実装し、
                                                XAML 側から PreviewMouseWheel でアタッチしている
      ReportPage.xaml(.cs)                  <- タグ集計レポート表示画面（対象ディレクトリ選択・再帰
                                                オプション・生成・タグ/出現回数の一覧表示。旧 DataPage から分離）。
                                                フェーズ24で、列見出しの上に ui:AutoSuggestBox
                                                （Text=ViewModel.FilterText、OriginalItemsSource=
                                                ViewModel.TagList）を追加し、タグ名でのインタラクティブな
                                                フィルタリングに対応した。フェーズ28で、ui:ListView に
                                                SelectedItem=ViewModel.SelectedTag（TwoWay）を配線し、
                                                独立したカード（見出し Report_ImageUsageImagesColumnHeader
                                                + 内側の Border、従来は空のプレースホルダーだった箇所）に、
                                                選択中のタグを使用している画像のファイル名一覧を
                                                ItemsControl（ItemsPanel=WrapPanel、水平方向に折り返し）で
                                                表示するようにした。未選択時（SelectedTag が null）は
                                                Report_TagUsageImagesPrompt のプレースホルダー文言を表示する
                                                （GalleryPage.xaml の Gallery_SelectImagePrompt と同じ
                                                DataTrigger パターン）
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
      TagInCollectionConverterTests.cs      <- TagInCollectionConverter のテスト（フェーズ22で新設。
                                                values null/要素数不正・tagList 空/null・tag 空文字・
                                                含まれる/含まれない・大文字小文字無視・ConvertBack 未実装を検証）
      ObjectEqualsConverterTests.cs         <- ObjectEqualsConverter のテスト（フェーズ23で新設。
                                                values null/要素数不正・同一インスタンス/異なるインスタンス・
                                                両方 null/片方のみ null・ConvertBack 未実装を検証）
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
                                                指定時は呼ばれないことを検証するテストを追加。フェーズ18で、
                                                AddTag(prepend: true) が先頭挿入・重複排除を行うこと、
                                                AddNewTagToStartCommand 実行時の NewTagInput 反映・クリアと
                                                onTagsChangedAsync コールバックの呼び出し条件を検証する
                                                テストを追加。フェーズ22で、ToggleTagSelectionCommand による
                                                選択/解除・複数選択、RemoveSelectedTagsCommand の
                                                CanExecute（選択の有無）・選択タグの一括削除と .txt への反映・
                                                削除成功時のコールバック呼び出しを検証するテストを追加。
                                                フェーズ26で、MoveSelectedTagsToStart/ToEnd/Up/Down 各
                                                コマンドの CanExecute・相対順序を保った移動と .txt への反映・
                                                境界（先頭/末尾）到達時は変化しないこと・連続選択タグが
                                                ブロックとして一体で移動することを検証するテストを追加。
                                                フェーズ27で、gallery_edit_log.jsonl への作業ログ記録
                                                （AddTag/RemoveTag の add_start/add_end/remove 記録、
                                                MoveSelectedTagsToStart/ToEnd/Up/Down の reorder_*
                                                記録・実際に変化がない場合は記録しないこと・空文字/重複/
                                                存在しないタグ指定時は記録しないこと・複数回の操作が
                                                順番通り追記されること）を検証するテストを追加。フェーズ29で、
                                                AddNewTagCommand/AddNewTagToStartCommand に既存タグ名を
                                                入力した場合に追加ではなく選択状態への切り替えになること
                                                （SelectedTags への反映・入力欄のクリア・.txt を書き換えない
                                                こと・既に選択済みの場合は変化しないこと・TagList 更新
                                                コールバックが呼ばれないこと・RemoveSelectedTagsCommand の
                                                CanExecute が有効になること）を検証するテストを追加）
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
                                                CreateReadyVmAsync として追加した。フェーズ19で、
                                                BulkAddTagToStartCommand の CanExecute・全画像の先頭への
                                                一括追加・実行後の TagList 再構築を検証するテストを追加。
                                                フェーズ23で、SelectedImage の初期値が null であること・
                                                LoadCommand の再実行で SelectedImage が null にリセットされる
                                                こと・SelectImageCommand 実行で SelectedImage が更新される
                                                ことを検証するテストを追加）
      ReportViewModelTests.cs               <- ReportViewModel のテスト（ConfigPath 読み込み成否・
                                                GenerateReportCommand の CanExecute/実行・レポート行の解析
                                                （コロンを含むタグ名を含む）・エラーハンドリング。
                                                旧 DataViewModelTests から分離。レポート行解析自体の
                                                詳細な検証は Services/TagReportGeneratorTests.cs 側にも
                                                持つ（フェーズ16でロジックを抽出したため重複気味だが、
                                                ReportViewModel 側は「サービス呼び出し～画面表示」の
                                                結合的な検証として残している）。フェーズ24で、
                                                GenerateReportCommand 実行後の TagList 反映・FilterText の
                                                部分一致/大文字小文字無視でのフィルタリング・空文字に戻した
                                                際の全件表示への復帰・一致なし時の空表示・レポート再生成時に
                                                前回の FilterText がリセットされることを検証するテストを追加。
                                                フェーズ28で、SelectedTag 設定時に TagUsageImages が
                                                ファイル名昇順で反映されること・null 設定で空になること・
                                                タグ一致が大文字小文字無視であること・該当画像なし時は
                                                空のままであること・SelectedTag 変更で前回の一覧が
                                                置き換わること・GenerateReportCommand 再実行時に
                                                SelectedTag/TagUsageImages がリセットされることを検証する
                                                テストを追加（非同期処理の完了は ReportViewModel.
                                                TagUsageLoadTask を await して待つ）
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
