using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using DevKill.Models;

namespace DevKill.Services;

public static class PortScanner
{
    private static readonly HashSet<string> DevProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "node", "dotnet", "php", "iisexpress", "python", "python3",
        "ruby", "java", "deno", "bun", "uvicorn", "gunicorn",
        "nginx", "httpd", "apache", "hugo", "caddy", "vite",
    };

    public static List<PortEntry> Scan()
    {
        var entries = new List<PortEntry>();
        var processCache = new Dictionary<int, (string Name, string Path)>();

        ScanTcp(entries, processCache);
        ScanUdp(entries, processCache);

        return entries;
    }

    private static void ScanTcp(List<PortEntry> entries, Dictionary<int, (string Name, string Path)> processCache)
    {
        int size = 0;
        int sizeResult = NativeMethods.GetExtendedTcpTable(IntPtr.Zero, ref size, false, NativeMethods.AF_INET, NativeMethods.TCP_TABLE_OWNER_PID_ALL, 0);
        if (size <= 0)
            return;

        IntPtr buffer = Marshal.AllocHGlobal(size);
        try
        {
            int result = NativeMethods.GetExtendedTcpTable(buffer, ref size, false, NativeMethods.AF_INET, NativeMethods.TCP_TABLE_OWNER_PID_ALL, 0);
            if (result != 0)
                return;

            int rowCount = Marshal.ReadInt32(buffer);
            IntPtr rowPtr = buffer + 4;
            int rowSize = Marshal.SizeOf<NativeMethods.MIB_TCPROW_OWNER_PID>();

            for (int i = 0; i < rowCount; i++)
            {
                var row = Marshal.PtrToStructure<NativeMethods.MIB_TCPROW_OWNER_PID>(rowPtr);
                rowPtr += rowSize;

                if (row.dwState != NativeMethods.MIB_TCP_STATE_LISTEN)
                    continue;

                int port = NetworkToHostPort(row.dwLocalPort);
                var addr = new IPAddress(row.dwLocalAddr).ToString();
                var (procName, procPath) = GetProcessInfo(row.dwOwningPid, processCache);

                entries.Add(new PortEntry
                {
                    Port = port,
                    Pid = row.dwOwningPid,
                    ProcessName = procName,
                    ProcessPath = procPath,
                    Protocol = "TCP",
                    LocalAddress = addr,
                    State = "LISTEN",
                    IsDevProcess = IsDevProcessName(procName),
                });
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static void ScanUdp(List<PortEntry> entries, Dictionary<int, (string Name, string Path)> processCache)
    {
        int size = 0;
        int sizeResult = NativeMethods.GetExtendedUdpTable(IntPtr.Zero, ref size, false, NativeMethods.AF_INET, NativeMethods.UDP_TABLE_OWNER_PID, 0);
        if (size <= 0)
            return;

        IntPtr buffer = Marshal.AllocHGlobal(size);
        try
        {
            int result = NativeMethods.GetExtendedUdpTable(buffer, ref size, false, NativeMethods.AF_INET, NativeMethods.UDP_TABLE_OWNER_PID, 0);
            if (result != 0)
                return;

            int rowCount = Marshal.ReadInt32(buffer);
            IntPtr rowPtr = buffer + 4;
            int rowSize = Marshal.SizeOf<NativeMethods.MIB_UDPROW_OWNER_PID>();

            for (int i = 0; i < rowCount; i++)
            {
                var row = Marshal.PtrToStructure<NativeMethods.MIB_UDPROW_OWNER_PID>(rowPtr);
                rowPtr += rowSize;

                int port = NetworkToHostPort(row.dwLocalPort);
                var addr = new IPAddress(row.dwLocalAddr).ToString();
                var (procName, procPath) = GetProcessInfo(row.dwOwningPid, processCache);

                entries.Add(new PortEntry
                {
                    Port = port,
                    Pid = row.dwOwningPid,
                    ProcessName = procName,
                    ProcessPath = procPath,
                    Protocol = "UDP",
                    LocalAddress = addr,
                    State = "",
                    IsDevProcess = IsDevProcessName(procName),
                });
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    internal static int NetworkToHostPort(int networkPort)
    {
        // Port is stored in first 2 bytes in network byte order.
        // Cast to ushort before widening to int to avoid sign extension for ports > 32767.
        return (ushort)IPAddress.NetworkToHostOrder((short)(networkPort & 0xFFFF));
    }

    internal static bool IsDevProcessName(string processName)
    {
        if (string.IsNullOrEmpty(processName))
            return false;

        // Strip .exe suffix for matching
        var name = processName;
        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            name = name[..^4];

        return DevProcessNames.Contains(name);
    }

    private static (string Name, string Path) GetProcessInfo(int pid, Dictionary<int, (string Name, string Path)> cache)
    {
        if (cache.TryGetValue(pid, out var cached))
            return cached;

        string name = "";
        string path = "";

        try
        {
            using var proc = Process.GetProcessById(pid);
            name = proc.ProcessName;
            try
            {
                path = proc.MainModule?.FileName ?? "";
            }
            catch
            {
                // Access denied for some system processes
            }
        }
        catch
        {
            // Process may have exited
        }

        var info = (name, path);
        cache[pid] = info;
        return info;
    }
}
