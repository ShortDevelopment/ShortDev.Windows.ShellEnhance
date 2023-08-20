using PowerSwitcher.Wrappers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.UI.Xaml.Controls;

namespace ShortDev.ShellEnhance.UI.Flyouts
{
    public sealed partial class EnergyFlyoutPage : Page, IShellEnhanceFlyout
    {
        public EnergyFlyoutPage()
        {
            this.InitializeComponent();

            this.Loaded += EnergyFlyoutPage_Loaded;
        }

        Win32PowSchemasWrapper _wrapper;
        List<PowerSchema> _powerSchemas;
        private void EnergyFlyoutPage_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            _wrapper = new Win32PowSchemasWrapper();

            _powerSchemas = _wrapper.GetCurrentSchemas();
            SelectPowerPlanListView.ItemsSource = _powerSchemas.Select((x) => x.Name);

            for (int i = 0; i < _powerSchemas.Count; i++)
            {
                if (_powerSchemas[i].Guid == _wrapper.GetActiveGuid())
                    SelectPowerPlanListView.SelectedIndex = i;
            }
        }

        public string IconAssetId
            => "battery";

        public Guid IconId
            => new Guid("3623AB2B-C8C7-4762-B658-4C24AD7BED5D");

        private void SelectPowerPlanListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            _wrapper.SetActiveGuid(_powerSchemas[SelectPowerPlanListView.SelectedIndex].Guid);
        }
    }
}

namespace PowerSwitcher.Wrappers
{

    public class Win32PowSchemasWrapper
    {

        public Guid GetActiveGuid()
        {
            Guid activeSchema = Guid.Empty;
            IntPtr guidPtr = IntPtr.Zero;

            try
            {
                var errCode = PowerGetActiveScheme(IntPtr.Zero, out guidPtr);

                if (errCode != 0) { throw new PowerSwitcherWrappersException($"GetActiveGuid() failed with code {errCode}"); }
                if (guidPtr == IntPtr.Zero) { throw new PowerSwitcherWrappersException("GetActiveGuid() returned null pointer for GUID"); }

                activeSchema = (Guid)Marshal.PtrToStructure(guidPtr, typeof(Guid));
            }
            finally
            {
                if (guidPtr != IntPtr.Zero) { LocalFree(guidPtr); }
            }

            return activeSchema;
        }

        public void SetActiveGuid(Guid guid)
        {
            var errCode = PowerSetActiveScheme(IntPtr.Zero, ref guid);
            if (errCode != 0)
                throw new PowerSwitcherWrappersException($"SetActiveGuid() failed with code {errCode}");
        }

        public string GetPowerPlanName(Guid guid)
        {
            string name = string.Empty;

            IntPtr bufferPointer = IntPtr.Zero;
            uint bufferSize = 0;

            try
            {
                var errCode = PowerReadFriendlyName(IntPtr.Zero, ref guid, IntPtr.Zero, IntPtr.Zero, bufferPointer, ref bufferSize);
                if (errCode != 0)
                    throw new PowerSwitcherWrappersException($"GetPowerPlanName() failed when getting buffer size with code {errCode}");

                if (bufferSize <= 0)
                    return String.Empty;
                bufferPointer = Marshal.AllocHGlobal((int)bufferSize);

                errCode = PowerReadFriendlyName(IntPtr.Zero, ref guid, IntPtr.Zero, IntPtr.Zero, bufferPointer, ref bufferSize);
                if (errCode != 0)
                    throw new PowerSwitcherWrappersException($"GetPowerPlanName() failed when getting buffer pointer with code {errCode}");

                name = Marshal.PtrToStringUni(bufferPointer);
            }
            finally
            {
                if (bufferPointer != IntPtr.Zero) { Marshal.FreeHGlobal(bufferPointer); }
            }

            return name;
        }

        private const int ERROR_NO_MORE_ITEMS = 259;
        public List<PowerSchema> GetCurrentSchemas()
        {
            var powerSchemas = getAllPowerSchemaGuids().Select(guid => new PowerSchema(GetPowerPlanName(guid), guid)).ToList();
            return powerSchemas;
        }

        private IEnumerable<Guid> getAllPowerSchemaGuids()
        {
            var schemeGuid = Guid.Empty;

            uint sizeSchemeGuid = (uint)Marshal.SizeOf(typeof(Guid));
            uint schemeIndex = 0;

            while (true)
            {
                uint errCode = PowerEnumerate(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, (uint)AccessFlags.ACCESS_SCHEME, schemeIndex, ref schemeGuid, ref sizeSchemeGuid);
                if (errCode == ERROR_NO_MORE_ITEMS) { yield break; }
                if (errCode != 0) { throw new PowerSwitcherWrappersException($"GetPowerSchemeGUIDs() failed when getting buffer pointer with code {errCode}"); }

                yield return schemeGuid;
                schemeIndex++;
            }
        }

        #region EnumerationEnums
        public enum AccessFlags : uint
        {
            ACCESS_SCHEME = 16,
            ACCESS_SUBGROUP = 17,
            ACCESS_INDIVIDUAL_SETTING = 18
        }
        #endregion


        #region DLL imports

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LocalFree(IntPtr hMem);

        [DllImport("powrprof.dll", EntryPoint = "PowerSetActiveScheme")]
        private static extern uint PowerSetActiveScheme(IntPtr UserPowerKey, ref Guid ActivePolicyGuid);

        [DllImport("powrprof.dll", EntryPoint = "PowerGetActiveScheme")]
        private static extern uint PowerGetActiveScheme(IntPtr UserPowerKey, out IntPtr ActivePolicyGuid);

        [DllImport("powrprof.dll", EntryPoint = "PowerReadFriendlyName")]
        private static extern uint PowerReadFriendlyName(IntPtr RootPowerKey, ref Guid SchemeGuid, IntPtr SubGroupOfPowerSettingsGuid, IntPtr PowerSettingGuid, IntPtr BufferPtr, ref uint BufferSize);

        [DllImport("PowrProf.dll")]
        public static extern UInt32 PowerEnumerate(IntPtr RootPowerKey, IntPtr SchemeGuid, IntPtr SubGroupOfPowerSettingGuid, UInt32 AcessFlags, UInt32 Index, ref Guid Buffer, ref UInt32 BufferSize);

        #endregion
    }

    public class PowerSwitcherWrappersException : Exception
    {
        public PowerSwitcherWrappersException() { }
        public PowerSwitcherWrappersException(string message) : base(message) { }
        public PowerSwitcherWrappersException(string message, Exception inner) : base(message, inner) { }
    }

    public enum PowerPlugStatus { Online, Offline }

    public class PowerSchema
    {
        public Guid Guid { get; }
        public string Name { get; }

        public PowerSchema(string name, Guid guid)
        {
            this.Name = name;
            this.Guid = guid;
        }
    }
}