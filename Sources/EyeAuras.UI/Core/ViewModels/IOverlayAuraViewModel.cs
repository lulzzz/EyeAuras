using System.Collections.ObjectModel;
using System.Windows.Input;
using EyeAuras.Shared;
using EyeAuras.UI.Core.Models;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.Core.ViewModels
{
    internal interface IOverlayAuraViewModel : IDisposableReactiveObject
    {
        string DefaultTabName { [NotNull] get; }

        ICommand AddTriggerCommand { [NotNull] get; }
        
        ICommand AddActionCommand { [NotNull] get; }

        ICommand RenameCommand { [NotNull] get; }

        IPropertyEditorViewModel GeneralEditor { [NotNull] get; }

        ReadOnlyObservableCollection<IAuraTrigger> KnownTriggers { [NotNull] get; }
        
        ReadOnlyObservableCollection<IAuraAction> KnownActions { [NotNull] get; }

        ReadOnlyObservableCollection<IPropertyEditorViewModel> TriggerEditors { [NotNull] get; }

        IOverlayAuraModel Model { [NotNull] get; }

        string TabName { [NotNull] get; }

        bool IsFlipped { get; set; }

        bool IsSelected { get; set; }
    }
}