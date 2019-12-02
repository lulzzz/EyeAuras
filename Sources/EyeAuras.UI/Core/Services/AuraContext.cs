using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using DynamicData;
using EyeAuras.Shared.Services;
using JetBrains.Annotations;
using log4net;
using PoeShared;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.Core.Services
{
    [UsedImplicitly]
    internal sealed class AuraContext : DisposableReactiveObject, IAuraContext
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(IAuraContext));

        private readonly ISourceList<WindowMatchParams> auraWindows = new SourceList<WindowMatchParams>();
        
        public AuraContext()
        {
            auraWindows
                .Connect()
                .Bind(out var auraWindowsSource)
                .Subscribe()
                .AddTo(Anchors);
            AuraWindows = auraWindowsSource;
        }

        public ReadOnlyObservableCollection<WindowMatchParams> AuraWindows { get; }
        
        public IDisposable RegisterWindow(WindowMatchParams windowDescription)
        {
            Guard.ArgumentIsTrue(!windowDescription.IsEmpty, nameof(windowDescription));
            
            Log.Debug($"Registering new Aura window {windowDescription}, window list: {(AuraWindows.Any() ?  "\n\t" + auraWindows.Items.DumpToTable() : "Empty")}");
            auraWindows.Add(windowDescription);

            return Disposable.Create(() =>
            {
                Log.Debug($"Unregistering Aura window {windowDescription}, window list: {(AuraWindows.Any() ?  "\n\t" + auraWindows.Items.DumpToTable() : "Empty")}");
                auraWindows.Remove(windowDescription);
            });
        }
    }
}