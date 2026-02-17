using DevKill.Services;
using Xunit;

namespace DevKill.Tests;

public class PortScannerTests
{
    // --- IsDevProcessName tests ---

    [Theory]
    [InlineData("node")]
    [InlineData("dotnet")]
    [InlineData("php")]
    [InlineData("iisexpress")]
    [InlineData("python")]
    [InlineData("python3")]
    [InlineData("ruby")]
    [InlineData("java")]
    [InlineData("deno")]
    [InlineData("bun")]
    [InlineData("uvicorn")]
    [InlineData("gunicorn")]
    [InlineData("nginx")]
    [InlineData("httpd")]
    [InlineData("apache")]
    [InlineData("hugo")]
    [InlineData("caddy")]
    [InlineData("vite")]
    public void IsDevProcessName_KnownDevProcess_ReturnsTrue(string name)
    {
        Assert.True(PortScanner.IsDevProcessName(name));
    }

    [Theory]
    [InlineData("Node")]
    [InlineData("NODE")]
    [InlineData("Dotnet")]
    [InlineData("PYTHON")]
    [InlineData("IISExpress")]
    public void IsDevProcessName_CaseInsensitive_ReturnsTrue(string name)
    {
        Assert.True(PortScanner.IsDevProcessName(name));
    }

    [Theory]
    [InlineData("node.exe")]
    [InlineData("dotnet.exe")]
    [InlineData("python.EXE")]
    [InlineData("JAVA.Exe")]
    public void IsDevProcessName_WithExeExtension_ReturnsTrue(string name)
    {
        Assert.True(PortScanner.IsDevProcessName(name));
    }

    [Theory]
    [InlineData("chrome")]
    [InlineData("explorer")]
    [InlineData("svchost")]
    [InlineData("notepad")]
    [InlineData("System")]
    [InlineData("devenv")]
    [InlineData("sqlservr")]
    public void IsDevProcessName_NonDevProcess_ReturnsFalse(string name)
    {
        Assert.False(PortScanner.IsDevProcessName(name));
    }

    [Fact]
    public void IsDevProcessName_Empty_ReturnsFalse()
    {
        Assert.False(PortScanner.IsDevProcessName(""));
    }

    [Fact]
    public void IsDevProcessName_Null_ReturnsFalse()
    {
        Assert.False(PortScanner.IsDevProcessName(null!));
    }

    [Fact]
    public void IsDevProcessName_ExeSuffix_OnlyStripsExe()
    {
        // "node.exe" → "node" → true
        Assert.True(PortScanner.IsDevProcessName("node.exe"));

        // "node.com" should not match (no .exe stripping)
        Assert.False(PortScanner.IsDevProcessName("node.com"));

        // "nodemon" should not match — it's not in the set
        Assert.False(PortScanner.IsDevProcessName("nodemon"));
    }

    // --- NetworkToHostPort tests ---

    [Fact]
    public void NetworkToHostPort_Port80_ConvertsCorrectly()
    {
        // Port 80 in network byte order: 0x0050 → stored as little-endian int with 80 << 8 = 20480
        int networkOrder = 80 << 8; // 0x5000 as int
        int result = PortScanner.NetworkToHostPort(networkOrder);
        Assert.Equal(80, result);
    }

    [Fact]
    public void NetworkToHostPort_Port443_ConvertsCorrectly()
    {
        // 443 = 0x01BB → network byte order in struct: 0xBB01
        int networkOrder = (0xBB << 8) | 0x01;
        int result = PortScanner.NetworkToHostPort(networkOrder);
        Assert.Equal(443, result);
    }

    [Fact]
    public void NetworkToHostPort_Port3000_ConvertsCorrectly()
    {
        // 3000 = 0x0BB8 → network byte order: 0xB80B
        int networkOrder = (0xB8 << 8) | 0x0B;
        int result = PortScanner.NetworkToHostPort(networkOrder);
        Assert.Equal(3000, result);
    }

    [Fact]
    public void NetworkToHostPort_Port8080_ConvertsCorrectly()
    {
        // 8080 = 0x1F90 → network byte order: 0x901F
        int networkOrder = (0x90 << 8) | 0x1F;
        int result = PortScanner.NetworkToHostPort(networkOrder);
        Assert.Equal(8080, result);
    }

    [Fact]
    public void NetworkToHostPort_HighPort49152_ConvertsCorrectly()
    {
        // 49152 = 0xC000 → network byte order in struct: 0x00C0
        int networkOrder = (0x00 << 8) | 0xC0;
        int result = PortScanner.NetworkToHostPort(networkOrder);
        Assert.Equal(49152, result);
    }

    [Fact]
    public void NetworkToHostPort_Port65535_ConvertsCorrectly()
    {
        // 65535 = 0xFFFF → network byte order in struct: 0xFFFF
        int networkOrder = (0xFF << 8) | 0xFF;
        int result = PortScanner.NetworkToHostPort(networkOrder);
        Assert.Equal(65535, result);
    }

    // --- Integration: Scan() actually runs ---

    [Fact]
    public void Scan_ReturnsNonNullList()
    {
        var result = PortScanner.Scan();
        Assert.NotNull(result);
    }

    [Fact]
    public void Scan_ReturnsEntries_OnActiveSystem()
    {
        // On any Windows system, there should be at least some listening ports
        var result = PortScanner.Scan();
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Scan_AllEntriesHaveValidPort()
    {
        var result = PortScanner.Scan();
        foreach (var entry in result)
        {
            Assert.InRange(entry.Port, 0, 65535);
        }
    }

    [Fact]
    public void Scan_AllEntriesHaveValidProtocol()
    {
        var result = PortScanner.Scan();
        foreach (var entry in result)
        {
            Assert.Contains(entry.Protocol, new[] { "TCP", "UDP" });
        }
    }

    [Fact]
    public void Scan_TcpEntries_HaveListenState()
    {
        var result = PortScanner.Scan();
        foreach (var entry in result.Where(e => e.Protocol == "TCP"))
        {
            Assert.Equal("LISTEN", entry.State);
        }
    }

    [Fact]
    public void Scan_UdpEntries_HaveEmptyState()
    {
        var result = PortScanner.Scan();
        foreach (var entry in result.Where(e => e.Protocol == "UDP"))
        {
            Assert.Equal("", entry.State);
        }
    }

    [Fact]
    public void Scan_DevProcessClassification_IsConsistent()
    {
        var result = PortScanner.Scan();
        foreach (var entry in result)
        {
            if (entry.IsDevProcess)
                Assert.True(PortScanner.IsDevProcessName(entry.ProcessName));
        }
    }
}
