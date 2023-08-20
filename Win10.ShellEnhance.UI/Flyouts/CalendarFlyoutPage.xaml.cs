using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.ApplicationModel.Appointments;
using Windows.UI.Xaml.Controls;

namespace ShortDev.ShellEnhance.UI.Flyouts;

public sealed partial class CalendarFlyoutPage : Page, IShellEnhanceFlyout, INotifyPropertyChanged
{
    public CalendarFlyoutPage()
    {
        InitializeComponent();
        Loaded += CalendarFlyoutPage_Loaded;
    }

    public string IconAssetId
        => "calendar";

    public Guid IconId
        => new("DB805D60-B947-4CEE-B62E-383DA470570F");

    private async void CalendarFlyoutPage_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
        await OnSelectedDateChangedAsync();
    }

    public string CurrentDateFormatted => DateTime.Now.ToString("D");

    AppointmentStore? _appointmentStore;
    DateTimeOffset SelectedDate = DateTimeOffset.Now;

    private async void CalendarView_SelectedDatesChanged(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args)
    {
        if (sender.SelectedDates.Count == 0)
            return;

        SelectedDate = sender.SelectedDates[0];
        await OnSelectedDateChangedAsync();
    }

    public ObservableCollection<Appointment> Appointments { get; } = new();
    async Task OnSelectedDateChangedAsync()
    {
        _appointmentStore ??= await AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AllCalendarsReadOnly);

        PropertyChanged?.Invoke(this, new(nameof(SelectedDate)));

        Appointments.Clear();
        foreach (var item in await _appointmentStore.FindAppointmentsAsync(SelectedDate, TimeSpan.FromDays(1)))
        {
            Appointments.Add(item);
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
}