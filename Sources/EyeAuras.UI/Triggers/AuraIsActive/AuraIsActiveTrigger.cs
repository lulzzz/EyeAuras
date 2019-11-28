using EyeAuras.Shared;
using EyeAuras.UI.Core.Services;
using log4net;
using ReactiveUI;
using System;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using EyeAuras.UI.Core.ViewModels;
using JetBrains.Annotations;
using PoeShared;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.Triggers.AuraIsActive
{
    internal sealed class AuraIsActiveTrigger : AuraTriggerBase<AuraIsActiveTriggerProperties>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AuraIsActiveTrigger));

        private string auraId;
        private IEyeAuraViewModel aura;

        public AuraIsActiveTrigger([NotNull] ISharedContext sharedContext)
        {
            Observable.Merge(
                    this.WhenAnyValue(x => x.AuraId).ToUnit(),
                    sharedContext.AuraList.ToObservableChangeSet().SkipInitial().ToUnit(),
                    sharedContext.AuraList.ToObservableChangeSet().SkipInitial().WhenPropertyChanged(x => x.Id).ToUnit())
                .StartWithDefault()
                .Select(x => sharedContext.AuraList.FirstOrDefault(y => y.Id == AuraId))
                .Subscribe(x => Aura = x)
                .AddTo(Anchors);
            
            this.WhenAnyValue(x => x.Aura)
                .Select(x => x == null 
                    ? Observable.Return(false) 
                    : x.WhenAnyValue(y => y.IsActive).Do(y => Log.Debug($"Child Aura {x.TabName}({x.Id}) IsActive changed to {x.IsActive}")))
                .Switch()
                .Subscribe(isActive => IsActive = isActive, Log.HandleUiException)
                .AddTo(Anchors);
        }

        public IEyeAuraViewModel Aura
        {
            get => aura;
            private set => this.RaiseAndSetIfChanged(ref aura, value);
        }

        public string AuraId    
        {
            get => auraId;
            set => this.RaiseAndSetIfChanged(ref auraId, value);
        }

        public override string TriggerName { get; } = "Aura Is Active";

        public override string TriggerDescription { get; } = "Checks whether specified Aura is active or not";

        protected override void Load(AuraIsActiveTriggerProperties source)
        {
            AuraId = source.AuraId;
        }

        protected override AuraIsActiveTriggerProperties Save()
        {
            return new AuraIsActiveTriggerProperties
            {
                AuraId = auraId
            };
        }
    }
}