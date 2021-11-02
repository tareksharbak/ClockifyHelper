using System.Runtime.InteropServices;

namespace ClockifyHelper.Helpers
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Win32LastInputInfo
    {
        internal uint cbSize;
        internal uint dwTime;

        internal static readonly int SizeOf = Marshal.SizeOf(typeof(Win32LastInputInfo));
    }
}