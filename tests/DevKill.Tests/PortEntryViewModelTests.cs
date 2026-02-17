using DevKill.Models;
using DevKill.ViewModels;
using Xunit;

namespace DevKill.Tests;

public class PortEntryViewModelTests
{
    private static PortEntry MakeEntry(
        int port = 3000,
        int pid = 1234,
        string processName = "node",
        string protocol = "TCP",
        bool isDevProcess = true) => new()
    {
        Port = port,
        Pid = pid,
        ProcessName = processName,
        ProcessPath = @"C:\Program Files\nodejs\node.exe",
        Protocol = protocol,
        LocalAddress = "0.0.0.0",
        State = "LISTEN",
        IsDevProcess = isDevProcess,
    };

    [Fact]
    public void Properties_DelegateToEntry()
    {
        var entry = MakeEntry(port: 8080, pid: 42, processName: "dotnet", protocol: "UDP");
        var vm = new PortEntryViewModel(entry);

        Assert.Equal(8080, vm.Port);
        Assert.Equal(42, vm.Pid);
        Assert.Equal("dotnet", vm.ProcessName);
        Assert.Equal("UDP", vm.Protocol);
        Assert.Equal("0.0.0.0", vm.LocalAddress);
        Assert.Equal("LISTEN", vm.State);
        Assert.True(vm.IsDevProcess);
        Assert.Equal("Dev Servers", vm.GroupName);
    }

    [Fact]
    public void Entry_PropertyExposesUnderlyingRecord()
    {
        var entry = MakeEntry();
        var vm = new PortEntryViewModel(entry);
        Assert.Same(entry, vm.Entry);
    }

    [Fact]
    public void GroupName_FollowsEntryGroupName()
    {
        var dev = new PortEntryViewModel(MakeEntry(isDevProcess: true));
        Assert.Equal("Dev Servers", dev.GroupName);

        var other = new PortEntryViewModel(MakeEntry(isDevProcess: false));
        Assert.Equal("Other Ports", other.GroupName);
    }

    [Fact]
    public void KillCommand_IsNotNull()
    {
        var vm = new PortEntryViewModel(MakeEntry());
        Assert.NotNull(vm.KillCommand);
    }

    [Fact]
    public void KillCommand_RaisesKillRequestedEvent()
    {
        // Use a PID that doesn't exist so Kill() won't actually kill anything real
        var entry = MakeEntry(pid: int.MaxValue);
        var vm = new PortEntryViewModel(entry);

        bool eventRaised = false;
        vm.KillRequested += (_, _) => eventRaised = true;

        vm.KillCommand.Execute(null);

        Assert.True(eventRaised);
    }
}
