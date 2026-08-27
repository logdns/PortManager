using System.Runtime.InteropServices;

namespace PortManager.Services;

/// <summary>
/// Minimal native notification-area icon for the unpackaged WinUI app.
/// </summary>
internal static class TrayIconService
{
    private const uint NimAdd = 0x00000000;
    private const uint NimDelete = 0x00000002;
    private const uint NimSetVersion = 0x00000004;
    private const uint NifMessage = 0x00000001;
    private const uint NifIcon = 0x00000002;
    private const uint NifTip = 0x00000004;
    private const uint ImageIcon = 1;
    private const uint LoadFromFile = 0x00000010;
    private const uint LoadDefaultSize = 0x00000040;
    private const uint WmTrayIcon = 0x8001;
    private const uint WmLButtonDblClk = 0x0203;
    private const uint WmRButtonUp = 0x0205;
    private const uint WmCommand = 0x0111;
    private const uint WmDestroy = 0x0002;
    private const uint TpmRightButton = 0x0002;
    private const uint TpmRetCmd = 0x0100;
    private const uint MenuString = 0x0000;
    private const uint MenuOpen = 1;
    private const uint MenuExit = 2;

    private static readonly object Sync = new();
    private static WndProc? _windowProc;
    private static IntPtr _window;
    private static IntPtr _icon;
    private static string? _className;
    private static Action? _restore;
    private static Action? _exit;

    public static void Initialize(IntPtr owner, string iconPath, Action restore, Action exit)
    {
        if (!OperatingSystem.IsWindows()) return;
        lock (Sync)
        {
            if (_window != IntPtr.Zero) return;
            _restore = restore;
            _exit = exit;
            _className = $"PortManager.Tray.{Environment.ProcessId}";
            _windowProc = WindowProc;
            var className = Marshal.StringToHGlobalUni(_className);
            try
            {
                var windowClass = new WindowClassEx
                {
                    Size = (uint)Marshal.SizeOf<WindowClassEx>(),
                    Style = 0,
                    WindowProc = Marshal.GetFunctionPointerForDelegate(_windowProc),
                    ClassExtra = 0,
                    WindowExtra = 0,
                    Instance = GetModuleHandle(null),
                    ClassName = className
                };
                if (RegisterClassEx(ref windowClass) == 0)
                    throw new InvalidOperationException($"Could not register the tray window class (Win32 error {Marshal.GetLastWin32Error()}).");
                _window = CreateWindowEx(0, _className, "PortManagerTray", 0, 0, 0, 0, 0,
                    IntPtr.Zero, IntPtr.Zero, windowClass.Instance, IntPtr.Zero);
                if (_window == IntPtr.Zero)
                    throw new InvalidOperationException("Could not create the tray notification window.");

                _icon = LoadImage(IntPtr.Zero, iconPath, ImageIcon, 0, 0, LoadFromFile | LoadDefaultSize);
                if (_icon == IntPtr.Zero)
                    _icon = LoadIcon(IntPtr.Zero, (IntPtr)32512);

                var data = CreateIconData();
                if (!Shell_NotifyIcon(NimAdd, ref data))
                    throw new InvalidOperationException($"Could not add the tray icon (Win32 error {Marshal.GetLastWin32Error()}).");

                data.Version = 4;
                Shell_NotifyIcon(NimSetVersion, ref data);
                App.LogStartup("Tray icon initialized.");
            }
            catch
            {
                if (_window != IntPtr.Zero)
                    DestroyWindow(_window);
                if (_icon != IntPtr.Zero)
                    DestroyIcon(_icon);
                _window = IntPtr.Zero;
                _icon = IntPtr.Zero;
                _restore = null;
                _exit = null;
                _windowProc = null;
                throw;
            }
            finally
            {
                Marshal.FreeHGlobal(className);
            }
        }
    }

