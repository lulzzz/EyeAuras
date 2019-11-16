using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using EyeAuras.Shared;
using EyeAuras.Shared.Services;
using EyeAuras.UI.Core.Models;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using ReactiveUI;

namespace EyeAuras.UI.Core.ViewModels
{
    internal sealed class OverlayAuraPropertiesEditorViewModel : AuraPropertiesEditorBase<OverlayAuraModelBase>
    {
        private readonly SerialDisposable activeSourceAnchors = new SerialDisposable();

        public OverlayAuraPropertiesEditorViewModel(
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
            
            Source.Overlay.WhenAnyValue(x => x.AttachedWindow).Subscribe(x => WindowSelector.ActiveWindow = x).AddTo(sourceAnchors);
            WindowSelector.WhenAnyValue(x => x.ActiveWindow).Subscribe(x => Source.Overlay.AttachedWindow = x).AddTo(sourceAnchors);
        }
    }
}