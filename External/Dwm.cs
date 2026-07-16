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

        // Window attributes for Windows 11 backdrop materials
        public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        public const int DWMWA_MICA_EFFECT = 1029;

        // System backdrop types
        public const int DWMSBT_NONE = 1;
        public const int DWMSBT_MAINWINDOW = 2;

        // Frame margins used to extend DWM rendering into the client area
        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS {
            public int Left;
            public int Right;
            public int Top;
            public int Bottom;

            public MARGINS(int value) {
                Left = value;
                Right = value;
                Top = value;
                Bottom = value;
            }
        }
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

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern int DwmExtendFrameIntoClientArea(
            IntPtr hwnd,
            ref MARGINS pMarInset);
#endregion

    }

}
