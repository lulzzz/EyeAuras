using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using EyeAuras.Shared;
using EyeAuras.UI.Core.Models;
using JetBrains.Annotations;
using log4net;
using PoeShared;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.UI;
using Prism.Commands;
using ReactiveUI;
using Unity;

namespace EyeAuras.UI.Core.ViewModels
{
    internal sealed class OverlayAuraViewModel : DisposableReactiveObject, IOverlayAuraViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(OverlayAuraViewModel));
        private readonly DelegateCommand<object> addTriggerCommand;
        private readonly DelegateCommand<object> addActionCommand;

        private readonly ReadOnlyObservableCollection<IAuraTrigger> knownTriggers;
        private readonly ReadOnlyObservableCollection<IAuraAction> knownActions;

        private readonly IAuraRepository repository;
        private readonly Fallback<string> tabName = new Fallback<string>();
        private bool isFlipped;
        private bool isSelected;

        public OverlayAuraViewModel(
            [NotNull] IOverlayAuraModel auraModel,
            [NotNull] IFactory<IPropertyEditorViewModel> propertiesEditorFactory,
            [NotNull] IAuraRepository repository,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Model = auraModel.AddTo(Anchors);
            this.repository = repository;
            tabName.SetDefaultValue(auraModel.Name);
            RenameCommand = new DelegateCommand<string>(RenameCommandExecuted);

            repository.KnownEntities
                .ToObservableChangeSet()
                .Filter(x => x is IAuraTrigger)
                .Transform(x => x as IAuraTrigger)
                .Bind(out knownTriggers)
                .Subscribe()
                .AddTo(Anchors);
            
            repository.KnownEntities
                .ToObservableChangeSet()
                .Filter(x => x is IAuraAction)
                .Transform(x => x as IAuraAction)
                .Bind(out knownActions)
                .Subscribe()
                .AddTo(Anchors);

            addTriggerCommand = new DelegateCommand<object>(AddTriggerCommandExecuted);
            addActionCommand = new DelegateCommand<object>(AddOnEnterActionCommandExecuted);

            auraModel.Triggers
                .ToObservableChangeSet()
                .Transform(
                    x =>
                    {
                        var editor = propertiesEditorFactory.Create();
                        var closeController = new RemoveItemController<IAuraTrigger>(x, auraModel.Triggers);
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
            
            auraModel.OnEnterActions
                .ToObservableChangeSet()
                .Transform(
                    x =>
                    {
                        var editor = propertiesEditorFactory.Create();
                        var closeController = new RemoveItemController<IAuraAction>(x, auraModel.OnEnterActions);
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

            GeneralEditor = propertiesEditorFactory.Create();
            GeneralEditor.Value = auraModel;

            this.RaiseWhenSourceValue(x => x.TabName, tabName, x => x.Value).AddTo(Anchors);
            this.RaiseWhenSourceValue(x => x.DefaultTabName, tabName, x => x.DefaultValue).AddTo(Anchors);
            Model.WhenAnyValue(x => x.Name)
                .Subscribe(x => tabName.SetValue(x))
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.TabName)
                .Subscribe(x => Model.Name = x)
                .AddTo(Anchors);
        }

        public string DefaultTabName => tabName.DefaultValue;

        public ICommand AddTriggerCommand => addTriggerCommand;
        
        public ICommand AddActionCommand => addActionCommand;

        public ICommand RenameCommand { [NotNull] get; }

        public IPropertyEditorViewModel GeneralEditor { get; }

        public ReadOnlyObservableCollection<IAuraTrigger> KnownTriggers => knownTriggers;
        
        public ReadOnlyObservableCollection<IAuraAction> KnownActions => knownActions;

        public ReadOnlyObservableCollection<IPropertyEditorViewModel> TriggerEditors { get; }
            
        public ReadOnlyObservableCollection<IPropertyEditorViewModel> ActionEditors { get; }

        public IOverlayAuraModel Model { get; }

        public string TabName => tabName.Value;

        public bool IsFlipped
        {
            get => isFlipped;
            set => RaiseAndSetIfChanged(ref isFlipped, value);
        }

        public bool IsSelected
        {
            get => isSelected;
            set => RaiseAndSetIfChanged(ref isSelected, value);
        }
        
        private void AddOnEnterActionCommandExecuted(object obj)
        {
            Guard.ArgumentNotNull(obj, nameof(obj));

            var model = repository.CreateModel<IAuraAction>(obj.GetType());
            Model.OnEnterActions.Add(model);
        }

        private void AddTriggerCommandExecuted(object obj)
        {
            Guard.ArgumentNotNull(obj, nameof(obj));

            var trigger = repository.CreateModel<IAuraTrigger>(obj.GetType());
            Model.Triggers.Add(trigger);
        }

        private void RenameCommandExecuted(string value)
        {
            if (IsFlipped)
            {
                if (value == null)
                {
                    // Cancel
                }
                else if (string.IsNullOrWhiteSpace(value))
                {
                    RenameTabTo(default);
                }
                else
                {
                    RenameTabTo(value);
                }
            }

            IsFlipped = !IsFlipped;
        }

        private void RenameTabTo(string newTabNameOrDefault)
        {
            if (newTabNameOrDefault == tabName.Value)
            {
                return;
            }

            var previousValue = tabName.Value;
            tabName.SetValue(newTabNameOrDefault);
            Log.Debug($"Changed name of tab {tabName.DefaultValue}, {previousValue} => {tabName.Value}");
        }

        public override string ToString()
        {
            return new {TabName, DefaultTabName, IsSelected, IsFlipped}.DumpToTextRaw();
        }
    }
}