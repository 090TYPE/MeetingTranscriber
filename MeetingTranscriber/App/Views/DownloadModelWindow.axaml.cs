using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using MeetingTranscriber.Core.Transcription;

namespace MeetingTranscriber.App.Views;

public partial class DownloadModelWindow : Window
{
    public DownloadModelWindow() => InitializeComponent();

    public async Task DownloadAsync(string modelName)
    {
        var progress = new Progress<double>(pct =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                Progress.Value = pct * 100;
                PercentText.Text = $"{pct * 100:F0}%";
                StatusText.Text = $"Downloading ggml-{modelName}.bin...";
            });
        });

        try
        {
            await WhisperService.DownloadModelAsync(modelName, progress);
            Close();
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() =>
            {
                StatusText.Text = $"Download failed: {ex.Message}";
                PercentText.Text = "Error — check internet connection and restart app.";
                Progress.Value = 0;
            });
        }
    }
}
