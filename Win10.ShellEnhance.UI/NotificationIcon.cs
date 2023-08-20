using ShortDev.Uwp.FullTrust.Xaml;
using ShortDev.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinUI.Interop.CoreWindow;

namespace ShortDev.ShellEnhance.UI;


// https://github.com/microsoft/Windows-classic-samples/blob/main/Samples/Win7Samples/winui/shell/appshellintegration/NotificationIcon/NotificationIcon.cpp

internal class NotificationIcon : Win32WindowSubclass.IMessageFilter, IDisposable
{
    public Guid Id { get; }

    public string IconId { get; set; }
    public Window Window { get; }
    public Type Content { get; }
    public NotificationIcon(Guid id, string iconId, Window window, Type content)
    {
        Id = id;
        IconId = iconId;
        Window = window;
        Content = content;

        var subclass = window.GetSubclass();
        subclass.Filters.Add(this);
    }

    public void Dispose()
    {
        var subclass = Window.GetSubclass();
        subclass.Filters.Remove(this);

        NotifyIconData data = new()
        {
            uFlags = NotifyIconFlags.Guid,
            guidItem = this.Id
        };
        data.cbSize = Marshal.SizeOf(data);
        Shell_NotifyIcon(NotifyIconMessage.Delete, ref data);
    }

    public void Show()
    {
        NotifyIconData data = new()
        {
            hWnd = Window.GetHwnd(),
            uFlags = NotifyIconFlags.Icon | NotifyIconFlags.Message | NotifyIconFlags.Guid,
            hIcon = LoadIcon(),
            uCallbackMessage = (int)WM.App_NotifyIcon,
            guidItem = Id
        };
        data.cbSize = Marshal.SizeOf(data);
        Shell_NotifyIcon(NotifyIconMessage.Add, ref data);

        const int NOTIFYICON_VERSION_4 = 4;
        data.uTimeoutOrVersion = NOTIFYICON_VERSION_4;
        Shell_NotifyIcon(NotifyIconMessage.SetVersion, ref data);
    }

    IntPtr LoadIcon()
    {
        string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", $"{IconId}_light.ico");
        if (!File.Exists(iconPath))
            throw new FileNotFoundException(iconPath);

        return LoadImage(
            IntPtr.Zero,
            iconPath,
            /* IMAGE_ICON */ 1,
            0, 0,
            /* LR_DEFAULTSIZE */ 0x00000040 | /* LR_LOADFROMFILE */ 0x00000010
        );
    }

    bool Win32WindowSubclass.IMessageFilter.PreFilterMessage(IntPtr hwnd, int msg, nuint wParam, nint lParam, nuint id, out IntPtr result)
    {
        const int WM_USER = 1024;
        const int NIN_SELECT = WM_USER;
        if (msg == (int)WM.App_NotifyIcon && LOWORD((int)lParam) == NIN_SELECT)
        {
            (Window as object as IWindowPrivate).Show();
            Program.YieldExecute(() => Program.Frame.Navigate(Content));

            IntPtr windowHwnd = Window.GetHwnd();

            NotifyIconDataIdentifier iconId = new()
            {
                guidItem = Id
            };
            iconId.cbSize = Marshal.SizeOf(iconId);
            Marshal.ThrowExceptionForHR(Shell_NotifyIconGetRect(ref iconId, out var rcIcon));

            POINT ptAnchor = new()
            {
                X = (rcIcon.left + rcIcon.right) / 2,
                Y = (rcIcon.top + rcIcon.bottom) / 2
            };

            if (!GetWindowRect(windowHwnd, out var rcWindow))
                throw new Win32Exception();

            SIZE sizeWindow = new()
            {
                cx = rcWindow.right - rcWindow.left,
                cy = rcWindow.bottom - rcWindow.top
            };
            if (!CalculatePopupWindowPosition(
                ref ptAnchor,
                ref sizeWindow,
                PopupWindowFlags.Vertical | PopupWindowFlags.VCenterAlign | PopupWindowFlags.CenterAlign | PopupWindowFlags.WorkArea,
                ref rcIcon,
                out var popupWindowPosition
            ))
                throw new Win32Exception();

            const int SWP_NOSIZE = 0x0001;
            IntPtr HWND_TOPMOST = (IntPtr)(-1);
            SetWindowPos(windowHwnd, HWND_TOPMOST, popupWindowPosition.left, popupWindowPosition.top - 20, 0, 0, SWP_NOSIZE);
            SetForegroundWindow(windowHwnd);
        }

        result = IntPtr.Zero;
        return false;
    }

    #region API
    int LOWORD(int input)
        => input & 0xFFFF;

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hwnd);

    enum WM : int
    {
        App = 0x8000,
        App_NotifyIcon = App + 1
    }

    [DllImport("shell32.dll")]
    static extern bool Shell_NotifyIcon(NotifyIconMessage dwMessage, [In] ref NotifyIconData pnid);

    enum NotifyIconMessage : int
    {
        Add = 0x00000000,
        Modify = 0x00000001,
        Delete = 0x00000002,
        SetFocus = 0x00000003,
        SetVersion = 0x00000004,
    }

    [StructLayout(LayoutKind.Sequential)]
    struct NotifyIconData
    {
        public System.Int32 cbSize; // DWORD
        public System.IntPtr hWnd; // HWND
        public System.Int32 uID; // UINT
        public NotifyIconFlags uFlags; // UINT
        public System.Int32 uCallbackMessage; // UINT
        public System.IntPtr hIcon; // HICON
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public System.String szTip; // char[128]
        public System.Int32 dwState; // DWORD
        public System.Int32 dwStateMask; // DWORD
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public System.String szInfo; // char[256]
        public System.Int32 uTimeoutOrVersion; // UINT
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public System.String szInfoTitle; // char[64]
        public System.Int32 dwInfoFlags; // DWORD
        public Guid guidItem;
    }

    enum NotifyIconFlags
    {
        Message = 0x1,
        Icon = 0x2,
        Guid = 0x20
    }

    [StructLayout(LayoutKind.Sequential)]
    struct NotifyIconDataIdentifier
    {
        public int cbSize;
        IntPtr hWnd;
        uint uId;
        public Guid guidItem;
    }


    [DllImport("shell32.dll", SetLastError = true)]
    static extern int Shell_NotifyIconGetRect([In] ref NotifyIconDataIdentifier id, [Out] out RECT location);

    [StructLayout(LayoutKind.Sequential)]
    struct RECT
    {
        public Int32 left;
        public Int32 top;
        public Int32 right;
        public Int32 bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SIZE
    {
        public int cx;
        public int cy;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CalculatePopupWindowPosition(ref POINT anchorPoint, ref SIZE windowSize, PopupWindowFlags flags, ref RECT excludeRect, out RECT popupWindowPosition);

    enum PopupWindowFlags
    {
        CenterAlign = 0x4,
        TopAlign = 0x0,
        BottomAlign = 0x0020,
        Horizontal = 0x0000,
        VCenterAlign = 0x0010,
        Vertical = 0x0040,
        WorkArea = 0x10000
    }


    [DllImport("user32.dll", SetLastError = true)]
    static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);


    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("shell32.dll")]
    static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType,
        int cxDesired, int cyDesired, uint fuLoad);
    #endregion
}
