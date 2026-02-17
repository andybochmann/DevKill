using DevKill.Models;
using DevKill.Services;
using DevKill.ViewModels;
using Xunit;

namespace DevKill.Tests;

public class PortEntryViewModelTests
{
    private class StubKiller : IProcessKiller
    {
        public int LastPid { get; private set; } = -1;
        public bool KillResult { get; set; } = true;
        public bool Kill(int pid) { LastPid = pid; return KillResult; }
    }

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
        var vm = new PortEntryViewModel(entry, new StubKiller());

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
        var vm = new PortEntryViewModel(entry, new StubKiller());
        Assert.Same(entry, vm.Entry);
    }

    [Fact]
    public void GroupName_FollowsEntryGroupName()
    {
        var dev = new PortEntryViewModel(MakeEntry(isDevProcess: true), new StubKiller());
        Assert.Equal("Dev Servers", dev.GroupName);

        var other = new PortEntryViewModel(MakeEntry(isDevProcess: false), new StubKiller());
        Assert.Equal("Other Ports", other.GroupName);
    }

    [Fact]
    public void KillCommand_IsNotNull()
    {
        var vm = new PortEntryViewModel(MakeEntry(), new StubKiller());
        Assert.NotNull(vm.KillCommand);
    }

    [Fact]
    public void KillCommand_RaisesKillRequestedEvent()
    {
        var entry = MakeEntry(pid: int.MaxValue);
        var killer = new StubKiller();
        var vm = new PortEntryViewModel(entry, killer);

        bool eventRaised = false;
        vm.KillRequested += (_, _) => eventRaised = true;

        vm.KillCommand.Execute(null);

        Assert.True(eventRaised);
        Assert.Equal(int.MaxValue, killer.LastPid);
    }
}
