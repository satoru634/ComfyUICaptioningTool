using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using ComfyUICaptioningTool.Helpers;
using ComfyUICaptioningTool.Models;
using ComfyUILibs.Common;
using ComfyUILibs.Exceptions;
using ComfyUILibs.Models;
using ComfyUILibs.Services;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace ComfyUICaptioningTool.ViewModels.Pages
{
    /// <summary>
    /// captioning_config.json 編集ページ（ConfigPage）の ViewModel。
    /// <see cref="AppConfig.ConfigPath"/> が指すファイルを読み込んでフォームに反映し、
    /// 保存時にファイルへ書き戻す。ComfyUI との通信は行わない（ファイル I/O のみ）。
    /// </summary>
    public partial class ConfigViewModel : ObservableObject, INavigationAware
    {
        /// <summary>デシリアライズ時のオプション。プロパティ名の大文字/小文字を区別しない。</summary>
        private static readonly JsonSerializerOptions ReadOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        /// <summary>
        /// シリアライズ時のオプション。null のプロパティ（default_workflow/workflows 等、
        /// 本ページで扱わないフィールド）を出力しないことで、既存の captioning_config.json の
        /// フォーマット（comfyui_url/wd14_tagger/prepend_tags/exclude_tags のみ）を維持する。
        /// </summary>
        private static readonly JsonSerializerOptions WriteOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        /// <summary>アプリケーション設定。</summary>
        public Setting<AppConfig> Config { get; }

        /// <summary>スナックバー通知サービス。</summary>
        private readonly ISnackbarService _snackbarService;

        /// <summary>ConfigPath の読み込みに成功し、編集・保存が可能な状態かどうか。</summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private bool _isConfigLoaded;

        /// <summary>ComfyUI サーバーの接続先 URL。</summary>
        [ObservableProperty]
        private string _comfyUiUrl = "";

        /// <summary>WD Timm Tagger のモデル名。</summary>
        [ObservableProperty]
        private string _modelName = "";

        /// <summary>
        /// モデル名選択コンボボックスに表示する選択肢。<see cref="ComfyUILibs.Services.WdV3TimmModelMap"/> の
        /// 対応表と一致させる（wdv3-timm バックエンド利用時に model_name から --model 値へ変換できる必要があるため）。
        /// </summary>
        public IReadOnlyCollection<string> ModelNameList { get; } = WdV3TimmModelMap.SupportedWd14ModelNames;

        /// <summary>一般タグを出力するしきい値（0.0〜1.0）。</summary>
        [ObservableProperty]
        private double _generalThreshold = 0.35;

        /// <summary>キャラクタータグを出力するしきい値（0.0〜1.0）。</summary>
        [ObservableProperty]
        private double _characterThreshold = 0.85;

        /// <summary>全画像に共通で先頭追加するタグ（カンマ区切り）。</summary>
        [ObservableProperty]
        private string _prependTagsText = "";

        /// <summary>全画像に共通で除外するタグ（カンマ区切り）。</summary>
        [ObservableProperty]
        private string _excludeTagsText = "";

        /// <summary>読み込み・保存に関する補足メッセージ（例: 新規ファイル作成の案内）。未設定時は空文字。</summary>
        [ObservableProperty]
        private string _statusText = "";

        /// <summary>DI コンテナから設定・スナックバーサービスを受け取って初期化する。</summary>
        public ConfigViewModel(Setting<AppConfig> config, ISnackbarService snackbarService)
        {
            Config = config;
            _snackbarService = snackbarService;
        }

        // ── INavigationAware ─────────────────────────────────────────────────

        /// <summary>ページへ遷移するたびに captioning_config.json を再読み込みする。</summary>
        public Task OnNavigatedToAsync()
        {
            LoadFromFile();
            return Task.CompletedTask;
        }

        /// <summary>ページから離れるときは何もしない（保存は明示的な保存ボタンでのみ行う）。</summary>
        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        // ── 読み込み ─────────────────────────────────────────────────────────

        /// <summary>
        /// Config.Data.ConfigPath から captioning_config.json を読み込み、フォームへ反映する。
        /// ファイルが存在しない場合は既定値で新規作成する前提として扱う（エラーにしない）。
        /// JSON が不正な場合は編集・保存を無効化する。
        /// </summary>
        private void LoadFromFile()
        {
            StatusText = "";
            var path = Config.Data.ConfigPath;

            if (string.IsNullOrWhiteSpace(path))
            {
                IsConfigLoaded = false;
                ShowDanger(LocalizationManager.Instance["Common_ConfigPathNotSet"]);
                return;
            }

            if (!File.Exists(path))
            {
                ApplyToFields(new WorkflowConfig());
                IsConfigLoaded = true;
                StatusText = LocalizationManager.Instance["Config_NewFileNotice"];
                return;
            }

            try
            {
                var json = File.ReadAllText(path, Encoding.UTF8);
                var config = JsonSerializer.Deserialize<WorkflowConfig>(json, ReadOptions) ?? new WorkflowConfig();
                ApplyToFields(config);
                IsConfigLoaded = true;
            }
            catch (JsonException ex)
            {
                IsConfigLoaded = false;
                ShowDanger(string.Format(LocalizationManager.Instance["Config_LoadErrorFormat"], ex.Message));
            }
        }

        /// <summary>読み込んだ設定値をフォームの各プロパティへ反映する。</summary>
        private void ApplyToFields(WorkflowConfig config)
        {
            ComfyUiUrl = config.ComfyuiUrl ?? "";
            ModelName = config.Wd14Tagger?.ModelName ?? "";
            GeneralThreshold = config.Wd14Tagger?.GeneralThreshold ?? 0.35;
            CharacterThreshold = config.Wd14Tagger?.CharacterThreshold ?? 0.85;
            PrependTagsText = string.Join(", ", config.PrependTags ?? new List<string>());
            ExcludeTagsText = string.Join(", ", config.ExcludeTags ?? new List<string>());
        }

        // ── 保存 ────────────────────────────────────────────────────────────

        private bool CanSave() => IsConfigLoaded;

        /// <summary>
        /// フォームの内容を検証し、Config.Data.ConfigPath へ captioning_config.json として書き込む。
        /// wdv3-timm はモデル名・しきい値・実行ファイルパスを独自に持たず、実行ファイルはアプリと同階層の
        /// wdv3-timm フォルダに固定配置し（<see cref="ComfyUILibs.Services.WdV3TimmPaths"/> 参照、
        /// SettingsPage の「ビルド」ボタンで用意する）、モデル名・しきい値は wd14_tagger
        /// （ModelName/GeneralThreshold/CharacterThreshold）を共用するため、本ページで編集する
        /// captioning_config.json には wd14_tagger セクションのみを持つ。
        /// 現在選択中のバックエンド（<see cref="Config"/>.Data.TaggerBackend）が ComfyUI の場合は
        /// ComfyUiUrl も必須として検証する（wdv3-timm の場合は ComfyUiUrl 不要）。
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSave))]
        private void Save()
        {
            var backend = Config.Data.TaggerBackend;

            if (backend == TaggerBackend.ComfyUI && string.IsNullOrWhiteSpace(ComfyUiUrl))
            {
                ShowDanger(LocalizationManager.Instance["Config_ComfyUiUrlEmptyError"]);
                return;
            }

            var config = new WorkflowConfig
            {
                ComfyuiUrl = string.IsNullOrWhiteSpace(ComfyUiUrl) ? null : ComfyUiUrl,
                Wd14Tagger = string.IsNullOrWhiteSpace(ModelName)
                    ? null
                    : new Wd14TaggerConfig
                    {
                        ModelName = ModelName,
                        GeneralThreshold = GeneralThreshold,
                        CharacterThreshold = CharacterThreshold,
                    },
                PrependTags = SplitTags(PrependTagsText),
                ExcludeTags = SplitTags(ExcludeTagsText),
            };

            try
            {
                if (backend == TaggerBackend.WdV3Timm)
                    ConfigLoader.ValidateWdV3TimmConfig(config);
                else
                    ConfigLoader.ValidateWd14TaggerConfig(config);

                var path = Config.Data.ConfigPath;
                var json = JsonSerializer.Serialize(config, WriteOptions);
                File.WriteAllText(path, json, Encoding.UTF8);

                StatusText = "";
                ShowSuccess(string.Format(LocalizationManager.Instance["Config_SaveSuccessFormat"], path));
            }
            catch (ComfyUIException ex)
            {
                ShowDanger(ex.Message);
            }
        }

        /// <summary>カンマ区切りタグ文字列を trim・空要素除去したリストに分割する。</summary>
        private static List<string> SplitTags(string text)
            => text.Split(',')
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .ToList();

        private void ShowDanger(string message) => _snackbarService.Show(
            LocalizationManager.Instance["Common_Error"],
            message,
            ControlAppearance.Danger,
            new SymbolIcon(SymbolRegular.ErrorCircle24),
            TimeSpan.FromSeconds(5.0));

        private void ShowSuccess(string message) => _snackbarService.Show(
            LocalizationManager.Instance["Common_Completed"],
            message,
            ControlAppearance.Success,
            new SymbolIcon(SymbolRegular.CheckmarkCircle24),
            TimeSpan.FromSeconds(4.0));
    }
}
