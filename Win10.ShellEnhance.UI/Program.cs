using ShortDev.ShellEnhance.UI.Flyouts;
using ShortDev.Uwp.FullTrust.Xaml;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinUI.Interop.CoreWindow;

namespace ShortDev.ShellEnhance.UI;

public static class Program
{
    public static IWindowPrivate WindowPrivate { get; private set; }
    public static Window Window { get; private set; }
    public static Frame Frame { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        new App();
        Window = XamlWindowActivator.CreateNewWindow(new XamlWindowConfig("ShellEnhance")
        {
            HasWin32Frame = false,
            IsVisible = false,
            HasTransparentBackground = true,
        });
        Window.Activated += Window_Activated;

        var subclass = Window.GetSubclass();
        subclass.Win32Window.ShowInTaskBar = false;

        var hwnd = Window.GetHwnd();
        PostMessage(hwnd, 0x270, 0, 1);

        WindowPrivate = (IWindowPrivate)(object)Window;
        WindowPrivate.MoveWindow(0, 0, 350, 650);
        WindowPrivate.Hide();

        Frame = new();
        Frame.Navigate(typeof(Page));
        Frame.Navigated += Frame_Navigated;
        Window.Content = Frame;

        RegisterFlyout<CalendarFlyoutPage>();

        Window.Dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessUntilQuit);
    }

    [DllImport("User32.Dll")]
    public static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

    static void RegisterFlyout<T>() where T : Page, IShellEnhanceFlyout, new()
    {
        T flyoutContent = new();
        NotificationIcon icon = new(flyoutContent.IconId, flyoutContent.IconAssetId, Window, typeof(T));
        icon.Show();
    }

    static void Window_Activated(object sender, WindowActivatedEventArgs e)
    {
        if (e.WindowActivationState != CoreWindowActivationState.Deactivated)
            return;

        Frame.GoBack();
    }

    private static void Frame_Navigated(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        if (e.NavigationMode != Windows.UI.Xaml.Navigation.NavigationMode.Back)
            return;

        WindowPrivate.Hide();
    }

    public static async void YieldExecute(Action action)
    {
        await Task.Yield();
        action();
    }
}