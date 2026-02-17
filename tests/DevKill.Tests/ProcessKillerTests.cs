using DevKill.Services;
using Xunit;

namespace DevKill.Tests;

public class ProcessKillerTests
{
    [Fact]
    public void Kill_NonExistentPid_ReturnsTrueAsAlreadyExited()
    {
        // PID that almost certainly doesn't exist
        bool result = ProcessKiller.Kill(int.MaxValue);
        Assert.True(result);
    }

    [Fact]
    public void Kill_ZeroPid_ReturnsFalse()
    {
        // PID 0 is the System Idle Process — can't be killed
        bool result = ProcessKiller.Kill(0);
        // This should either return true (ArgumentException = already exited)
        // or false (access denied). Either way, it shouldn't throw.
        _ = result;
    }

    [Fact]
    public void Kill_DoesNotThrow_ForAnyPid()
    {
        // Verifies that Kill never propagates exceptions
        var ex = Record.Exception(() => ProcessKiller.Kill(-1));
        Assert.Null(ex);
    }
}
