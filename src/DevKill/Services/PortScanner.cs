using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using DevKill.Models;

namespace DevKill.Services;

public class PortScanner : IPortScanner
{
    private static readonly HashSet<string> DevProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "node", "dotnet", "php", "iisexpress", "python", "python3",
        "ruby", "java", "deno", "bun", "uvicorn", "gunicorn",
        "nginx", "httpd", "apache", "hugo", "caddy", "vite",
    };

    public List<PortEntry> Scan()
    {
        var entries = new List<PortEntry>();
        var processCache = new Dictionary<int, (string Name, string Path, string WorkDir)>();

        ScanTcp(entries, processCache);
        ScanTcp6(entries, processCache);
        ScanUdp(entries, processCache);
        ScanUdp6(entries, processCache);

        // Deduplicate dual-stack entries (e.g. 0.0.0.0 + ::) — keep first (IPv4)
        return entries
            .DistinctBy(e => (e.Port, e.Pid, e.Protocol))
            .ToList();
    }

    private void ScanTcp(List<PortEntry> entries, Dictionary<int, (string Name, string Path, string WorkDir)> processCache)
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
                var (procName, procPath, procWorkDir) = GetProcessInfo(row.dwOwningPid, processCache);

                entries.Add(new PortEntry
                {
                    Port = port,
                    Pid = row.dwOwningPid,
                    ProcessName = procName,
                    ProcessPath = procPath,
                    WorkingDirectory = procWorkDir,
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

    private void ScanUdp(List<PortEntry> entries, Dictionary<int, (string Name, string Path, string WorkDir)> processCache)
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
                var (procName, procPath, procWorkDir) = GetProcessInfo(row.dwOwningPid, processCache);

                if (!IsDevProcessName(procName))
                    continue;

                var addr = new IPAddress(row.dwLocalAddr).ToString();

                entries.Add(new PortEntry
                {
                    Port = port,
                    Pid = row.dwOwningPid,
                    ProcessName = procName,
                    ProcessPath = procPath,
                    WorkingDirectory = procWorkDir,
                    Protocol = "UDP",
                    LocalAddress = addr,
                    State = "",
                    IsDevProcess = true,
                });
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private unsafe void ScanTcp6(List<PortEntry> entries, Dictionary<int, (string Name, string Path, string WorkDir)> processCache)
    {
        int size = 0;
        NativeMethods.GetExtendedTcpTable(IntPtr.Zero, ref size, false, NativeMethods.AF_INET6, NativeMethods.TCP_TABLE_OWNER_PID_ALL, 0);
        if (size <= 0)
            return;

        IntPtr buffer = Marshal.AllocHGlobal(size);
        try
        {
            int result = NativeMethods.GetExtendedTcpTable(buffer, ref size, false, NativeMethods.AF_INET6, NativeMethods.TCP_TABLE_OWNER_PID_ALL, 0);
            if (result != 0)
                return;

            int rowCount = Marshal.ReadInt32(buffer);
            IntPtr rowPtr = buffer + 4;
            int rowSize = Marshal.SizeOf<NativeMethods.MIB_TCP6ROW_OWNER_PID>();

            for (int i = 0; i < rowCount; i++)
            {
                var row = Marshal.PtrToStructure<NativeMethods.MIB_TCP6ROW_OWNER_PID>(rowPtr);
                rowPtr += rowSize;

                if (row.dwState != NativeMethods.MIB_TCP_STATE_LISTEN)
                    continue;

                int port = NetworkToHostPort(row.dwLocalPort);
                var addrBytes = new byte[16];
                for (int j = 0; j < 16; j++)
                    addrBytes[j] = row.ucLocalAddr[j];
                var addr = new IPAddress(addrBytes).ToString();
                var (procName, procPath, procWorkDir) = GetProcessInfo(row.dwOwningPid, processCache);

                entries.Add(new PortEntry
                {
                    Port = port,
                    Pid = row.dwOwningPid,
                    ProcessName = procName,
                    ProcessPath = procPath,
                    WorkingDirectory = procWorkDir,
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

    private unsafe void ScanUdp6(List<PortEntry> entries, Dictionary<int, (string Name, string Path, string WorkDir)> processCache)
    {
        int size = 0;
        NativeMethods.GetExtendedUdpTable(IntPtr.Zero, ref size, false, NativeMethods.AF_INET6, NativeMethods.UDP_TABLE_OWNER_PID, 0);
        if (size <= 0)
            return;

        IntPtr buffer = Marshal.AllocHGlobal(size);
        try
        {
            int result = NativeMethods.GetExtendedUdpTable(buffer, ref size, false, NativeMethods.AF_INET6, NativeMethods.UDP_TABLE_OWNER_PID, 0);
            if (result != 0)
                return;

            int rowCount = Marshal.ReadInt32(buffer);
            IntPtr rowPtr = buffer + 4;
            int rowSize = Marshal.SizeOf<NativeMethods.MIB_UDP6ROW_OWNER_PID>();

            for (int i = 0; i < rowCount; i++)
            {
                var row = Marshal.PtrToStructure<NativeMethods.MIB_UDP6ROW_OWNER_PID>(rowPtr);
                rowPtr += rowSize;

                int port = NetworkToHostPort(row.dwLocalPort);
                var (procName, procPath, procWorkDir) = GetProcessInfo(row.dwOwningPid, processCache);

                if (!IsDevProcessName(procName))
                    continue;

                var addrBytes = new byte[16];
                for (int j = 0; j < 16; j++)
                    addrBytes[j] = row.ucLocalAddr[j];
                var addr = new IPAddress(addrBytes).ToString();

                entries.Add(new PortEntry
                {
                    Port = port,
                    Pid = row.dwOwningPid,
                    ProcessName = procName,
                    ProcessPath = procPath,
                    WorkingDirectory = procWorkDir,
                    Protocol = "UDP",
                    LocalAddress = addr,
                    State = "",
                    IsDevProcess = true,
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

    private static (string Name, string Path, string WorkDir) GetProcessInfo(int pid, Dictionary<int, (string Name, string Path, string WorkDir)> cache)
    {
        if (cache.TryGetValue(pid, out var cached))
            return cached;

        string name = "";
        string path = "";
        string workDir = "";

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
            workDir = GetProcessWorkingDirectory(pid);
        }
        catch
        {
            // Process may have exited
        }

        var info = (name, path, workDir);
        cache[pid] = info;
        return info;
    }

    /// <summary>
    /// Reads the working directory of a process by querying its PEB via NtQueryInformationProcess.
    /// Returns empty string if access is denied or the process has exited.
    /// </summary>
    private static string GetProcessWorkingDirectory(int pid)
    {
        IntPtr hProcess = NativeMethods.OpenProcess(
            NativeMethods.PROCESS_QUERY_INFORMATION | NativeMethods.PROCESS_VM_READ,
            false, pid);

        if (hProcess == IntPtr.Zero)
            return "";

        try
        {
            var pbi = new NativeMethods.PROCESS_BASIC_INFORMATION();
            int status = NativeMethods.NtQueryInformationProcess(
                hProcess, 0, ref pbi,
                Marshal.SizeOf<NativeMethods.PROCESS_BASIC_INFORMATION>(), out _);

            if (status != 0 || pbi.PebBaseAddress == IntPtr.Zero)
                return "";

            // Read ProcessParameters pointer from PEB (offset 0x20 on x64)
            IntPtr processParametersPtr = ReadPointer(hProcess, pbi.PebBaseAddress + 0x20);
            if (processParametersPtr == IntPtr.Zero)
                return "";

            // Read CurrentDirectory.DosPath UNICODE_STRING at offset 0x38
            // UNICODE_STRING layout on x64: ushort Length, ushort MaxLength, 4 bytes padding, IntPtr Buffer
            IntPtr unicodeStringAddr = processParametersPtr + 0x38;
            IntPtr buf = Marshal.AllocHGlobal(16);
            try
            {
                if (!NativeMethods.ReadProcessMemory(hProcess, unicodeStringAddr, buf, 16, out _))
                    return "";

                ushort length = (ushort)Marshal.ReadInt16(buf, 0);
                IntPtr strBuffer = Marshal.ReadIntPtr(buf, 8);

                if (length == 0 || strBuffer == IntPtr.Zero)
                    return "";

                IntPtr strBuf = Marshal.AllocHGlobal(length);
                try
                {
                    if (!NativeMethods.ReadProcessMemory(hProcess, strBuffer, strBuf, length, out _))
                        return "";

                    string dir = Marshal.PtrToStringUni(strBuf, length / 2) ?? "";
                    // Remove trailing backslash for consistency (unless it's a root like C:\)
                    if (dir.Length > 3 && dir.EndsWith('\\'))
                        dir = dir[..^1];
                    return dir;
                }
                finally
                {
                    Marshal.FreeHGlobal(strBuf);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }
        catch
        {
            return "";
        }
        finally
        {
            NativeMethods.CloseHandle(hProcess);
        }
    }

    private static IntPtr ReadPointer(IntPtr hProcess, IntPtr address)
    {
        IntPtr buf = Marshal.AllocHGlobal(IntPtr.Size);
        try
        {
            if (!NativeMethods.ReadProcessMemory(hProcess, address, buf, IntPtr.Size, out _))
                return IntPtr.Zero;
            return Marshal.ReadIntPtr(buf);
        }
        finally
        {
            Marshal.FreeHGlobal(buf);
        }
    }
}
