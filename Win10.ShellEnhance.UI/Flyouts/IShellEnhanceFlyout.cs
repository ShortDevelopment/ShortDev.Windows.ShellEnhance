using System;

namespace ShortDev.ShellEnhance.UI.Flyouts
{
    internal interface IShellEnhanceFlyout
    {
        string IconAssetId { get; }
        Guid IconId { get; }
    }
}
