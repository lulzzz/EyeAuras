using System;
using System.Collections.ObjectModel;
using JetBrains.Annotations;

namespace EyeAuras.Shared
{
    public interface IAuraRepository
    {
        ReadOnlyObservableCollection<IAuraModel> KnownEntities { [NotNull] get; }

        [NotNull]
        TAuraBaseType CreateModel<TAuraBaseType>([NotNull] Type auraModelType) where TAuraBaseType : IAuraModel;

        [NotNull]
        TAuraBaseType CreateModel<TAuraBaseType>([NotNull] IAuraProperties properties);

        [CanBeNull]
        IAuraPropertiesEditor CreateEditor([NotNull] IAuraModel model);
    }
}