    public static void Dispose()
    {
        if (!OperatingSystem.IsWindows()) return;
        lock (Sync)
        {
            if (_window == IntPtr.Zero) return;
            var data = CreateIconData();
            Shell_NotifyIcon(NimDelete, ref data);
            DestroyWindow(_window);
            if (_icon != IntPtr.Zero) DestroyIcon(_icon);
            if (_className is not null)
                UnregisterClass(_className, GetModuleHandle(null));
            _window = IntPtr.Zero;
            _icon = IntPtr.Zero;
            _restore = null;
            _exit = null;
            _windowProc = null;
        }
    }

    private static NotifyIconData CreateIconData() => new()
    {
        Size = (uint)Marshal.SizeOf<NotifyIconData>(),
        Window = _window,
        Id = 1,
        Flags = NifMessage | NifIcon | NifTip,
        CallbackMessage = WmTrayIcon,
        Icon = _icon,
        Tip = LanguageState.IsEnglish ? "Port Manager" : "端口管理器",
        Info = string.Empty,
        InfoTitle = string.Empty
    };

    private static IntPtr WindowProc(IntPtr window, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message == WmTrayIcon)
        {
            // NOTIFYICON_VERSION_4 packs the icon id in HIWORD(lParam).
            var notification = unchecked((uint)lParam.ToInt64()) & 0xffff;
            if (notification == WmLButtonDblClk)
            {
                _restore?.Invoke();
                return IntPtr.Zero;
            }

            if (notification == WmRButtonUp)
            {
                ShowContextMenu(window);
                return IntPtr.Zero;
            }
        }
        else if (message == WmCommand)
        {
            var command = unchecked((uint)wParam.ToInt64()) & 0xffff;
            if (command == MenuOpen) _restore?.Invoke();
            if (command == MenuExit) _exit?.Invoke();
            PostMessage(window, 0, IntPtr.Zero, IntPtr.Zero);
            return IntPtr.Zero;
        }
        else if (message == WmDestroy)
        {
            return IntPtr.Zero;
        }

        return DefWindowProc(window, message, wParam, lParam);
    }

    private static void ShowContextMenu(IntPtr window)
    {
        var menu = CreatePopupMenu();
        if (menu == IntPtr.Zero) return;
        try
        {
            AppendMenu(menu, MenuString, MenuOpen, LanguageState.IsEnglish ? "Open Port Manager" : "打开端口管理器");
            AppendMenu(menu, MenuString, MenuExit, LanguageState.IsEnglish ? "Exit" : "退出程序");
            SetForegroundWindow(window);
            GetCursorPos(out var point);
            var command = TrackPopupMenu(menu, TpmRightButton | TpmRetCmd, point.X, point.Y, 0, window, IntPtr.Zero);
            if (command == MenuOpen) _restore?.Invoke();
            if (command == MenuExit) _exit?.Invoke();
            PostMessage(window, 0, IntPtr.Zero, IntPtr.Zero);
        }
        finally
        {
            DestroyMenu(menu);
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate IntPtr WndProc(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowClassEx
    {
        public uint Size;
        public uint Style;
        public IntPtr WindowProc;
        public int ClassExtra;
        public int WindowExtra;
        public IntPtr Instance;
        public IntPtr Icon;
        public IntPtr Cursor;
        public IntPtr Background;
        public IntPtr MenuName;
        public IntPtr ClassName;
        public IntPtr SmallIcon;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public uint Size;
        public IntPtr Window;
        public uint Id;
        public uint Flags;
        public uint CallbackMessage;
        public IntPtr Icon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Tip;
        public uint State;
        public uint StateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Info;
        public uint Version;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string InfoTitle;
        public uint InfoFlags;
        public Guid GuidItem;
        public IntPtr BalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassEx(ref WindowClassEx windowClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(int exStyle, string className, string windowName, int style,
        int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterClass(string className, IntPtr instance);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr window);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? moduleName);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Shell_NotifyIcon(uint message, ref NotifyIconData data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadImage(IntPtr instance, string name, uint type, int width, int height, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadIcon(IntPtr instance, IntPtr iconName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr icon);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AppendMenu(IntPtr menu, uint flags, uint id, string text);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr menu);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr window);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll")]
    private static extern uint TrackPopupMenu(IntPtr menu, uint flags, int x, int y, int reserved, IntPtr window, IntPtr rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
}
