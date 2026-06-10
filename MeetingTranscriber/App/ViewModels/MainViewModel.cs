using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using System.Reactive.Linq;
using ReactiveUI;
using MeetingTranscriber.Core;
using MeetingTranscriber.Core.Audio;
using MeetingTranscriber.Core.Transcription;
using MeetingTranscriber.Core.Summary;
using MeetingTranscriber.Core.Storage;
using MeetingTranscriber.Core.Export;
using MeetingTranscriber.Models;

namespace MeetingTranscriber.App.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly MicRecorder _mic;
    private readonly WhisperService _whisper;
    private readonly ISummaryProvider _summary;
    private readonly SessionRepository _repo;
    private readonly AppSettings _settings;

    private bool _isRecording;
    private bool _isFileMode;
    private string _statusText = "READY";
    private string _timer = "00:00:00";
    private string _summaryText = string.Empty;
    private float[] _waveformSamples = Array.Empty<float>();
    private string _errorMessage = string.Empty;
    private bool _hasError;
    private string _activeTab = "record";
    private int _currentSessionId;
    private int _elapsedSec;
    private System.Timers.Timer? _clock;

    public ObservableCollection<TranscriptLine> TranscriptLines { get; } = new();

    public bool IsRecording { get => _isRecording; private set => this.RaiseAndSetIfChanged(ref _isRecording, value); }
    public bool IsFileMode { get => _isFileMode; set => this.RaiseAndSetIfChanged(ref _isFileMode, value); }
    public string StatusText { get => _statusText; private set => this.RaiseAndSetIfChanged(ref _statusText, value); }
    public string Timer { get => _timer; private set => this.RaiseAndSetIfChanged(ref _timer, value); }
    public string SummaryText { get => _summaryText; private set => this.RaiseAndSetIfChanged(ref _summaryText, value); }
    public float[] WaveformSamples { get => _waveformSamples; private set => this.RaiseAndSetIfChanged(ref _waveformSamples, value); }
    public string ErrorMessage { get => _errorMessage; private set => this.RaiseAndSetIfChanged(ref _errorMessage, value); }
    public bool HasError { get => _hasError; private set => this.RaiseAndSetIfChanged(ref _hasError, value); }

    public string ActiveTab
    {
        get => _activeTab;
        private set
        {
            this.RaiseAndSetIfChanged(ref _activeTab, value);
            this.RaisePropertyChanged(nameof(IsRecordTab));
            this.RaisePropertyChanged(nameof(IsHistoryTab));
            this.RaisePropertyChanged(nameof(IsSettingsTab));
        }
    }
    public bool IsRecordTab => _activeTab == "record";
    public bool IsHistoryTab => _activeTab == "history";
    public bool IsSettingsTab => _activeTab == "settings";

    public HistoryViewModel HistoryVM { get; }
    public SettingsViewModel SettingsVM { get; }

    public ICommand SwitchToRecordCommand { get; }
    public ICommand SwitchToHistoryCommand { get; }
    public ICommand SwitchToSettingsCommand { get; }

    public ICommand StartRecordingCommand { get; }
    public ICommand StopRecordingCommand { get; }
    public ICommand OpenFileCommand { get; }
    public ICommand SummarizeCommand { get; }
    public ICommand ExportTxtCommand { get; }
    public ICommand ExportPdfCommand { get; }
    public ICommand ExportSrtCommand { get; }
    public ICommand DismissErrorCommand { get; }

    public MainViewModel(MicRecorder mic, WhisperService whisper, ISummaryProvider summary,
        SessionRepository repo, AppSettings settings, SettingsService settingsSvc)
    {
        _mic = mic;
        _whisper = whisper;
        _summary = summary;
        _repo = repo;
        _settings = settings;

        HistoryVM = new HistoryViewModel(repo);
        SettingsVM = new SettingsViewModel(settingsSvc);

        _mic.WaveformSamples += samples =>
            Avalonia.Threading.Dispatcher.UIThread.Post(() => WaveformSamples = samples);

        _mic.ChunkReady += async chunk => await ProcessChunkAsync(chunk);

        var notRecording = this.WhenAnyValue(x => x.IsRecording, rec => !rec)
            .ObserveOn(RxApp.MainThreadScheduler);
        var isRecording = this.WhenAnyValue(x => x.IsRecording)
            .ObserveOn(RxApp.MainThreadScheduler);

        StartRecordingCommand = ReactiveCommand.CreateFromTask(StartRecordingAsync, notRecording);
        StopRecordingCommand = ReactiveCommand.CreateFromTask(StopRecordingAsync, isRecording);
        OpenFileCommand = ReactiveCommand.CreateFromTask(OpenFileAsync, notRecording);
        SummarizeCommand = ReactiveCommand.CreateFromTask(SummarizeAsync, notRecording);
        ExportTxtCommand = ReactiveCommand.CreateFromTask(ExportTxtAsync);
        ExportPdfCommand = ReactiveCommand.CreateFromTask(ExportPdfAsync);
        ExportSrtCommand = ReactiveCommand.CreateFromTask(ExportSrtAsync);
        DismissErrorCommand = ReactiveCommand.Create(() => { HasError = false; ErrorMessage = string.Empty; });

        SwitchToRecordCommand = ReactiveCommand.Create(() => ActiveTab = "record");
        SwitchToHistoryCommand = ReactiveCommand.Create(() =>
        {
            ActiveTab = "history";
            _ = HistoryVM.LoadSessionsAsync();
        });
        SwitchToSettingsCommand = ReactiveCommand.Create(() => ActiveTab = "settings");
    }

    private async Task StartRecordingAsync()
    {
        if (!System.IO.File.Exists(WhisperService.ModelPath(_settings.WhisperModel)))
        {
            ErrorMessage = $"Whisper model '{_settings.WhisperModel}' not found. Restart app to download.";
            HasError = true;
            return;
        }

        var session = await _repo.CreateSessionAsync(
            $"Meeting {DateTime.Now:yyyy-MM-dd HH:mm}", _settings.Language);
        _currentSessionId = session.Id;
        TranscriptLines.Clear();
        SummaryText = string.Empty;
        _elapsedSec = 0;
        _clock = new System.Timers.Timer(1000);
        _clock.Elapsed += (_, _) =>
        {
            _elapsedSec++;
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                Timer = TimeSpan.FromSeconds(_elapsedSec).ToString(@"hh\:mm\:ss"));
        };
        _clock.Start();
        try
        {
            _mic.Start();
            IsRecording = true;
            StatusText = "● REC";
        }
        catch (Exception ex)
        {
            _clock?.Stop();
            ErrorMessage = $"Mic error: {ex.Message}";
            HasError = true;
            StatusText = "READY";
        }
    }

    private async Task StopRecordingAsync()
    {
        _mic.StopAndFlush();
        _clock?.Stop();
        IsRecording = false;
        StatusText = "PROCESSING...";
        WaveformSamples = Array.Empty<float>();
        var wordCount = TranscriptLines.Sum(l => l.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
        await _repo.FinalizeSessionAsync(_currentSessionId, _elapsedSec, wordCount);
        StatusText = "READY";
    }

    private async Task ProcessChunkAsync(AudioChunk chunk)
    {
        try
        {
            var line = await _whisper.ProcessChunkAsync(chunk);
            if (string.IsNullOrWhiteSpace(line.Text)) return;
            line.SessionId = _currentSessionId;
            await _repo.AppendLineAsync(_currentSessionId, line.TimestampSec, line.Text);
            Avalonia.Threading.Dispatcher.UIThread.Post(() => TranscriptLines.Add(line));
        }
        catch (Exception ex)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                ErrorMessage = ex.Message.Length > 120 ? ex.Message[..120] + "…" : ex.Message;
                HasError = true;
                StatusText = "ERROR";
            });
        }
    }

    private async Task OpenFileAsync()
    {
        if (!System.IO.File.Exists(WhisperService.ModelPath(_settings.WhisperModel)))
        {
            ErrorMessage = $"Whisper model '{_settings.WhisperModel}' not found. Restart app to download.";
            HasError = true;
            return;
        }

        var topLevel = GetTopLevel();
        if (topLevel is null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Open audio file",
                AllowMultiple = false,
                FileTypeFilter = [new("Audio") { Patterns = ["*.mp3", "*.wav", "*.mp4", "*.m4a"] }]
            });
        if (files.Count == 0) return;

        var session = await _repo.CreateSessionAsync(
            Path.GetFileNameWithoutExtension(files[0].Name), _settings.Language);
        _currentSessionId = session.Id;
        TranscriptLines.Clear();
        StatusText = "PROCESSING FILE...";
        IsRecording = true;

        await Task.Run(async () =>
        {
            foreach (var chunk in FileAudioReader.ReadChunks(files[0].Path.LocalPath, _settings.ChunkSeconds))
                await ProcessChunkAsync(chunk);
        });

        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsRecording = false;
            StatusText = "READY";
        });
    }

    private async Task SummarizeAsync()
    {
        SummaryText = string.Empty;
        var transcript = string.Join("\n", TranscriptLines.Select(l =>
            $"[{TimeSpan.FromSeconds(l.TimestampSec):mm\\:ss}] {l.Text}"));
        await foreach (var token in _summary.SummarizeAsync(transcript))
            SummaryText += token;
    }

    private async Task ExportTxtAsync()
    {
        var path = await PickSavePathAsync("txt");
        if (path is null) return;
        await TxtExporter.ExportAsync(TranscriptLines, $"Meeting {DateTime.Now:yyyy-MM-dd}", path);
    }

    private async Task ExportPdfAsync()
    {
        var path = await PickSavePathAsync("pdf");
        if (path is null) return;
        await PdfExporter.ExportAsync(TranscriptLines, $"Meeting {DateTime.Now:yyyy-MM-dd}", path);
    }

    private async Task ExportSrtAsync()
    {
        var path = await PickSavePathAsync("srt");
        if (path is null) return;
        await SrtExporter.ExportAsync(TranscriptLines, path);
    }

    private static async Task<string?> PickSavePathAsync(string ext)
    {
        var topLevel = GetTopLevel();
        if (topLevel is null) return null;
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                DefaultExtension = ext,
                SuggestedFileName = $"transcript_{DateTime.Now:yyyyMMdd_HHmm}"
            });
        return file?.Path.LocalPath;
    }

    private static TopLevel? GetTopLevel()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            return TopLevel.GetTopLevel(lifetime.MainWindow);
        return null;
    }
}
