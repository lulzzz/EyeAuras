using System;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using EyeAuras.Shared;
using EyeAuras.Shared.Services;
using EyeAuras.UI.Core.Models;
using JetBrains.Annotations;
using PoeShared;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.UI;
using Prism.Commands;
using ReactiveUI;
using Unity;

namespace EyeAuras.UI.Core.ViewModels
{
    internal sealed class OverlayAuraPropertiesEditorViewModel : AuraPropertiesEditorBase<OverlayAuraModelBase>
    {
        private readonly IAuraRepository repository;
        private readonly IFactory<IPropertyEditorViewModel> propertiesEditorFactory;
        private readonly IScheduler uiScheduler;
        private readonly SerialDisposable activeSourceAnchors = new SerialDisposable();

        private readonly DelegateCommand<object> addTriggerCommand;
        private readonly DelegateCommand<object> addActionCommand;
        
        private ReadOnlyObservableCollection<IPropertyEditorViewModel> triggerEditors;
        private ReadOnlyObservableCollection<IPropertyEditorViewModel> actionEditors;

        public OverlayAuraPropertiesEditorViewModel(
            [NotNull] IAuraRepository repository,
            [NotNull] IFactory<IPropertyEditorViewModel> propertiesEditorFactory,
            [NotNull] IWindowSelectorViewModel windowSelector,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            this.repository = repository;
            this.propertiesEditorFactory = propertiesEditorFactory;
            this.uiScheduler = uiScheduler;
            WindowSelector = windowSelector.AddTo(Anchors);
            activeSourceAnchors.AddTo(Anchors);
            
            addTriggerCommand = new DelegateCommand<object>(AddTriggerCommandExecuted);
            addActionCommand = new DelegateCommand<object>(AddOnEnterActionCommandExecuted);

            repository.KnownEntities
                .ToObservableChangeSet()
                .Filter(x => x is IAuraTrigger)
                .Transform(x => x as IAuraTrigger)
                .Bind(out var knownTriggers)
                .Subscribe()
                .AddTo(Anchors);
            KnownTriggers = knownTriggers;
            
            repository.KnownEntities
                .ToObservableChangeSet()
                .Filter(x => x is IAuraAction)
                .Transform(x => x as IAuraAction)
                .Bind(out var knownActions)
                .Subscribe()
                .AddTo(Anchors);
            KnownActions = knownActions;
             
            this.WhenAnyValue(x => x.Source)
                .Subscribe(HandleSourceChange)
                .AddTo(Anchors);
        }

        public IWindowSelectorViewModel WindowSelector { get; }
        
        public ReadOnlyObservableCollection<IAuraTrigger> KnownTriggers { get; }
        
        public ReadOnlyObservableCollection<IAuraAction> KnownActions { get; }

        public ReadOnlyObservableCollection<IPropertyEditorViewModel> TriggerEditors
        {
            get => triggerEditors;
            private set => this.RaiseAndSetIfChanged(ref triggerEditors, value);
        }

        public ReadOnlyObservableCollection<IPropertyEditorViewModel> ActionEditors
        {
            get => actionEditors;
            private set => this.RaiseAndSetIfChanged(ref actionEditors, value);
        }
        
        public ICommand AddTriggerCommand => addTriggerCommand;
        
        public ICommand AddActionCommand => addActionCommand;
        
        private void AddOnEnterActionCommandExecuted(object obj)
        {
            Guard.ArgumentNotNull(obj, nameof(obj));

            var model = repository.CreateModel<IAuraAction>(obj.GetType(), Source.Context);
            Source.OnEnterActions.Add(model);
        }

        private void AddTriggerCommandExecuted(object obj)
        {
            Guard.ArgumentNotNull(obj, nameof(obj));

            var trigger = repository.CreateModel<IAuraTrigger>(obj.GetType(), Source.Context);
            Source.Triggers.Add(trigger);
        }
         
        private void HandleSourceChange()
        {
            var sourceAnchors = new CompositeDisposable().AssignTo(activeSourceAnchors);

            if (Source == null)
            {
                return;
            }

            Disposable.Create(() =>
            {
                Source = null;
                ActionEditors = null;
                TriggerEditors = null;
            }).AddTo(sourceAnchors);
            
            Source.WhenAnyValue(x => x.TargetWindow).Subscribe(x => WindowSelector.TargetWindow = x).AddTo(sourceAnchors);
            WindowSelector.WhenAnyValue(x => x.TargetWindow).Subscribe(x => Source.TargetWindow = x).AddTo(sourceAnchors);
            
            Source.Overlay.WhenAnyValue(x => x.AttachedWindow).Subscribe(x => WindowSelector.ActiveWindow = x).AddTo(sourceAnchors);
            WindowSelector.WhenAnyValue(x => x.ActiveWindow).Subscribe(x => Source.Overlay.AttachedWindow = x).AddTo(sourceAnchors);

            Source.Triggers
                .ToObservableChangeSet()
                .Transform(
                    x =>
                    {
                        var editor = propertiesEditorFactory.Create();
                        var closeController = new RemoveItemController<IAuraTrigger>(x, Source.Triggers);
                        editor.SetCloseController(closeController);
                        editor.Value = x;
                        return editor;
                    })
                .DisposeMany()
                .ObserveOn(uiScheduler)
                .Bind(out var triggersSource)
                .Subscribe()
                .AddTo(Anchors);

            TriggerEditors = triggersSource;
            
            Source.OnEnterActions
                .ToObservableChangeSet()
                .Transform(
                    x =>
                    {
                        var editor = propertiesEditorFactory.Create();
                        var closeController = new RemoveItemController<IAuraAction>(x, Source.OnEnterActions);
                        editor.SetCloseController(closeController);
                        editor.Value = x;
                        return editor;
                    })
                .DisposeMany()
                .ObserveOn(uiScheduler)
                .Bind(out var onEnterActionsSource)
                .Subscribe()
                .AddTo(Anchors);
            ActionEditors = onEnterActionsSource;
        }
    }
}