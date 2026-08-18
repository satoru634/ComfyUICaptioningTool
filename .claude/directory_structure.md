# ディレクトリ構成

```
ComfyUICaptioningTool/                      <- ソリューションルート
  ComfyUILibs/                              <- Git submodule（.gitmodules 参照）。
                                                ComfyUICaptioningTool.csproj の ProjectReference が
                                                実際に参照する実体（フェーズ7で参照パスを修正済み。
                                                詳細は CLAUDE.md の「ComfyUILibs の参照経路に注意」を参照）
  wdv3-timm/                                 <- Git submodule（.gitmodules 参照、satoru634/wdv3-timm）。
                                                timm ライブラリで WD Tagger V3 を実行する単一スクリプト
                                                （wdv3_timm.py）のリポジトリ。`--serve` 常駐サーバーモードを
                                                持ち、ComfyUILibs.Services.WdV3TimmTaggerRunner から
                                                サブプロセスとして起動される想定（プロトコル契約は
                                                wdv3-timm リポジトリ側 .claude/usage.md の「サーバーモード」
                                                節、および ComfyUILibs 側 IWdV3TimmProcessClient.cs の
                                                XML ドキュメントコメントを参照）。C# プロジェクトからの
                                                直接参照（ProjectReference）はなく、実行時にランタイム
                                                パス（ComfyUILibs.Services.WdV3TimmPaths が指す、アプリ
                                                実行ファイルと同階層の wdv3-timm フォルダ固定。フェーズ32の
                                                追加修正で config ファイル指定から変更）として利用する。
                                                .venv・wdv3_timm.exe 自体はリポジトリに含まれないため、
                                                SettingsPage の「ビルド」ボタン（WdV3TimmBuildService が
                                                本サブモジュール同梱の setup.bat → build_exe.bat を実行）で
                                                都度構築する想定。フェーズ33で、ComfyUICaptioningTool.csproj
                                                の Content ItemGroup（Link メタデータで出力先を wdv3-timm\
                                                フォルダへ再配置）により、setup.bat/build_exe.bat 実行に
                                                必要な最小限のファイル（setup.bat・build_exe.bat・
                                                requirements.txt・launcher.py・wdv3_timm.py の5点。README 等の
                                                ドキュメント類・.venv・dist/build 中間生成物は含まない）を
                                                dotnet build/publish のたびに実行ファイルと同階層の
                                                wdv3-timm\ フォルダへ自動展開するようにした
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
                                                本クラスには持たない。フェーズ32で TaggerBackend
                                                （既定 TaggerBackend.ComfyUI）を追加し、SettingsPage の
                                                コンボボックスで ComfyUI 経由／ローカル wdv3-timm 経由の
                                                タグ付けバックエンドを切り替えられるようにした）
      TaggerBackend.cs                        <- タグ付けバックエンドの種別を表す列挙型（ComfyUI/WdV3Timm）。
                                                AppConfig.TaggerBackend で保持する（フェーズ32で新設）
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
      CaptioningServiceAdapter.cs           <- ICaptioningService の既定実装（実 CaptioningService をラップ）。
                                                コンストラクター引数はフェーズ32で ITaggerRunner
                                                （Wd14TaggerRunner の具象型ではなく抽象）に変更した
      TaggerRunnerFactory.cs                <- captioning_config.json のパスと TaggerBackend から
                                                ITaggerRunner の実装（Wd14TaggerRunner または
                                                WdV3TimmTaggerRunner）を構築する静的ファクトリー
                                                （フェーズ32で新設）。MainPageViewModel/ReportViewModel/
                                                GalleryViewModel の TryLoadRunnerAsync から呼ばれる
      TagReportGenerator.cs                 <- ICaptioningService.GenerateReportAsync を呼び出し、
                                                tags_report.txt を読み込んで TagCountEntry のリストへ
                                                変換する静的クラス（フェーズ16で ReportViewModel から
                                                抽出。GalleryViewModel 等の別 ViewModel でも
                                                タグ一覧取得に再利用できるようにしたもの）
      IWdV3TimmBuildService.cs              <- wdv3-timm 実行環境（.venv・wdv3_timm.exe）のビルドを
                                                抽象化するインターフェース（フェーズ32の追加修正で新設、
                                                DI / テスト用の境界）。bool IsExeReady（固定パスに
                                                wdv3_timm.exe が存在するか）と
                                                Task&lt;bool&gt; BuildAsync(IProgress&lt;string&gt;, CancellationToken)
                                                を持つ
      WdV3TimmBuildService.cs               <- IWdV3TimmBuildService の既定実装。IsExeReady は
                                                File.Exists(ComfyUILibs.Services.WdV3TimmPaths.ExeFilePath)。
                                                BuildAsync は wdv3-timm 同梱の setup.bat → build_exe.bat を
                                                cmd.exe /c 経由で順に実行し（前者が終了コード 0 の場合のみ
                                                後者を実行）、RedirectStandardOutput/Error +
                                                OutputDataReceived/ErrorDataReceived で標準出力・エラー
                                                出力を 1 行ごとに IProgress&lt;string&gt;.Report へ流す。
                                                **フェーズ34の修正**: OutputDataReceived/ErrorDataReceived は
                                                AsyncStreamReader 専用のスレッドプールのスレッドから発火する
                                                ため、ハンドラー内で直接 Report を呼ぶと
                                                SettingsViewModel.SynchronousProgress&lt;T&gt; が UI スレッド
                                                以外から WdV3TimmBuildLogEntries（UI バインド済み
                                                ObservableCollection）を変更してしまい、CollectionView の
                                                NotSupportedException（「Dispatcher スレッドとは異なる
                                                スレッドからの SourceCollection 変更はサポートしない」）を
                                                引き起こしていた。ハンドラーは
                                                System.Threading.Channels.Channel&lt;string&gt; への書き込みのみ
                                                に留め、Report の呼び出しは RunScriptAsync 自身の
                                                await foreach（呼び出し元の SynchronizationContext を捕捉して
                                                再開する）から行うよう修正し、常に呼び出し元スレッド（本番では
                                                UI スレッド）で Report が呼ばれるようにした。両ストリームの
                                                EOF（e.Data == null）を Interlocked.Decrement でカウントし、
                                                両方閉じた時点で Channel の Writer を TryComplete する。
                                                ルートフォルダ／スクリプト未検出時はプロセスを起動せず
                                                メッセージのみ報告する。コンストラクターは既定
                                                （WdV3TimmPaths.RootDirectory を使用）と、テスト用に
                                                対象フォルダを差し替え可能な
                                                WdV3TimmBuildService(string rootDirectory) の2種を
                                                public で公開する（本プロジェクトは internal コンストラクター
                                                + InternalsVisibleTo の境界パターンを採用していないため）
    ViewModels/Pages/
      MainPageViewModel.cs                  <- ディレクトリ一括タグ付け実行ページの VM。ConfigPath・
                                                TaggerBackend から TaggerRunnerFactory 経由で ITaggerRunner
                                                （Wd14TaggerRunner または WdV3TimmTaggerRunner、フェーズ32で
                                                Wd14TaggerRunner 固定から変更）を読み込み、ICaptioningService
                                                経由で ProcessDirectoryAsync/GenerateReportAsync を実行する。
                                                TryLoadRunnerAsync（旧 TryLoadRunner を async 化）は
                                                既存 Runner が IAsyncDisposable（WdV3TimmTaggerRunner）の場合
                                                再構築前に破棄する。RunAsync の finally では
                                                SaveResultLogAsync 後に TryLoadRunnerAsync(showErrorSnackbar:
                                                false) を呼び、実行完了のたびに Runner を破棄・再構築する
                                                （WdV3Timm 利用時に常駐サーバープロセスがページ再訪なしに
                                                リークするのを防ぐため。ComfyUI 利用時は実質的な影響はない）。
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
                                                ITaggerRunner には依存しないが、一括タグ操作の
                                                AutoSuggestBox 候補一覧 TagList（フェーズ17で追加）の取得
                                                （TagReportGenerator 経由で tags_report.txt を生成・解析）
                                                にのみこれらに依存する（INavigationAware.OnNavigatedToAsync
                                                で ConfigPath・TaggerBackend から TaggerRunnerFactory 経由で
                                                ITaggerRunner を読み込む（フェーズ32、Wd14TaggerRunner 固定
                                                から変更）。失敗しても TagList 更新を静かにスキップする
                                                のみでエラー表示はしない）。
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
      ReportViewModel.cs                    <- タグ集計レポート表示ページの VM。ConfigPath・TaggerBackend から
                                                TaggerRunnerFactory 経由で ITaggerRunner を読み込み
                                                （フェーズ32、Wd14TaggerRunner 固定から変更）、対象ディレクトリを選択して
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
                                                ResultsFolder の選択（BrowseResultsFolderCommand）に加え、
                                                フェーズ32でタグ付けバックエンド選択（SelectedTaggerBackend/
                                                TaggerBackendList、ThemeList/SelectedTheme と同じパターン。
                                                変更は即座に Config.Data.TaggerBackend へ反映）を実装。
                                                フェーズ32の追加修正で wdv3-timm 実行環境のビルド機能を
                                                追加した。コンストラクターに ISnackbarService（必須、
                                                ビルド完了時の成功/失敗スナックバー表示用）と
                                                IWdV3TimmBuildService?（既定 new WdV3TimmBuildService()）を
                                                追加。IsWdV3TimmExeReady（bool）・WdV3TimmStatusText
                                                （IsWdV3TimmExeReady に応じて Settings_WdV3TimmReady/
                                                Settings_WdV3TimmNotReady を返す計算プロパティ）・
                                                IsBuildingWdV3Timm・WdV3TimmBuildLogEntries
                                                （ObservableCollection&lt;string&gt;）を新設。
                                                OnNavigatedToAsync は（テーマ/言語/バックエンド初期化は
                                                初回訪問時のみだったのに対し）毎回 IsWdV3TimmExeReady を
                                                再チェックする。BuildWdV3TimmCommand（CanExecute:
                                                !IsBuildingWdV3Timm）は MainPageViewModel.SynchronousProgress&lt;T&gt;
                                                と同じ考え方の同期 IProgress&lt;string&gt; 実装（本クラス内に
                                                private nested class として複製）でビルド出力を
                                                WdV3TimmBuildLogEntries へ即時反映し、完了後に
                                                IsWdV3TimmExeReady を再取得してスナックバー表示・
                                                IsBuildingWdV3Timm を false に戻す
      ConfigViewModel.cs                    <- captioning_config.json 編集ページの VM。ConfigPath が指す
                                                ファイルを System.Text.Json で直接読み書きする（ComfyUI との
                                                通信は行わないため ICaptioningService 経由のファクトリー境界は
                                                不要）。フェーズ32で wdv3_timm セクションの編集項目
                                                （WdV3TimmExePath・BrowseWdV3TimmExePathCommand）を一時追加
                                                したが、フェーズ32の追加修正（wdv3_timm.exe の実行ファイルパスを
                                                アプリ同階層固定にする方針転換）でこれらを完全に削除した。
                                                wdv3-timm はモデル名・しきい値を独自に持たず wd14_tagger
                                                （ModelName/GeneralThreshold/CharacterThreshold）を共用する
                                                （ComfyUILibs.Services.WdV3TimmModelMap で wd14_tagger.model_name
                                                を変換する設計。当初は WdV3TimmModel 等を独自フィールドとして
                                                持たせていたが、ComfyUI 版と設定がずれる問題があったため
                                                フェーズ32内で設計修正した）。wdv3_timm セクション自体が
                                                廃止されたため、Save() の出力対象は comfyui_url/wd14_tagger
                                                のみになった（wd14_tagger セクションは ModelName（ComfyUiUrl
                                                ではない）非空の場合に出力する）。Save() は
                                                Config.Data.TaggerBackend が指す方の必須検証のみ行う
                                                （ComfyUI なら ValidateWd14TaggerConfig、WdV3Timm なら
                                                ValidateWdV3TimmConfig。後者は内部で ValidateWd14TaggerConfig
                                                も呼ぶため wd14_tagger セクションが実質必須になる。
                                                exe_path 関連の検証は廃止済み）。ModelName は自由入力の
                                                ui:TextBox だと WdV3TimmModelMap で変換できない値を入力
                                                できてしまうため、選択肢を
                                                ComfyUILibs.Services.WdV3TimmModelMap.SupportedWd14ModelNames
                                                （5モデル、二重管理を避けるため ComfyUILibs 側の対応表を
                                                そのまま参照する）とする ModelNameList を追加し、
                                                ConfigPage.xaml 側を ComboBox 化した
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
                                                しきい値・prepend/exclude タグ既定値の編集、保存ボタン）。
                                                フェーズ32で「WD14 モデル設定」カードに実行ファイルパス
                                                選択欄を持つ「wdv3-timm 設定（ローカルプロセス版）」カードを
                                                追加したが、フェーズ32の追加修正（実行ファイルパスをアプリ
                                                同階層固定にする方針転換）でこのカード自体を削除した。
                                                代わりに「WD14 モデル設定」カードの4行目（Grid.Row=3・
                                                ColumnSpan=2）に、モデル名・しきい値は wdv3-timm 版でも
                                                共用する旨の案内文（Config_WdV3TimmSharesWd14Notice、
                                                wording はフェーズ32の追加修正で更新）のみを残した
      SettingsPage.xaml(.cs)                <- 設定画面（テーマ・言語切り替え、captioning_config.json パス選択、
                                                実行結果ログ出力先 ResultsFolder のフォルダ選択、フェーズ32で
                                                タグ付けバックエンド選択カード（ThemeList/SelectedTheme と
                                                同じ ComboBox パターン、アイコン ArrowSwap24）を追加）。
                                                ラベルは LocalizationManager バインディング。フェーズ32の
                                                追加修正で「wdv3-timm 実行環境（ローカルプロセス版）」カード
                                                （Settings_WdV3TimmSectionLabel、アイコン DeviceEq24）を
                                                「ComfyUI 接続」カードの前に追加した。準備状態表示
                                                （ViewModel.WdV3TimmStatusText）・ビルド中は
                                                ui:ProgressRing を表示する「ビルド」ボタン
                                                （ViewModel.BuildWdV3TimmCommand）・説明文
                                                （Settings_WdV3TimmBuildDescription）・実行ログ
                                                （ViewModel.WdV3TimmBuildLogEntries、DataPage.xaml の
                                                1 ファイルごとの処理結果ログと同じ見た目）で構成する。
                                                フェーズ35で、実行ログ表示を ui:ListView（内部テンプレートが
                                                独自の ScrollViewer を持つ）から、明示的な
                                                ScrollViewer（x:Name="WdV3TimmLogScrollViewer"）+
                                                ItemsControl の組み合わせへ変更した。ui:ListView が
                                                入れ子の ScrollViewer としてマウスホイールを常に自身で
                                                消費してしまい、ページ全体を包む外側の ScrollViewer まで
                                                イベントがバブルせずページ全体のスクロールが機能しなく
                                                なる不具合があったため（GalleryPage.xaml の
                                                TagsScrollViewer と同じ問題）、対応する
                                                WdV3TimmLogScrollViewer_PreviewMouseWheel
                                                （SettingsPage.xaml.cs、GalleryPage.xaml.cs の
                                                TagsScrollViewer_PreviewMouseWheel と同一ロジック。内側が
                                                スクロール端に達している場合のみイベントを親要素へ手動
                                                転送する）を追加して解決した。フェーズ37で、ページ最上位に
                                                独自に持っていた &lt;ScrollViewer&gt;（他ページと同じ
                                                &lt;ScrollViewer&gt;&lt;StackPanel&gt; 構成）を完全に削除し、
                                                直下の &lt;StackPanel&gt; を Page の直接のコンテンツにした
                                                （Page ルート要素に ScrollViewer.CanContentScroll="False"
                                                添付プロパティを明示指定）。Wpf.Ui.Controls.NavigationView が
                                                ホスト中の Page 自体を包む組み込みの ScrollViewer を持ち
                                                （NavigationViewContentPresenter が
                                                ScrollViewer.CanContentScrollProperty の既定値を Page 型に
                                                対して True へ上書きし、それを自身の組み込み ScrollViewer に
                                                反映する。lepoco/wpfui#1041 として報告されている未解決の
                                                既知の挙動）、SettingsPage 自身が独自の ScrollViewer を
                                                持つと NavigationView 組み込みの ScrollViewer（外側、実際に
                                                スクロールバーが表示される方）の内側にもう1つ ScrollViewer
                                                が入れ子になり、内側が常にマウスホイールを消費してしまい
                                                外側まで届かなくなっていた（dotnet/wpf#8353 と同種の
                                                既知の WPF の挙動。「スクロールバーのドラッグは効くが
                                                マウスホイールだけ効かない」という報告と一致）ため。
                                                他ページ（MainPage/DataPage/GalleryPage/ReportPage/
                                                ConfigPage）も同じ「最上位に独自の ScrollViewer を持つ」
                                                構成のため理論上同じ問題を抱えている可能性があるが、
                                                報告があったのは SettingsPage のみのため他ページは
                                                未対応のまま
    Views/Windows/
      MainWindow.xaml(.cs)                  <- ナビゲーションホスト
    Usings.cs
  ComfyUICaptioningToolTests/                <- xUnit テストプロジェクト（ComfyUIRunWorkflowTests を参考に新設）
    Fakes/
      FakeSnackbarService.cs                <- ISnackbarService のテスト用スタブ（Show 呼び出し履歴を記録）
      FakeCaptioningService.cs              <- ICaptioningService のテスト用スタブ（進捗・結果・例外発生を
                                                あらかじめ設定可能。ProcessDirectoryAsync/GenerateReportAsync
                                                それぞれ個別に例外を発生させられる）
      FakeWdV3TimmBuildService.cs           <- IWdV3TimmBuildService のテスト用スタブ（フェーズ32の追加修正で
                                                新設。IsExeReady/BuildResult/OutputLinesToReport を設定可能。
                                                BuildAsyncCallCount・BuildAsyncCalledWithCancelledToken で
                                                呼び出し履歴を記録する）
    Helpers/
      LocalizationManagerTests.cs           <- LocalizationManager のカルチャ切替・フォールバック挙動のテスト
      TagInCollectionConverterTests.cs      <- TagInCollectionConverter のテスト（フェーズ22で新設。
                                                values null/要素数不正・tagList 空/null・tag 空文字・
                                                含まれる/含まれない・大文字小文字無視・ConvertBack 未実装を検証）
      ObjectEqualsConverterTests.cs         <- ObjectEqualsConverter のテスト（フェーズ23で新設。
                                                values null/要素数不正・同一インスタンス/異なるインスタンス・
                                                両方 null/片方のみ null・ConvertBack 未実装を検証）
    Models/
      AppConfigTests.cs                     <- AppConfig/WindowSettingData のデフォルト値・PropertyChanged のテスト。
                                                フェーズ32で TaggerBackend の既定値（ComfyUI）・PropertyChanged
                                                のテストを追加
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
      TaggerRunnerFactoryTests.cs            <- TaggerRunnerFactory のテスト（フェーズ32で新設。
                                                TaggerBackend.ComfyUI/WdV3Timm それぞれで正しい実装型
                                                （Wd14TaggerRunner/WdV3TimmTaggerRunner）が返ること、
                                                該当セクション欠落時に ComfyUIException を送出することを検証。
                                                フェーズ32の追加修正で wdv3_timm セクションが廃止されたため、
                                                WdV3Timm バックエンドのテストは wd14_tagger セクションのみの
                                                config ファイルで検証するよう更新した）
      WdV3TimmBuildServiceTests.cs          <- WdV3TimmBuildService のテスト（フェーズ32の追加修正で新設。
                                                隔離した一時フォルダに軽量なダミー .bat スクリプトを配置し、
                                                実際の cmd.exe プロセス起動・標準出力ストリーミング・
                                                終了コード判定・setup.bat→build_exe.bat の逐次実行
                                                〔前者が終了コード0以外の場合は後者を実行しない〕・
                                                ルートフォルダ/スクリプト未検出時の挙動・IsExeReady を検証。
                                                フェイクではなく実サービスクラスをそのまま対象にできるのは
                                                WdV3TimmBuildService(string rootDirectory) コンストラクター
                                                があるため）。フェーズ34で
                                                BuildAsync_ProgressReportedOnCallingStaThread_
                                                DoesNotThrowCrossThreadCollectionException を追加。
                                                RecordingProgress&lt;T&gt;（呼び出し元スレッドを問わず単に
                                                記録するだけ）ではクロススレッド問題を検出できないため、
                                                TestSupport.StaTestRunner 上で
                                                CollectionViewSource.GetDefaultView により
                                                ObservableCollection&lt;string&gt; に対する CollectionView
                                                （SettingsPage.xaml の ListBox バインディングが実行時に
                                                生成するのと同種のオブジェクト）を生成し、
                                                SynchronousProgress&lt;T&gt; と同じ同期委譲パターンの
                                                IProgress&lt;T&gt; 実装からそのコレクションへ直接 Add する
                                                ことで、修正前のコードでは実際に
                                                System.NotSupportedException が再現することを確認した
                                                回帰テスト
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
                                                RunOnSta（TestSupport.StaTestRunner に委譲）でラップ。
                                                フェーズ32で、TaggerBackend.WdV3Timm 選択時に
                                                wd14_tagger セクションの読み込み成否で IsConfigLoaded が
                                                切り替わること・RunCommand が captioningServiceFactory へ
                                                WdV3TimmTaggerRunner 型のインスタンスを渡すこと・
                                                実行完了後に Runner が破棄・再構築されページ再訪なしに
                                                連続実行できることを検証するテストを追加（フェーズ32の
                                                追加修正で wdv3_timm セクション自体が廃止されたため、
                                                WriteWdV3TimmConfigFile ヘルパーは wd14_tagger セクションのみ
                                                書き出すよう更新した）
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
      SettingsViewModelTests.cs             <- SettingsViewModel のテスト（テーマ・言語切り替え等）。
                                                フェーズ32で TaggerBackendList・SelectedTaggerBackend
                                                （Config への反映・PropertyChanged・OnNavigatedToAsync
                                                での読み込み）のテストを追加。フェーズ32の追加修正で
                                                コンストラクターに ISnackbarService（必須）・
                                                IWdV3TimmBuildService? が追加されたことに伴い、
                                                CreateVm ヘルパー（FakeSnackbarService・
                                                FakeWdV3TimmBuildService を注入）と RunOnSta ヘルパー
                                                （StaTestRunner に委譲）を新設し、既存の直接
                                                new SettingsViewModel(...) 呼び出し箇所をすべて
                                                CreateVm() 経由に置き換えた。IsWdV3TimmExeReady・
                                                WdV3TimmStatusText・BuildWdV3TimmCommand（CanExecute・
                                                実行時のビルドサービス呼び出し・ログ追記・実行前の
                                                ログクリア・IsBuildingWdV3Timm のトグル・成功時の
                                                IsWdV3TimmExeReady 再取得と成功スナックバー・失敗時の
                                                危険スナックバー）・OnNavigatedToAsync が毎回
                                                IsWdV3TimmExeReady を再チェックすることを検証する
                                                テストを追加
      ConfigViewModelTests.cs               <- ConfigViewModel のテスト（ConfigPath 読み込み成否・
                                                ファイル未存在時の新規作成扱い・SaveCommand の CanExecute/実行・
                                                保存前バリデーション・タグ既定値の union なしの単純反映）。
                                                フェーズ32で wdv3-timm フィールドの読み込み・既定値、
                                                TaggerBackend ごとの Save 挙動のテストを一時追加したが、
                                                フェーズ32の追加修正で ExePath 関連のテストをすべて削除し、
                                                TaggerBackend.WdV3Timm 選択時の Save 挙動を「wd14_tagger を
                                                共用する」観点で検証し直した（ModelName 欠落/未対応モデル名/
                                                しきい値範囲外時のエラー表示・ComfyUiUrl なしで保存できること）
    ViewModels/Windows/
      MainWindowViewModelTests.cs           <- MainWindowViewModel のテスト（メニュー項目構築・ウィンドウクローズ時保存等）
```

## 現時点で存在しないもの（ComfyUIRunWorkflow との差分）

- `doc/` ディレクトリ（使い方ドキュメント・クラス図など）
- `templates/` ディレクトリ（WD14 Tagger 用ワークフローテンプレートは `ComfyUILibs` 側の `template_wd14_tagger.json` を利用する想定だが、本プロジェクト側への配置は未着手。実行時は `captioning_config.json` と同様、実行ファイルと同階層の `templates/` に配置する必要がある）
