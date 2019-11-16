namespace EyeAuras.Shared
{
    public abstract class AuraTriggerBase<TAuraProperties> : AuraModelBase<TAuraProperties>, IAuraTrigger where TAuraProperties : class, IAuraProperties
    {
        private bool isActive;

        public abstract string TriggerName { get; }

        public abstract string TriggerDescription { get; }

        public bool IsActive
        {
            get => isActive;
            set => RaiseAndSetIfChanged(ref isActive, value);
        }
    }
}