using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using EyeAuras.Shared;
using EyeAuras.Shared.Services;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using ReactiveUI;

namespace EyeAuras.DefaultAuras.Triggers.WinActive
{
    internal sealed class WinActiveTriggerEditor : AuraPropertiesEditorBase<WinActiveTrigger>
    {
        private readonly SerialDisposable activeSourceAnchors = new SerialDisposable();

        public WinActiveTriggerEditor(
            [NotNull] IWindowSelectorViewModel windowSelector)
        {
            WindowSelector = windowSelector.AddTo(Anchors);
            activeSourceAnchors.AddTo(Anchors);
             
            this.WhenAnyValue(x => x.Source)
                .Subscribe(HandleSourceChange)
                .AddTo(Anchors);
        }

        public IWindowSelectorViewModel WindowSelector { get; }
        
         
        private void HandleSourceChange()
        {
            var sourceAnchors = new CompositeDisposable().AssignTo(activeSourceAnchors);

            if (Source == null)
            {
                return;
            }

            Source.WhenAnyValue(x => x.TargetWindow).Subscribe(x => WindowSelector.TargetWindow = x).AddTo(sourceAnchors);
            WindowSelector.WhenAnyValue(x => x.TargetWindow).Subscribe(x => Source.TargetWindow = x).AddTo(sourceAnchors);
        }
    }
}