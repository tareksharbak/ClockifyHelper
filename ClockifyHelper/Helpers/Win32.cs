using System;
using System.Runtime.InteropServices;

namespace ClockifyHelper.Helpers
{
    internal class Win32
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto,
                   CallingConvention = CallingConvention.StdCall)]
        internal static extern bool GetLastInputInfo(out Win32LastInputInfo plii);
    }
}
