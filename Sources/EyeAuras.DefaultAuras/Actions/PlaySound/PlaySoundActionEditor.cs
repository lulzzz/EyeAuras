using System.Reactive.Disposables;
using EyeAuras.Shared;
using PoeShared.Audio.ViewModels;
using PoeShared.Scaffolding;
using ReactiveUI;
using System.Linq;
using System;

namespace EyeAuras.DefaultAuras.Actions.PlaySound
{
    internal sealed class PlaySoundActionEditor : AuraPropertiesEditorBase<PlaySoundAction>
    {
        public IAudioNotificationSelectorViewModel AudioNotificationSelector { get; }
        private readonly SerialDisposable activeSourceAnchors = new SerialDisposable();

        public PlaySoundActionEditor(IAudioNotificationSelectorViewModel audioNotificationSelector)
        {
            AudioNotificationSelector = audioNotificationSelector.AddTo(Anchors);
            activeSourceAnchors.AddTo(Anchors);

            this.WhenAnyValue(x => x.Source)
                .Subscribe(HandleSourceChange)
                .AddTo(Anchors);
        }
        
        
        private void HandleSourceChange()
        {
            var sourceAnchors = new CompositeDisposable().AssignTo(activeSourceAnchors);

            if (Source == null)
            {
                return;
            }

            Source.WhenAnyValue(x => x.Notification).Subscribe(x => AudioNotificationSelector.SelectedValue = x).AddTo(sourceAnchors);
            AudioNotificationSelector.WhenAnyValue(x => x.SelectedValue).Subscribe(x => Source.Notification = x).AddTo(sourceAnchors);
        }
    }
}