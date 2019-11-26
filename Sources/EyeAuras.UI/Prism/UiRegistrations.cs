using EyeAuras.Shared;
using EyeAuras.Shared.Services;
using EyeAuras.UI.Core.Models;
using EyeAuras.UI.Core.Services;
using EyeAuras.UI.Core.ViewModels;
using EyeAuras.UI.MainWindow.Models;
using EyeAuras.UI.MainWindow.ViewModels;
using EyeAuras.UI.Overlay.ViewModels;
using EyeAuras.UI.Prism.Modularity;
using EyeAuras.UI.RegionSelector.Services;
using EyeAuras.UI.RegionSelector.ViewModels;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using Unity;
using Unity.Extension;

namespace EyeAuras.UI.Prism
{
    internal sealed class UiRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterSingleton<AuraRepository>(typeof(IAuraRepository), typeof(IAuraRegistrator))
                .RegisterSingleton<MainWindowBlocksService>(typeof(IMainWindowBlocksProvider), typeof(IMainWindowBlocksRepository))
                .RegisterSingleton<IWindowListProvider, WindowListProvider>()
                .RegisterSingleton<ISharedContext, MainWindowSharedContext>()
                .RegisterSingleton<IRegionSelectorService, RegionSelectorService>()
                .RegisterSingleton<IUniqueIdGenerator, UniqueIdGenerator>()
                .RegisterSingleton<IPrismModuleStatusViewModel, PrismModuleStatusViewModel>()
                .RegisterSingleton<MainWindowViewModel>(typeof(IMainWindowViewModel));

            Container
                .RegisterType<ISelectionAdornerViewModel, SelectionAdornerViewModel>()
                .RegisterType<IWindowSelectorViewModel, WindowSelectorViewModel>()
                .RegisterType<IMessageBoxViewModel, MessageBoxViewModel>()
                .RegisterType<IOverlayAuraModel, OverlayAuraModelBase>()
                .RegisterType<IRegionSelectorViewModel, RegionSelectorViewModel>()
                .RegisterType<IPropertyEditorViewModel, PropertyEditorViewModel>()
                .RegisterType<IOverlayAuraViewModel, OverlayAuraViewModel>()
                .RegisterType<IEyeOverlayViewModel, EyeOverlayViewModel>();

            Container.RegisterSingleton<IConfigProvider, ConfigProviderFromFile>();
        }
    }
}