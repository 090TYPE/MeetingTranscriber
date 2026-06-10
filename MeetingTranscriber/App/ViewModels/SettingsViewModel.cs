using System.Windows.Input;
using ReactiveUI;
using MeetingTranscriber.Core;
using MeetingTranscriber.Models;

namespace MeetingTranscriber.App.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _svc;
    private AppSettings _settings;

    public string WhisperModel
    {
        get => _settings.WhisperModel;
        set { _settings.WhisperModel = value; this.RaisePropertyChanged(); }
    }
    public string Language
    {
        get => _settings.Language;
        set { _settings.Language = value; this.RaisePropertyChanged(); }
    }
    public string AiProvider
    {
        get => _settings.AiProvider;
        set { _settings.AiProvider = value; this.RaisePropertyChanged(); }
    }
    public string ClaudeApiKey
    {
        get => _settings.ClaudeApiKey;
        set { _settings.ClaudeApiKey = value; this.RaisePropertyChanged(); }
    }
    public string OpenAiApiKey
    {
        get => _settings.OpenAiApiKey;
        set { _settings.OpenAiApiKey = value; this.RaisePropertyChanged(); }
    }
    public int ChunkSeconds
    {
        get => _settings.ChunkSeconds;
        set { _settings.ChunkSeconds = value; this.RaisePropertyChanged(); }
    }

    public string[] WhisperModels => AppSettings.WhisperModels;
    public string[] Languages => AppSettings.Languages;
    public string[] AiProviders => ["disabled", "claude", "openai"];

    public ICommand SaveCommand { get; }

    public SettingsViewModel(SettingsService svc)
    {
        _svc = svc;
        _settings = svc.Load();
        SaveCommand = ReactiveCommand.Create(() => _svc.Save(_settings));
    }
}
