  //\\   OmenMon: Hardware Monitoring & Control Utility
 //  \\  Copyright © 2023-2024 Piotr Szczepański * License: GPL3
     //  https://omenmon.github.io/

using System;
using System.Runtime.InteropServices;

namespace OmenMon.External {

    // Windows Desktop Window Manager API (dwmapi.dll) resources
    public class Dwm {

#region Windows Desktop Window Manager API Data
        // Window attribute for using the immersive dark title bar
        public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        public const int DWMWA_USE_IMMERSIVE_DARK_MODE_LEGACY = 19;
#endregion

#region Windows Desktop Window Manager API Imports
        public const string DllName = "dwmapi.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            int dwAttribute,
            ref int pvAttribute,
            int cbAttribute);
#endregion

    }

}
