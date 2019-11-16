using Prism.Modularity;

namespace EyeAuras.UI.MainWindow.ViewModels
{
    internal sealed class PrismModuleStatus
    {
        public PrismModuleStatus(IModuleInfo moduleInfo)
        {
            Info = moduleInfo;
        }

        public IModuleInfo Info { get; }

        public string ModuleName => Info.ModuleName;

        public bool IsLoaded => Info.State == ModuleState.Initialized;

        private bool Equals(PrismModuleStatus other)
        {
            return Equals(Info, other.Info);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is PrismModuleStatus other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Info != null
                ? Info.GetHashCode()
                : 0;
        }
    }
}