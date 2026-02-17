using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevKill.Models;
using DevKill.Services;

namespace DevKill.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IPortScanner _portScanner;
    private readonly IProcessKiller _processKiller;
    private readonly DispatcherTimer _refreshTimer;
    private HashSet<(int Port, int Pid, string Protocol, string LocalAddress)> _lastSnapshot = [];
    private bool _isRefreshing;

    public ObservableCollection<PortEntryViewModel> Entries { get; } = [];

    [ObservableProperty]
    private string _filterText = "";

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _devCount;

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private bool _autoStartEnabled;

    public ICollectionView EntriesView { get; }

    public MainWindowViewModel(IPortScanner portScanner, IProcessKiller processKiller)
    {
        _portScanner = portScanner;
        _processKiller = processKiller;

        EntriesView = CollectionViewSource.GetDefaultView(Entries);
        EntriesView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(PortEntryViewModel.GroupName)));
        EntriesView.Filter = FilterEntries;

        // Sort dev servers first, then by port
        EntriesView.SortDescriptions.Add(new SortDescription(nameof(PortEntryViewModel.GroupName), ListSortDirection.Ascending));
        EntriesView.SortDescriptions.Add(new SortDescription(nameof(PortEntryViewModel.Port), ListSortDirection.Ascending));

        _autoStartEnabled = Helpers.StartupManager.IsEnabled;

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3),
        };
        _refreshTimer.Tick += async (_, _) =>
        {
            try { await RefreshAsync(); }
            catch { /* swallow — timer should not crash the app */ }
        };
    }

    partial void OnFilterTextChanged(string value)
    {
        EntriesView.Refresh();
    }

    private bool FilterEntries(object obj)
    {
        if (string.IsNullOrWhiteSpace(FilterText))
            return true;

        if (obj is not PortEntryViewModel vm)
            return false;

        var filter = FilterText.Trim();
        return vm.Port.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase)
            || vm.ProcessName.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || vm.Pid.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase)
            || vm.ProcessPath.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || vm.Protocol.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (_isRefreshing) return;
        _isRefreshing = true;
        try
        {
            var entries = await Task.Run(_portScanner.Scan);

            var newSnapshot = entries
                .Select(e => (e.Port, e.Pid, e.Protocol, e.LocalAddress))
                .ToHashSet();

            if (newSnapshot.SetEquals(_lastSnapshot))
                return;

            _lastSnapshot = newSnapshot;

            // In-place diff to avoid flicker: remove stale, add new, keep existing
            var incomingKeys = newSnapshot;
            var currentKeys = Entries
                .Select(vm => (vm.Port, vm.Pid, vm.Protocol, vm.LocalAddress))
                .ToHashSet();

            // Remove entries no longer present
            for (int i = Entries.Count - 1; i >= 0; i--)
            {
                var vm = Entries[i];
                var key = (vm.Port, vm.Pid, vm.Protocol, vm.LocalAddress);
                if (!incomingKeys.Contains(key))
                    Entries.RemoveAt(i);
            }

            // Add entries that are new
            foreach (var entry in entries)
            {
                var key = (entry.Port, entry.Pid, entry.Protocol, entry.LocalAddress);
                if (!currentKeys.Contains(key))
                {
                    var vm = new PortEntryViewModel(entry, _processKiller);
                    vm.KillRequested += (_, _) => _ = RefreshAsync();
                    Entries.Add(vm);
                }
            }

            TotalCount = entries.Count;
            DevCount = entries.Count(e => e.IsDevProcess);
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    [RelayCommand]
    private void BulkKill(IList<object>? selectedItems)
    {
        if (selectedItems is null || selectedItems.Count == 0)
            return;

        var targets = selectedItems.OfType<PortEntryViewModel>().ToList();
        foreach (var vm in targets)
        {
            _processKiller.Kill(vm.Pid);
        }

        _ = RefreshAsync();
    }

    [RelayCommand]
    private void ToggleAutoStart()
    {
        Helpers.StartupManager.Toggle();
        AutoStartEnabled = Helpers.StartupManager.IsEnabled;
    }

    public void StartAutoRefresh() => _refreshTimer.Start();
    public void StopAutoRefresh() => _refreshTimer.Stop();
}
