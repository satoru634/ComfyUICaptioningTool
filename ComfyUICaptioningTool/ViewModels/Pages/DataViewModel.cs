using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using ComfyUICaptioningTool.Helpers;
using ComfyUICaptioningTool.Models;
using ComfyUILibs.Common;
using Wpf.Ui.Abstractions.Controls;

namespace ComfyUICaptioningTool.ViewModels.Pages
{
    /// <summary>
    /// DataPage の ViewModel。<see cref="AppConfig.ResultsFolder"/> 配下の
    /// captioning_result_*.json（<see cref="MainPageViewModel"/> が実行のたびに出力する結果ログ）を
    /// 新しい順に読み込んで一覧表示する。
    /// </summary>
    public partial class DataViewModel : ObservableObject, INavigationAware
    {
        /// <summary>captioning_result_*.json 読み込み時のオプション。プロパティ名の大文字/小文字を区別しない。</summary>
        private static readonly JsonSerializerOptions JsonReadOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        /// <summary>アプリケーション設定。</summary>
        public Setting<AppConfig> Config { get; }

        /// <summary>読み込んだ実行結果ログの一覧（新しい順）。</summary>
        [ObservableProperty]
        private ObservableCollection<CaptioningResultLogPreview> _results = new();

        /// <summary>状態メッセージ（結果フォルダ未設定・フォルダ未存在・結果なしのいずれか。空文字ならメッセージなし）。</summary>
        [ObservableProperty]
        private string _statusMessage = "";

        /// <summary>読み込み中かどうか。</summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>DI コンテナから設定を受け取って初期化する。</summary>
        public DataViewModel(Setting<AppConfig> config)
        {
            Config = config;
        }

        // ── INavigationAware ─────────────────────────────────────────────────

        /// <summary>ページへ遷移するたびに Results フォルダを再スキャンする。</summary>
        public Task OnNavigatedToAsync() => LoadResultsAsync();

        /// <summary>ページから離れるときは何もしない。</summary>
        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        /// <summary>結果フォルダを再スキャンして一覧を更新する。</summary>
        [RelayCommand]
        private Task RefreshAsync() => LoadResultsAsync();

        /// <summary>
        /// <see cref="AppConfig.ResultsFolder"/> 配下の captioning_result_*.json を新しい順（ファイル名の降順）に
        /// 読み込んで <see cref="Results"/> を更新する。フォルダが未設定・未存在の場合はエラーメッセージのみ表示する。
        /// </summary>
        private async Task LoadResultsAsync()
        {
            var folder = Config.Data.ResultsFolder;
            Results = new ObservableCollection<CaptioningResultLogPreview>();
            StatusMessage = "";

            if (string.IsNullOrWhiteSpace(folder))
            {
                StatusMessage = LocalizationManager.Instance["Data_ResultsFolderNotSet"];
                return;
            }

            if (!Directory.Exists(folder))
            {
                StatusMessage = string.Format(LocalizationManager.Instance["Data_FolderNotFound_Format"], folder);
                return;
            }

            IsLoading = true;

            try
            {
                var files = await Task.Run(() =>
                    Directory.GetFiles(folder, "captioning_result_*.json")
                        .OrderByDescending(f => f)
                        .ToArray());

                var loaded = new ObservableCollection<CaptioningResultLogPreview>();
                foreach (var file in files)
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(file);
                        var log = JsonSerializer.Deserialize<CaptioningResultLog>(json, JsonReadOptions);
                        if (log != null)
                            loaded.Add(CreatePreview(log));
                    }
                    catch
                    {
                        // 読み込めないファイルはスキップする
                    }
                }

                Results = loaded;

                if (loaded.Count == 0)
                    StatusMessage = LocalizationManager.Instance["Data_NoResults"];
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>読み込んだログから、一覧表示用の整形済み文字列を含むプレビューを構築する。</summary>
        private static CaptioningResultLogPreview CreatePreview(CaptioningResultLog log)
        {
            var timestampText = string.Format(LocalizationManager.Instance["Data_LastRunTimestampFormat"], log.Timestamp);
            var summaryText = log.Status == "success"
                ? string.Format(LocalizationManager.Instance["Main_SummaryFormat"], log.Processed, log.Skipped, log.Errors)
                : log.Error ?? "";

            return new CaptioningResultLogPreview(log, timestampText, summaryText);
        }
    }
}
