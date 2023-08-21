using ShortDev.Windows.ShellEnhance.UI.Flyouts;
using ShortDev.Uwp.FullTrust.Xaml;
using System;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Win32.Foundation;
using WinUI.Interop.CoreWindow;
using Windows.UI.Xaml.Navigation;

namespace ShortDev.Windows.ShellEnhance.UI;

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
        PostMessage((HWND)hwnd, 0x270, 0, 1);

        WindowPrivate = (IWindowPrivate)(object)Window;
        WindowPrivate.MoveWindow(0, 0, 350, 650);
        WindowPrivate.Hide();

        Frame = new();
        Frame.Navigate(typeof(Page));
        Frame.Navigated += Frame_Navigated;
        Window.Content = Frame;

        RegisterFlyout<CalendarFlyoutPage>();
        RegisterFlyout<EnergyFlyoutPage>();

        Window.Dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessUntilQuit);
    }

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

        if (Frame.CanGoBack)
            Frame.GoBack();
    }

    private static void Frame_Navigated(object sender, NavigationEventArgs e)
    {
        if (e.NavigationMode != NavigationMode.Back)
            return;

        WindowPrivate.Hide();
    }

    public static async void YieldExecute(Action action)
    {
        await Task.Yield();
        action();
    }
}