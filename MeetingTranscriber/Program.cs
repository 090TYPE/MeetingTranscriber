using Avalonia;
using Avalonia.ReactiveUI;
using System;

namespace MeetingTranscriber;

class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<MeetingTranscriber.App.App>()
            .UsePlatformDetect()
            .UseReactiveUI()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
