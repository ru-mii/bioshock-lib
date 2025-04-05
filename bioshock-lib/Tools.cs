using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

class Tools
{
    [DllImport("kernel32.dll")]
    private static extern void OutputDebugString(string lpOutputString);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool ReadProcessMemory(
    IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);

    [DllImport("kernel32.dll")]
    public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
    uint dwSize, uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress,
    int dwSize, uint flNewProtect, out int lpflOldProtect);

    private static string RandomIDDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "rumii", "bioshock-lib", "random.txt");

    public static string RandomID = "";

    internal static void SaveRandomID(string id)
    {
        if (!Directory.Exists(Path.GetDirectoryName(RandomIDDir)))
            Directory.CreateDirectory(Path.GetDirectoryName(RandomIDDir));

        RandomID = id;
        File.WriteAllText(RandomIDDir, id);
    }

    internal static string GetRandomID()
    {
        if (!Directory.Exists(Path.GetDirectoryName(RandomIDDir)))
            Directory.CreateDirectory(Path.GetDirectoryName(RandomIDDir));

        if (File.Exists(RandomIDDir)) return File.ReadAllText(RandomIDDir);
        else return "";
    }

    internal static string GetProcessToken(Process process)
    {
        ulong startTime = (ulong)((DateTimeOffset)process.StartTime).ToUnixTimeMilliseconds();
        return startTime.ToString("X") + "-" + process.Id.ToString("X");
    }

    internal static ulong GetTimeMiliseconds()
    {
        return (ulong)((DateTimeOffset)DateTime.Now).ToUnixTimeMilliseconds();
    }

    internal static ulong AllocateMemory(Process process, ulong address = 0, uint size = 0x1000)
    {
        return (ulong)VirtualAllocEx(process.Handle,
            (IntPtr)address, size, 0x1000 | 0x2000, 0x40);
    }

    public static bool WriteMemoryBytes(Process process, ulong address, byte[] data)
    {
        return WriteMemoryBytes(process, (IntPtr)address, data);
    }

    internal static bool WriteMemoryBytes(Process process, IntPtr address, byte[] data)
    {
        bool success = WriteProcessMemory(process.Handle, address, data, data.Length, out _);

        if (!success)
        {
            int oldProt = 0;
            VirtualProtectEx(process.Handle, address, data.Length, 0x40, out oldProt);
            bool success2 = WriteProcessMemory(process.Handle, address, data, data.Length, out _);
            VirtualProtectEx(process.Handle, address, data.Length, (uint)oldProt, out _);
            return success2;
        }
        else return true;
    }

    internal static byte[] ReadMemoryBytes(Process process, ulong address, int size)
    {
        return ReadMemoryBytes(process, (IntPtr)address, size);
    }

    internal static byte[] ReadMemoryBytes(Process process, IntPtr address, int size)
    {
        byte[] data = new byte[size];
        if (ReadProcessMemory(process.Handle, address, data, data.Length, out _))
            return data;
        return null;
    }

    internal static T ReadMemory<T>(Process process, ulong address) where T : unmanaged
    {
        return ReadMemory<T>(process, (IntPtr)address);
    }

    internal static T ReadMemory<T>(Process process, IntPtr address) where T : unmanaged
    {
        int typeSize = Marshal.SizeOf(typeof(T));
        byte[] data = new byte[typeSize];

        if (ReadProcessMemory(process.Handle, address, data, data.Length, out _))
        {
            if (typeof(T) == typeof(byte)) return (T)(object)data[0];
            else if (typeof(T) == typeof(short)) return (T)(object)BitConverter.ToInt16(data, 0);
            else if (typeof(T) == typeof(ushort)) return (T)(object)BitConverter.ToUInt16(data, 0);
            else if (typeof(T) == typeof(int)) return (T)(object)BitConverter.ToInt32(data, 0);
            else if (typeof(T) == typeof(uint)) return (T)(object)BitConverter.ToUInt32(data, 0);
            else if (typeof(T) == typeof(long)) return (T)(object)BitConverter.ToInt64(data, 0);
            else if (typeof(T) == typeof(ulong)) return (T)(object)BitConverter.ToUInt64(data, 0);
            else if (typeof(T) == typeof(double)) return (T)(object)BitConverter.ToDouble(data, 0);
            else if (typeof(T) == typeof(float)) return (T)(object)BitConverter.ToSingle(data, 0);
        }
        return default(T);
    }

    internal static void OutputDebug(string text)
    {
        OutputDebugString(text);
    }
}
