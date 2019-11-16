using System;
using PoeShared.Scaffolding;

namespace EyeAuras.Shared
{
    public abstract class AuraModelBase : DisposableReactiveObject, IAuraModel
    {
        public IAuraProperties Properties
        {
            get => SaveProperties();
            set => LoadProperties(value);
        }

        protected abstract void LoadProperties(IAuraProperties source);

        protected abstract IAuraProperties SaveProperties();
    }

    public abstract class AuraModelBase<T> : AuraModelBase, IAuraModel<T> where T : class, IAuraProperties
    {
        public new T Properties
        {
            get => Save();
            set => Load(value);
        }

        protected abstract void Load(T source);

        protected abstract T Save();

        protected override IAuraProperties SaveProperties()
        {
            return Save();
        }

        protected override void LoadProperties(IAuraProperties source)
        {
            if (!(source is T typedSource))
            {
                throw new ArgumentException(
                    $"Invalid Properties source, expected value of type {typeof(T)}, got {(source == null ? "null" : source.GetType().FullName)} instead");
            }

            Load(typedSource);
        }
    }
}