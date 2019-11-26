using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using PoeShared.Scaffolding;
using System;

namespace EyeAuras.Shared
{
    public abstract class ComplexAuraAction<T> : AuraActionBase<T>, IComplexAuraAction where T : class, IAuraProperties
    {
        private readonly ISourceList<IAuraAction> actions = new SourceList<IAuraAction>();
        
        protected ComplexAuraAction()
        {
            var actionList = new ObservableCollectionExtended<IAuraAction>();
            actions
                .Connect()
                .DisposeMany()
                .Bind(actionList)
                .Subscribe()
                .AddTo(Anchors);
            Actions = actionList;
        }

        public override void Execute()
        {
            Actions.ForEach(x => x.Execute());
        }

        public override string ActionName { get; } = "Multi-action";

        public override string ActionDescription { get; } =
            "Action which combines multiple child action into single one";

        public ObservableCollection<IAuraAction> Actions { get; }
    }
}