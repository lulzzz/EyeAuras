using System.Windows.Input;
using EyeAuras.Shared;
using EyeAuras.UI.Core.Models;
using JetBrains.Annotations;
using PoeShared.Native;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.Core.ViewModels
{
    internal interface IEyeAuraViewModel : IDisposableReactiveObject
    {
        string Id { [NotNull] get; }
        
        string TabName { [NotNull] get; }
        
        bool IsSelected { get; set; }
        
        bool IsFlipped { get; set; }
        
        string DefaultTabName { [NotNull] get; }
        
        bool IsActive { get; }
        
        bool IsEnabled { get; set; }
        
        ICommand RenameCommand { [NotNull] get; }

        OverlayAuraProperties Properties { get; }

        void SetCloseController([NotNull] ICloseController closeController);
    }
}