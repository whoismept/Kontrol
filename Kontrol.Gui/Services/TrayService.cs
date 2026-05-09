using System.Runtime.InteropServices;

namespace Kontrol.Services;

internal sealed class TrayService : IDisposable
{
    // Shell_NotifyIcon messages
    private const uint NIM_ADD = 0, NIM_MODIFY = 1, NIM_DELETE = 2;

    // NOTIFYICONDATA flags
    private const uint NIF_MESSAGE = 0x01, NIF_ICON = 0x02, NIF_TIP = 0x04, NIF_INFO = 0x10;

    // Callback notification codes
    private const uint NIN_SELECT = 0x0400;
    private const uint WM_RBUTTONUP = 0x0205;

    // Balloon icon flags
    private const uint NIIF_INFO = 0x01, NIIF_NOSOUND = 0x10;

    // Popup menu flags
    private const uint TPM_RIGHTBUTTON = 0x0002, TPM_RETURNCMD = 0x0100, TPM_BOTTOMALIGN = 0x0020;
    private const uint MF_STRING = 0x00, MF_SEPARATOR = 0x0800;

    // Custom WM_APP message for tray callbacks
    private const uint WM_TRAY = 0x8001;

    private const uint IMAGE_ICON = 1, LR_LOADFROMFILE = 0x0010;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    private delegate IntPtr SUBCLASSPROC(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpdata);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadImage(IntPtr hInst, string name, uint type, int cx, int cy, uint fuLoad);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, IntPtr uIDNewItem, string? lpNewItem);

    [DllImport("user32.dll")]
    private static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern bool SetWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, IntPtr uIdSubclass, IntPtr dwRefData);

    [DllImport("comctl32.dll")]
    private static extern bool RemoveWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, IntPtr uIdSubclass);

    [DllImport("comctl32.dll")]
    private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    private static readonly IntPtr IDI_APPLICATION = new(32512);
    private static readonly IntPtr SubclassId = new(0x4B4F4E54); // "KONT"

    private NOTIFYICONDATA _nid;
    private SUBCLASSPROC? _subclassDelegate;
    private readonly IntPtr _hwnd;
    private IntPtr _hIcon;
    private bool _added;
    private bool _disposed;

    private readonly string _showHideText;
    private readonly string _exitText;

    public Action? OnLeftClick { get; set; }
    public Action? OnExit { get; set; }

    public TrayService(IntPtr hwnd, string tooltip, string showHideText, string exitText, string? iconPath = null)
    {
        _hwnd = hwnd;
        _showHideText = showHideText;
        _exitText = exitText;

        _hIcon = iconPath is not null && File.Exists(iconPath)
            ? LoadImage(IntPtr.Zero, iconPath, IMAGE_ICON, 16, 16, LR_LOADFROMFILE)
            : LoadIcon(IntPtr.Zero, IDI_APPLICATION);

        _nid = new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = hwnd,
            uID = 1,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = WM_TRAY,
            hIcon = _hIcon,
            szTip = tooltip,
            szInfo = string.Empty,
            szInfoTitle = string.Empty
        };

        _subclassDelegate = WndProcSubclass;
        SetWindowSubclass(hwnd, _subclassDelegate, SubclassId, IntPtr.Zero);

        Shell_NotifyIcon(NIM_ADD, ref _nid);
        _added = true;
    }

    private IntPtr WndProcSubclass(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData)
    {
        if (uMsg == WM_TRAY)
        {
            uint code = (uint)(lParam.ToInt64() & 0xFFFF);
            if (code == NIN_SELECT)
                OnLeftClick?.Invoke();
            else if (code == WM_RBUTTONUP)
                ShowContextMenu();
        }
        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    private void ShowContextMenu()
    {
        GetCursorPos(out POINT pt);
        var hMenu = CreatePopupMenu();
        AppendMenu(hMenu, MF_STRING, new IntPtr(1), _showHideText);
        AppendMenu(hMenu, MF_SEPARATOR, IntPtr.Zero, null);
        AppendMenu(hMenu, MF_STRING, new IntPtr(2), _exitText);
        SetForegroundWindow(_hwnd);
        var cmd = TrackPopupMenu(hMenu, TPM_RETURNCMD | TPM_RIGHTBUTTON | TPM_BOTTOMALIGN, pt.X, pt.Y, 0, _hwnd, IntPtr.Zero);
        DestroyMenu(hMenu);
        if (cmd == 1) OnLeftClick?.Invoke();
        else if (cmd == 2) OnExit?.Invoke();
    }

    public void ShowNotification(string title, string message)
    {
        if (_disposed) return;
        var nid = _nid;
        nid.uFlags |= NIF_INFO;
        nid.szInfoTitle = title;
        nid.szInfo = message;
        nid.dwInfoFlags = NIIF_INFO | NIIF_NOSOUND;
        nid.uTimeoutOrVersion = 5000;
        Shell_NotifyIcon(NIM_MODIFY, ref nid);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_added)
        {
            Shell_NotifyIcon(NIM_DELETE, ref _nid);
            _added = false;
        }
        if (_subclassDelegate is not null)
        {
            RemoveWindowSubclass(_hwnd, _subclassDelegate, SubclassId);
            _subclassDelegate = null;
        }
        if (_hIcon != IntPtr.Zero)
        {
            DestroyIcon(_hIcon);
            _hIcon = IntPtr.Zero;
        }
    }
}
