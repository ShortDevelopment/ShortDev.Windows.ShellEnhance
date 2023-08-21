using System;

namespace ShortDev.Windows.ShellEnhance.UI.Flyouts
{
    internal interface IShellEnhanceFlyout
    {
        string IconAssetId { get; }
        ushort IconId { get; }
    }
}
