using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using MeetingTranscriber.Core.Storage;
using MeetingTranscriber.Models;

namespace MeetingTranscriber.App.ViewModels;

public class HistoryViewModel : ViewModelBase
{
    private readonly SessionRepository _repo;
    private string _searchQuery = string.Empty;
    private Session? _selectedSession;

    public ObservableCollection<Session> Sessions { get; } = new();
    public ObservableCollection<TranscriptLine> SelectedLines { get; } = new();

    public string SearchQuery
    {
        get => _searchQuery;
        set { this.RaiseAndSetIfChanged(ref _searchQuery, value); _ = LoadSessionsAsync(); }
    }

    public Session? SelectedSession
    {
        get => _selectedSession;
        set { this.RaiseAndSetIfChanged(ref _selectedSession, value); _ = LoadLinesAsync(); }
    }

    public ICommand DeleteCommand { get; }
    public ICommand RefreshCommand { get; }

    public HistoryViewModel(SessionRepository repo)
    {
        _repo = repo;
        DeleteCommand = ReactiveCommand.CreateFromTask<Session>(DeleteAsync);
        RefreshCommand = ReactiveCommand.CreateFromTask(LoadSessionsAsync);
        _ = LoadSessionsAsync();
    }

    public async Task LoadSessionsAsync()
    {
        var list = await _repo.SearchSessionsAsync(_searchQuery);
        Sessions.Clear();
        foreach (var s in list) Sessions.Add(s);
    }

    private async Task LoadLinesAsync()
    {
        SelectedLines.Clear();
        if (_selectedSession is null) return;
        var lines = await _repo.GetLinesAsync(_selectedSession.Id);
        foreach (var l in lines) SelectedLines.Add(l);
    }

    private async Task DeleteAsync(Session s)
    {
        await _repo.DeleteSessionAsync(s.Id);
        await LoadSessionsAsync();
    }
}
