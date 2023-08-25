#nullable enable

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Appointments;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Timer = System.Timers.Timer;

namespace ShortDev.Windows.ShellEnhance.UI.Flyouts;

public sealed partial class CalendarFlyoutPage : Page, IShellEnhanceFlyout, INotifyPropertyChanged
{
    public CalendarFlyoutPage()
    {
        InitializeComponent();
        Loaded += CalendarFlyoutPage_Loaded;
    }

    public string IconAssetId
        => "calendar";

    public ushort IconId
        => 0x2023;

    private async void CalendarFlyoutPage_Loaded(object sender, RoutedEventArgs e)
    {
        await OnSelectedDateChangedAsync();
    }

    #region Clock Timer
    readonly Timer timer = new()
    {
        Interval = 10
    };
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        timer.Elapsed += OnTimerTick;
        timer.Enabled = true;
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        timer.Elapsed -= OnTimerTick;
        timer.Enabled = false;
    }

    int _lastSecond = -1;
    void OnTimerTick(object sender, object e)
    {
        var currentSecond = DateTime.Now.Second;
        if (Interlocked.Exchange(ref _lastSecond, currentSecond) == currentSecond)
            return;

        _ = Dispatcher.RunAsync(
                CoreDispatcherPriority.High,
                () => PropertyChanged?.Invoke(this, new(nameof(CurrentTimeFormatted)))
            );
    }
    #endregion

    public string CurrentTimeFormatted => DateTime.Now.ToString("HH:mm:ss");
    public string CurrentDateFormatted => DateTime.Now.ToString("D");
    public string SelectedDateFormatted => SelectedDate.ToString("D");

    AppointmentStore? _appointmentStore;
    DateTimeOffset SelectedDate = DateTimeOffset.Now;
    private async void CalendarView_SelectedDatesChanged(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args)
    {
        if (sender.SelectedDates.Count == 0)
            SelectedDate = DateTimeOffset.Now;
        else
            SelectedDate = sender.SelectedDates[0];

        await OnSelectedDateChangedAsync();
    }

    internal ObservableCollection<AppointmentInfo> Appointments { get; } = new();
    async Task OnSelectedDateChangedAsync()
    {
        _appointmentStore ??= await AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AllCalendarsReadOnly);

        PropertyChanged?.Invoke(this, new(nameof(SelectedDateFormatted)));

        Appointments.Clear();
        foreach (var item in await _appointmentStore.FindAppointmentsAsync(SelectedDate.Date, TimeSpan.FromDays(1)))
        {
            var calendar = await _appointmentStore.GetAppointmentCalendarAsync(item.CalendarId);
            Appointments.Add(new()
            {
                Appointment = item,
                Calendar = calendar
            });
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private async void LaunchCalendarButton_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new("outlookcal:"));
    }

    private async void LaunchClockButton_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new("ms-clock:"));
    }
}

internal sealed class AppointmentInfo
{
    public required Appointment Appointment { get; init; }
    public required AppointmentCalendar Calendar { get; init; }

    public string StartTimeFormatted => Appointment.AllDay ? "All Day" : Appointment.StartTime.ToString("HH:mm");
    public Visibility LocationVisibility => string.IsNullOrEmpty(Appointment.Location) ? Visibility.Collapsed : Visibility.Visible;
}