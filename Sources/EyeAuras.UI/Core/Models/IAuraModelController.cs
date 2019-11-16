using DynamicData.Annotations;
using PoeShared.Native;

namespace EyeAuras.UI.Core.Models
{
    internal interface IAuraModelController
    {
        bool IsEnabled { get; set; }
        
        ICloseController CloseController { [CanBeNull] get; }

        string Name { [CanBeNull] get; [CanBeNull] set; }
    }
}