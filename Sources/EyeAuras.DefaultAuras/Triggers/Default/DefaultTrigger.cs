using EyeAuras.Shared;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace EyeAuras.DefaultAuras.Triggers.Default
{
    public sealed class DefaultTrigger : AuraTriggerBase<DefaultTriggerProperties>
    {
        public override string TriggerName { get; } = "Fixed Value Trigger";

        public override string TriggerDescription { get; } = "Trigger that is always True or False";

        public DefaultTrigger()
        {
            this.RaiseWhenSourceValue(x => x.TriggerValue, this, x => x.IsActive).AddTo(Anchors);
        }

        protected override void Load(DefaultTriggerProperties source)
        {
            IsActive = source.TriggerValue;
        }

        public bool TriggerValue => IsActive;

        protected override DefaultTriggerProperties Save()
        {
            return new DefaultTriggerProperties
            {
                TriggerValue = IsActive
            };
        }
    }
}