using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using DevKill.Models;
using DevKill.Services;
using DevKill.ViewModels;
using Wpf.Ui.Controls;

namespace DevKill.Views;

public partial class MainWindow : FluentWindow
{
    private readonly MainWindowViewModel _vm;
    private readonly IProcessKiller _processKiller;
    private bool _isExiting;

    public MainWindow()
    {
        var portScanner = new PortScanner();
        _processKiller = new ProcessKiller();
        _vm = new MainWindowViewModel(portScanner, _processKiller);
        DataContext = _vm;
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        _vm.KillFailureNotification += OnKillFailure;
    }

    public ICommand FocusSearchCommand => new RelayCommand(() => SearchBox.Focus());

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _vm.RefreshAsync();
        _vm.StartAutoRefresh();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isExiting)
        {
            // Minimize to tray instead of closing
            e.Cancel = true;
            Hide();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        switch (e.Key)
        {
            case Key.Delete:
                KillSelected();
                e.Handled = true;
                break;

            case Key.Escape:
                Hide();
                e.Handled = true;
                break;

            case Key.A when Keyboard.Modifiers == ModifierKeys.Control:
                PortGrid.SelectAll();
                e.Handled = true;
                break;
        }
    }

    private void KillSelected()
    {
        var selected = PortGrid.SelectedItems.Cast<PortEntryViewModel>().ToList();
        if (selected.Count == 0) return;

        var names = string.Join(", ", selected.Select(s => $"{s.ProcessName}:{s.Port}"));
        var result = System.Windows.MessageBox.Show(
            $"Kill {selected.Count} process(es)?\n\n{names}",
            "DevKill",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        var failures = new List<string>();
        foreach (var vm in selected)
        {
            if (!_processKiller.Kill(vm.Pid))
                failures.Add($"{vm.ProcessName}:{vm.Port} (PID {vm.Pid})");
        }

        if (failures.Count > 0)
            ShowKillFailureSnackbar($"Failed to kill: {string.Join(", ", failures)}");

        _ = _vm.RefreshAsync();
    }

    private void PortGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var count = PortGrid.SelectedItems.Count;
        _vm.SelectedCount = count;
        BulkKillButton.Visibility = count > 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BulkKill_Click(object sender, RoutedEventArgs e)
    {
        KillSelected();
    }

    private void PortGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Manually stretch the Path column to fill remaining space
        // (star columns don't work reliably with grouped DataGrid)
        const double fixedColumnsWidth = 80 + 85 + 80 + 150 + 70; // Port + Protocol + PID + Process + Kill
        var available = PortGrid.ActualWidth - fixedColumnsWidth - SystemParameters.VerticalScrollBarWidth - 8;
        if (available > 100)
            PathColumn.Width = new DataGridLength(available);
    }

    // Tray icon handlers
    private void TrayIcon_LeftDoubleClick(object sender, RoutedEventArgs e)
    {
        ShowAndActivate();
    }

    private void TrayShowWindow_Click(object sender, RoutedEventArgs e)
    {
        ShowAndActivate();
    }

    private void TrayExit_Click(object sender, RoutedEventArgs e)
    {
        _isExiting = true;
        TrayIcon.Dispose();
        Application.Current.Shutdown();
    }

    private void TrayContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        // Build dynamic tray menu with current dev server entries
        TrayContextMenu.Items.Clear();

        var devEntries = _vm.Entries
            .Where(x => x.GroupName == PortEntry.DevServersGroup)
            .Take(10)
            .ToList();

        if (devEntries.Count > 0)
        {
            foreach (var entry in devEntries)
            {
                var item = new System.Windows.Controls.MenuItem
                {
                    Header = $"Kill {entry.ProcessName}:{entry.Port} (PID {entry.Pid})",
                };
                var pid = entry.Pid;
                var procName = entry.ProcessName;
                var port = entry.Port;
                item.Click += (_, _) =>
                {
                    if (!_processKiller.Kill(pid))
                        ShowKillFailureSnackbar($"Failed to kill {procName}:{port} (PID {pid})");
                    _ = _vm.RefreshAsync();
                };
                TrayContextMenu.Items.Add(item);
            }

            TrayContextMenu.Items.Add(new Separator());
        }

        var showItem = new System.Windows.Controls.MenuItem
        {
            Header = "Show DevKill",
            FontWeight = FontWeights.Bold,
        };
        showItem.Click += TrayShowWindow_Click;
        TrayContextMenu.Items.Add(showItem);

        var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
        exitItem.Click += TrayExit_Click;
        TrayContextMenu.Items.Add(exitItem);
    }

    private void OnKillFailure(string message)
    {
        ShowKillFailureSnackbar(message);
    }

    private void ShowKillFailureSnackbar(string message)
    {
        var snackbar = new Snackbar(SnackbarPresenter)
        {
            Title = "Kill Failed",
            Content = message,
            Appearance = Wpf.Ui.Controls.ControlAppearance.Danger,
            Timeout = TimeSpan.FromSeconds(5),
        };
        snackbar.Show();
    }

    private void ShowAndActivate()
    {
        Show();
        ShowInTaskbar = true;
        WindowState = WindowState.Normal;
        Activate();
    }
}
