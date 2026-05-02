using System.Collections.ObjectModel;
using System.ComponentModel;
using CopyTrail.Data.Repositories;
using CopyTrail.Models;
using CopyTrail.Services;

namespace CopyTrail.ViewModels;

public enum FilterKind { All, Text, Links, Code, Images, Files, Colors, Pinned }

public sealed class PopupViewModel : INotifyPropertyChanged
{
    private readonly ClipboardRepository? _repository;
    private readonly CleanupService? _cleanupService;
    private readonly AppSettings _settings;
    private List<ClipCardViewModel> _allCards = [];
    private string _searchText = "";
    private FilterKind _selectedFilter = FilterKind.All;
    private bool _isLoading;
    private bool _isEmpty;
    private int _selectedIndex = -1;

    public ObservableCollection<ClipCardViewModel> Cards { get; } = [];

    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged(nameof(SearchText));
            OnPropertyChanged(nameof(HasSearchText));
            OnPropertyChanged(nameof(IsSearchActive));
            ApplyFilterAndSearch();
        }
    }

    public FilterKind SelectedFilter
    {
        get => _selectedFilter;
        private set
        {
            _selectedFilter = value;
            OnPropertyChanged(nameof(SelectedFilter));
            OnPropertyChanged(nameof(IsSearchActive));
            ApplyFilterAndSearch();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); }
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        private set { _isEmpty = value; OnPropertyChanged(nameof(IsEmpty)); }
    }

    public bool HasSearchText => !string.IsNullOrWhiteSpace(_searchText);
    public bool IsSearchActive => HasSearchText || _selectedFilter != FilterKind.All;
    public bool IsHistoryEmpty => IsEmpty && !IsSearchActive;
    public bool IsFilteredEmpty => IsEmpty && IsSearchActive;
    public int ResultCount => Cards.Count;

    public IEnumerable<ClipCardViewModel> PinnedCards => Cards.Where(c => c.IsPinned);
    public IEnumerable<ClipCardViewModel> RegularCards => Cards.Where(c => !c.IsPinned);
    public bool HasPinnedCards => Cards.Any(c => c.IsPinned);
    public bool HasRegularCards => Cards.Any(c => !c.IsPinned);

    public int SelectedIndex
    {
        get => _selectedIndex;
        private set { _selectedIndex = value; OnPropertyChanged(nameof(SelectedIndex)); OnPropertyChanged(nameof(SelectedCard)); }
    }

    public ClipCardViewModel? SelectedCard =>
        _selectedIndex >= 0 && _selectedIndex < Cards.Count ? Cards[_selectedIndex] : null;

    public PopupViewModel(ClipboardRepository? repository, AppSettings settings, CleanupService? cleanupService = null)
    {
        _repository = repository;
        _settings = settings;
        _cleanupService = cleanupService;
    }

    public async Task LoadAsync()
    {
        if (_repository is null)
        {
            IsLoading = false;
            IsEmpty = true;
            return;
        }

        IsLoading = true;
        IsEmpty = false;

        try
        {
            var items = await _repository.GetRecentTimelineItemsAsync(_settings.MaxHistoryCount)
                .ConfigureAwait(true); // continue on UI thread

            _allCards = items
                .Select(ClipCardViewModel.FromRecord)
                .ToList();

            ApplyFilterAndSearch();
        }
        catch
        {
            _allCards = [];
            Cards.Clear();
            IsEmpty = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void SelectNext()
    {
        if (Cards.Count == 0) return;
        int next = _selectedIndex + 1;
        if (next >= Cards.Count) next = Cards.Count - 1;
        SetSelected(next);
    }

    public void SelectPrevious()
    {
        if (Cards.Count == 0) return;
        int prev = _selectedIndex - 1;
        if (prev < 0) prev = 0;
        SetSelected(prev);
    }

    public void SetSelectedCard(ClipCardViewModel vm)
    {
        var idx = Cards.IndexOf(vm);
        if (idx >= 0) SetSelected(idx);
    }

    private void SetSelected(int index)
    {
        if (_selectedIndex >= 0 && _selectedIndex < Cards.Count)
            Cards[_selectedIndex].IsSelected = false;

        SelectedIndex = index;

        if (_selectedIndex >= 0 && _selectedIndex < Cards.Count)
            Cards[_selectedIndex].IsSelected = true;
    }

    public void SelectFilter(FilterKind kind)
    {
        SelectedFilter = kind;
    }

    public async Task TogglePinAsync(ClipCardViewModel vm)
    {
        if (_repository is null || vm.Content is null) return;

        bool nowPinned = !vm.IsPinned;
        vm.IsPinned = nowPinned;
        vm.Content.IsPinned = nowPinned;

        NotifyPinnedSectionChanged();

        if (nowPinned)
            await _repository.PinContentAsync(vm.Content.Id).ConfigureAwait(true);
        else
            await _repository.UnpinContentAsync(vm.Content.Id).ConfigureAwait(true);
    }

    public async Task DeleteCardAsync(ClipCardViewModel vm)
    {
        if (_cleanupService is null || vm.Content is null) return;

        await _cleanupService.DeleteItemAsync(vm.Content.Id, vm.Content.ImagePath, vm.Content.ThumbnailPath)
            .ConfigureAwait(true);

        _allCards.Remove(vm);
        Cards.Remove(vm);

        IsEmpty = Cards.Count == 0 && !IsLoading;
        OnPropertyChanged(nameof(ResultCount));
        OnPropertyChanged(nameof(IsHistoryEmpty));
        OnPropertyChanged(nameof(IsFilteredEmpty));
        NotifyPinnedSectionChanged();
    }

    private void NotifyPinnedSectionChanged()
    {
        OnPropertyChanged(nameof(PinnedCards));
        OnPropertyChanged(nameof(RegularCards));
        OnPropertyChanged(nameof(HasPinnedCards));
        OnPropertyChanged(nameof(HasRegularCards));
    }

    private void ApplyFilterAndSearch()
    {
        IEnumerable<ClipCardViewModel> filtered = _allCards;

        if (_selectedFilter != FilterKind.All)
            filtered = filtered.Where(c => MatchesFilter(c, _selectedFilter));

        if (!string.IsNullOrWhiteSpace(_searchText))
            filtered = filtered.Where(c => MatchesSearch(c, _searchText));

        // Deselect old before clearing
        if (_selectedIndex >= 0 && _selectedIndex < Cards.Count)
            Cards[_selectedIndex].IsSelected = false;

        Cards.Clear();
        foreach (var card in filtered)
            Cards.Add(card);

        // Reset selection; no card is pre-selected after a filter/search change
        SelectedIndex = -1;
        IsEmpty = Cards.Count == 0 && !IsLoading;
        OnPropertyChanged(nameof(ResultCount));
        OnPropertyChanged(nameof(IsHistoryEmpty));
        OnPropertyChanged(nameof(IsFilteredEmpty));
        NotifyPinnedSectionChanged();
    }

    private static bool MatchesFilter(ClipCardViewModel card, FilterKind filter) => filter switch
    {
        FilterKind.Text => card.Kind is ClipboardItemKind.Text or ClipboardItemKind.RichText
                                     or ClipboardItemKind.Html or ClipboardItemKind.Markdown
                                     or ClipboardItemKind.Svg or ClipboardItemKind.WordContent
                                     or ClipboardItemKind.PdfText,
        FilterKind.Links => card.Kind == ClipboardItemKind.Url,
        FilterKind.Code => card.Kind is ClipboardItemKind.Code or ClipboardItemKind.Json
                                      or ClipboardItemKind.TerminalCommand,
        FilterKind.Images => card.Kind is ClipboardItemKind.Image or ClipboardItemKind.Screenshot,
        FilterKind.Files => card.Kind == ClipboardItemKind.FileReference,
        FilterKind.Colors => card.Kind == ClipboardItemKind.ColorValue,
        FilterKind.Pinned => card.IsPinned,
        _ => true
    };

    private static bool MatchesSearch(ClipCardViewModel card, string query)
    {
        var q = query.Trim();
        if (q.Length == 0) return true;

        return card.Preview.Contains(q, StringComparison.OrdinalIgnoreCase) ||
               card.Source.AppName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
               (card.SourceWindowTitle?.Contains(q, StringComparison.OrdinalIgnoreCase) == true) ||
               card.ContentKind.Contains(q, StringComparison.OrdinalIgnoreCase) ||
               (card.Content?.PlainText?.Contains(q, StringComparison.OrdinalIgnoreCase) == true) ||
               (card.Content?.Url?.Contains(q, StringComparison.OrdinalIgnoreCase) == true);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
