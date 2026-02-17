using DevKill.Models;
using Xunit;

namespace DevKill.Tests;

public class PortEntryTests
{
    private static PortEntry MakeEntry(
        int port = 3000,
        int pid = 1234,
        string processName = "node",
        string protocol = "TCP",
        bool isDevProcess = false) => new()
    {
        Port = port,
        Pid = pid,
        ProcessName = processName,
        ProcessPath = "",
        Protocol = protocol,
        LocalAddress = "0.0.0.0",
        State = protocol == "TCP" ? "LISTEN" : "",
        IsDevProcess = isDevProcess,
    };

    [Fact]
    public void GroupName_IsDevProcess_ReturnsDevServers()
    {
        var entry = MakeEntry(isDevProcess: true);
        Assert.Equal("Dev Servers", entry.GroupName);
    }

    [Fact]
    public void GroupName_NotDevProcess_ReturnsOtherPorts()
    {
        var entry = MakeEntry(isDevProcess: false);
        Assert.Equal("Other Ports", entry.GroupName);
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        var a = MakeEntry(port: 3000, pid: 100, processName: "node", protocol: "TCP", isDevProcess: true);
        var b = MakeEntry(port: 3000, pid: 100, processName: "node", protocol: "TCP", isDevProcess: true);
        Assert.Equal(a, b);
    }

    [Fact]
    public void RecordEquality_DifferentPort_AreNotEqual()
    {
        var a = MakeEntry(port: 3000);
        var b = MakeEntry(port: 3001);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void RecordEquality_DifferentPid_AreNotEqual()
    {
        var a = MakeEntry(pid: 100);
        var b = MakeEntry(pid: 200);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void RequiredProperties_AreSet()
    {
        var entry = MakeEntry(port: 8080, pid: 42, processName: "dotnet", protocol: "UDP");
        Assert.Equal(8080, entry.Port);
        Assert.Equal(42, entry.Pid);
        Assert.Equal("dotnet", entry.ProcessName);
        Assert.Equal("UDP", entry.Protocol);
        Assert.Equal("0.0.0.0", entry.LocalAddress);
        Assert.Equal("", entry.State);
    }
}
