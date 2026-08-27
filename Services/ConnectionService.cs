using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using PortManager.Models;

namespace PortManager.Services;

public static class ConnectionService
{
    private const int AfInet = 2;
    private const int ErrorInsufficientBuffer = 122;
    private const int TcpTableOwnerPidAll = 5;
    private const int UdpTableOwnerPid = 1;

    public static Task<List<ConnectionModel>> ListConnectionsAsync() =>
        Task.Run(() =>
        {
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Connection monitoring is available only on Windows.");

            var connections = new List<ConnectionModel>();
            connections.AddRange(ReadTcpConnections());
            connections.AddRange(ReadUdpConnections());
            return connections
                .OrderBy(connection => connection.Protocol)
                .ThenBy(connection => connection.LocalPort)
                .ThenBy(connection => connection.RemoteAddress, StringComparer.OrdinalIgnoreCase)
                .ToList();
        });

    private static IEnumerable<ConnectionModel> ReadTcpConnections()
    {
        var buffer = GetTable(IntPtr.Zero, out var size, TcpTableOwnerPidAll, true);
        try
        {
            var rowSize = Marshal.SizeOf<MibTcpRowOwnerPid>();
            var count = Marshal.ReadInt32(buffer);
            var rows = new List<ConnectionModel>(count);
            var rowPointer = IntPtr.Add(buffer, sizeof(int));
            for (var index = 0; index < count; index++)
            {
                var row = Marshal.PtrToStructure<MibTcpRowOwnerPid>(rowPointer);
                rows.Add(new ConnectionModel
                {
                    Protocol = "TCP",
                    LocalAddress = FormatAddress(row.LocalAddress),
                    LocalPort = NetworkToHostPort(row.LocalPort),
                    RemoteAddress = FormatAddress(row.RemoteAddress),
                    RemotePort = NetworkToHostPort(row.RemotePort),
                    State = FormatTcpState(row.State),
                    ProcessId = checked((int)row.ProcessId),
                    ProcessName = GetProcessName(row.ProcessId)
                });
                rowPointer = IntPtr.Add(rowPointer, rowSize);
            }

            return rows;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static IEnumerable<ConnectionModel> ReadUdpConnections()
    {
        var buffer = GetTable(IntPtr.Zero, out var size, UdpTableOwnerPid, false);
        try
        {
            var rowSize = Marshal.SizeOf<MibUdpRowOwnerPid>();
            var count = Marshal.ReadInt32(buffer);
            var rows = new List<ConnectionModel>(count);
            var rowPointer = IntPtr.Add(buffer, sizeof(int));
            for (var index = 0; index < count; index++)
            {
                var row = Marshal.PtrToStructure<MibUdpRowOwnerPid>(rowPointer);
                rows.Add(new ConnectionModel
                {
                    Protocol = "UDP",
                    LocalAddress = FormatAddress(row.LocalAddress),
                    LocalPort = NetworkToHostPort(row.LocalPort),
                    RemoteAddress = "*",
                    RemotePort = 0,
                    State = "Udp",
                    ProcessId = checked((int)row.ProcessId),
                    ProcessName = GetProcessName(row.ProcessId)
                });
                rowPointer = IntPtr.Add(rowPointer, rowSize);
            }

            return rows;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static IntPtr GetTable(IntPtr table, out int size, int tableClass, bool tcp)
    {
        size = 0;
        var result = tcp
            ? GetExtendedTcpTable(table, ref size, true, AfInet, tableClass, 0)
            : GetExtendedUdpTable(table, ref size, true, AfInet, tableClass, 0);
        if (result != 0 && result != ErrorInsufficientBuffer)
            throw new ConnectionOperationException($"Windows connection table failed with error {result}.");

        table = Marshal.AllocHGlobal(size);
        result = tcp
            ? GetExtendedTcpTable(table, ref size, true, AfInet, tableClass, 0)
            : GetExtendedUdpTable(table, ref size, true, AfInet, tableClass, 0);
        if (result != 0)
        {
            Marshal.FreeHGlobal(table);
            throw new ConnectionOperationException($"Windows connection table failed with error {result}.");
        }

        return table;
    }

    private static string GetProcessName(uint processId)
    {
        if (processId == 0)
            return "System";

        try
        {
            return Process.GetProcessById(checked((int)processId)).ProcessName;
        }
        catch (Exception) when (processId != 0)
        {
            return $"PID {processId}";
        }
    }

    private static string FormatAddress(uint address) => new IPAddress(address).ToString();

    private static int NetworkToHostPort(uint port) =>
        (ushort)IPAddress.NetworkToHostOrder(unchecked((short)port));

    private static string FormatTcpState(uint state) => state switch
    {
        1 => "Closed",
        2 => "Listen",
        3 => "SynSent",
        4 => "SynReceived",
        5 => "Established",
        6 => "FinWait1",
        7 => "FinWait2",
        8 => "CloseWait",
        9 => "Closing",
        10 => "LastAck",
        11 => "TimeWait",
        12 => "DeleteTcb",
        _ => $"State {state}"
    };

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern int GetExtendedTcpTable(
        IntPtr table, ref int size, bool order, int addressFamily, int tableClass, uint reserved);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern int GetExtendedUdpTable(
        IntPtr table, ref int size, bool order, int addressFamily, int tableClass, uint reserved);

    [StructLayout(LayoutKind.Sequential)]
    private struct MibTcpRowOwnerPid
    {
        public uint State;
        public uint LocalAddress;
        public uint LocalPort;
        public uint RemoteAddress;
        public uint RemotePort;
        public uint ProcessId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MibUdpRowOwnerPid
    {
        public uint LocalAddress;
        public uint LocalPort;
        public uint ProcessId;
    }
}

public sealed class ConnectionOperationException : Exception
{
    public ConnectionOperationException(string message) : base(message)
    {
    }
}
