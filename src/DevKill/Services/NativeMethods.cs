using System.Runtime.InteropServices;

namespace DevKill.Services;

internal static partial class NativeMethods
{
    public const int AF_INET = 2;
    public const int AF_INET6 = 23;

    // TCP table types
    public const int TCP_TABLE_OWNER_PID_ALL = 5;

    // UDP table types
    public const int UDP_TABLE_OWNER_PID = 1;

    // TCP states
    public const int MIB_TCP_STATE_LISTEN = 2;

    // Console attachment
    public const int ATTACH_PARENT_PROCESS = -1;

    [LibraryImport("iphlpapi.dll", SetLastError = true)]
    public static partial int GetExtendedTcpTable(
        IntPtr pTcpTable,
        ref int pdwSize,
        [MarshalAs(UnmanagedType.Bool)] bool bOrder,
        int ulAf,
        int TableClass,
        int Reserved);

    [LibraryImport("iphlpapi.dll", SetLastError = true)]
    public static partial int GetExtendedUdpTable(
        IntPtr pUdpTable,
        ref int pdwSize,
        [MarshalAs(UnmanagedType.Bool)] bool bOrder,
        int ulAf,
        int TableClass,
        int Reserved);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AttachConsole(int dwProcessId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AllocConsole();

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool FreeConsole();

    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_TCPROW_OWNER_PID
    {
        public int dwState;
        public uint dwLocalAddr;
        public int dwLocalPort;
        public uint dwRemoteAddr;
        public int dwRemotePort;
        public int dwOwningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_UDPROW_OWNER_PID
    {
        public uint dwLocalAddr;
        public int dwLocalPort;
        public int dwOwningPid;
    }

    // Process working directory retrieval
    public const int PROCESS_QUERY_INFORMATION = 0x0400;
    public const int PROCESS_VM_READ = 0x0010;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial IntPtr OpenProcess(int dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(IntPtr hObject);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, nint dwSize, out nint lpNumberOfBytesRead);

    [LibraryImport("ntdll.dll")]
    public static partial int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref PROCESS_BASIC_INFORMATION processInformation, int processInformationLength, out int returnLength);

    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_BASIC_INFORMATION
    {
        public IntPtr Reserved1;
        public IntPtr PebBaseAddress;
        public IntPtr Reserved2_0;
        public IntPtr Reserved2_1;
        public IntPtr UniqueProcessId;
        public IntPtr Reserved3;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct MIB_TCP6ROW_OWNER_PID
    {
        public fixed byte ucLocalAddr[16];
        public uint dwLocalScopeId;
        public int dwLocalPort;
        public fixed byte ucRemoteAddr[16];
        public uint dwRemoteScopeId;
        public int dwRemotePort;
        public int dwState;
        public int dwOwningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct MIB_UDP6ROW_OWNER_PID
    {
        public fixed byte ucLocalAddr[16];
        public uint dwLocalScopeId;
        public int dwLocalPort;
        public int dwOwningPid;
    }
}
