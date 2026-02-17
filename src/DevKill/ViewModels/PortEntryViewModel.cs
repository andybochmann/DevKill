using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevKill.Models;
using DevKill.Services;

namespace DevKill.ViewModels;

public partial class PortEntryViewModel : ObservableObject
{
    private readonly IProcessKiller _processKiller;

    public PortEntry Entry { get; }

    public int Port => Entry.Port;
    public int Pid => Entry.Pid;
    public string ProcessName => Entry.ProcessName;
    public string ProcessPath => Entry.ProcessPath;
    public string Protocol => Entry.Protocol;
    public string LocalAddress => Entry.LocalAddress;
    public string State => Entry.State;
    public bool IsDevProcess => Entry.IsDevProcess;
    public string GroupName => Entry.GroupName;

    public PortEntryViewModel(PortEntry entry, IProcessKiller processKiller)
    {
        Entry = entry;
        _processKiller = processKiller;
    }

    [RelayCommand]
    private void Kill()
    {
        _processKiller.Kill(Pid);
        KillRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? KillRequested;
}
