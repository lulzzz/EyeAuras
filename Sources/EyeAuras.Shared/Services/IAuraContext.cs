using System;
using System.Collections.ObjectModel;
using JetBrains.Annotations;

namespace EyeAuras.Shared.Services
{
    public interface IAuraContext
    {
            ReadOnlyObservableCollection<WindowMatchParams> AuraWindows { [NotNull] get; }

        [NotNull]
        IDisposable RegisterWindow(WindowMatchParams windowDescription);
    }
}