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
