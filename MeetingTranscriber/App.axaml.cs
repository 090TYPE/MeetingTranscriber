using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using MeetingTranscriber.App.ViewModels;
using MeetingTranscriber.App.Views;
using MeetingTranscriber.Core;
using MeetingTranscriber.Core.Audio;
using MeetingTranscriber.Core.Storage;
using MeetingTranscriber.Core.Summary;
using MeetingTranscriber.Core.Transcription;

namespace MeetingTranscriber.App;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settingsSvc = new SettingsService();
            var settings = settingsSvc.Load();

            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MeetingTranscriber", "sessions.db");
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            var dbOpts = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={dbPath}").Options;
            var db = new AppDbContext(dbOpts);
            var repo = new SessionRepository(db);
            _ = repo.EnsureCreatedAsync();

            var modelPath = WhisperService.ModelPath(settings.WhisperModel);
            var whisper = new WhisperService(modelPath, settings.Language);

            ISummaryProvider summary = settings.AiProvider switch
            {
                "claude" => new ClaudeProvider(settings.ClaudeApiKey),
                "openai" => new OpenAIProvider(settings.OpenAiApiKey),
                _ => new NullSummaryProvider()
            };

            var mic = new MicRecorder(settings.ChunkSeconds);

            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel(mic, whisper, summary, repo, settings, settingsSvc)
            };
            desktop.MainWindow = mainWindow;

            if (!File.Exists(modelPath))
            {
                mainWindow.Opened += (_, _) =>
                {
                    var dlWindow = new DownloadModelWindow();
                    dlWindow.Show(mainWindow);
                    _ = dlWindow.DownloadAsync(settings.WhisperModel);
                };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
