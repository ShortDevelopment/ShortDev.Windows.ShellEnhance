using Internal.Windows.UI.Xaml;
using ShortDev.Uwp.FullTrust.Xaml;
using ShortDev.Win32.Windowing;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT;
using Window = Windows.UI.Xaml.Window;

namespace ShortDev.Windows.ShellEnhance.UI;


// https://github.com/microsoft/Windows-classic-samples/blob/main/Samples/Win7Samples/winui/shell/appshellintegration/NotificationIcon/NotificationIcon.cpp

internal class NotificationIcon : WindowSubclass.IMessageFilter, IDisposable
{
    public ushort Id { get; }

    public string IconId { get; set; }
    public Window Window { get; }
    public Type Content { get; }
    public NotificationIcon(ushort id, string iconId, Window window, Type content)
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

        NOTIFYICONDATAW data = new()
        {
            hWnd = (HWND)Window.Hwnd,
            uID = Id
        };
        data.cbSize = (uint)Marshal.SizeOf(data);
        Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, data);
    }

    const uint App_NotifyIcon = 0x8000 + 1;
    const int NOTIFYICON_VERSION_4 = 4;

    public void Show()
    {
        NOTIFYICONDATAW data = new()
        {
            hWnd = (HWND)Window.Hwnd,
            uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE,
            hIcon = LoadIcon(),
            uCallbackMessage = App_NotifyIcon,
            uID = Id
        };
        data.cbSize = (uint)Marshal.SizeOf(data);
        Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_ADD, data);

        data.Anonymous.uVersion = NOTIFYICON_VERSION_4;
        Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_SETVERSION, data);
    }

    unsafe HICON LoadIcon()
    {
        string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", $"{IconId}_light.ico");
        if (!File.Exists(iconPath))
            throw new FileNotFoundException(iconPath);

        fixed (char* pPath = iconPath)
        {
            return (HICON)LoadImage(
                HINSTANCE.Null,
                (PCWSTR)pPath,
                GDI_IMAGE_TYPE.IMAGE_ICON,
                0, 0,
                IMAGE_FLAGS.LR_DEFAULTSIZE | IMAGE_FLAGS.LR_LOADFROMFILE
            ).Value;
        }
    }

    unsafe bool WindowSubclass.IMessageFilter.PreFilterMessage(IntPtr hwnd, int msg, nuint wParam, nint lParam, nuint id, out IntPtr result)
    {
        const int WM_USER = 1024;
        const int NIN_SELECT = WM_USER;
        if (msg == (int)App_NotifyIcon && LOWORD(lParam) == NIN_SELECT && HIWORD(lParam) == Id)
        {
            this.Window.As<IWindowPrivate>().Show();
            Program.YieldExecute(() => Program.Frame?.Navigate(Content));

            HWND windowHwnd = (HWND)Window.Hwnd;

            NOTIFYICONIDENTIFIER iconId = new()
            {
                hWnd = windowHwnd,
                uID = Id,
                cbSize = (uint)sizeof(NOTIFYICONIDENTIFIER)
            };
            Marshal.ThrowExceptionForHR(Shell_NotifyIconGetRect(iconId, out var rcIcon));

            System.Drawing.Point ptAnchor = new()
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
                ptAnchor,
                sizeWindow,
                (uint)(TRACK_POPUP_MENU_FLAGS.TPM_VERTICAL | TRACK_POPUP_MENU_FLAGS.TPM_VCENTERALIGN | TRACK_POPUP_MENU_FLAGS.TPM_CENTERALIGN | TRACK_POPUP_MENU_FLAGS.TPM_WORKAREA),
                rcIcon,
                out var popupWindowPosition
            ))
                throw new Win32Exception();

            SetWindowPos(windowHwnd, HWND.HWND_TOPMOST, popupWindowPosition.left, popupWindowPosition.top - 20, 0, 0, SET_WINDOW_POS_FLAGS.SWP_NOSIZE);
            SetForegroundWindow(windowHwnd);
        }

        result = IntPtr.Zero;
        return false;
    }

    static nint LOWORD(nint input)
        => input & 0xFFFF;

    static nint HIWORD(nint input)
        => input >> 16;
}
