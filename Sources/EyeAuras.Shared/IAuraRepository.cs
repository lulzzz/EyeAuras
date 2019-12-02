using System;
using System.Collections.ObjectModel;
using EyeAuras.Shared.Services;
using JetBrains.Annotations;

namespace EyeAuras.Shared
{
    public interface IAuraRepository
    {
        ReadOnlyObservableCollection<IAuraModel> KnownEntities { [NotNull] get; }

        [NotNull]
        TAuraBaseType CreateModel<TAuraBaseType>([NotNull] Type auraModelType, [NotNull] IAuraContext context) where TAuraBaseType : IAuraModel;
        
        [NotNull]
        TAuraBaseType CreateModel<TAuraBaseType>([NotNull] IAuraProperties properties);

        [CanBeNull]
        IAuraPropertiesEditor CreateEditor([NotNull] IAuraModel model);
    }
}