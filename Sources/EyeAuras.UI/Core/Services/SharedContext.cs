using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DynamicData;
using EyeAuras.Shared;
using EyeAuras.UI.Core.ViewModels;
using JetBrains.Annotations;
using log4net;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Unity;
using System;
using DynamicData.Binding;
using PoeShared;

namespace EyeAuras.UI.Core.Services
{
    internal sealed class SharedContext : DisposableReactiveObject, ISharedContext
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SharedContext));

        public IComplexAuraTrigger SystemTrigger { get; }

        public ObservableCollection<IEyeAuraViewModel> AuraList => auraList;

        private readonly ISourceList<IEyeAuraViewModel> tabsListSource = new SourceList<IEyeAuraViewModel>();
        private readonly ObservableCollectionExtended<IEyeAuraViewModel> auraList;

        public SharedContext(
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            SystemTrigger = new ComplexAuraTrigger().AddTo(Anchors);
            
            auraList = new ObservableCollectionExtended<IEyeAuraViewModel>();
            tabsListSource
                .Connect()
                .DisposeMany()
                .ObserveOn(uiScheduler)
                .Bind(auraList)
                .Subscribe(() => { }, Log.HandleUiException)
                .AddTo(Anchors);
        }
    }
}