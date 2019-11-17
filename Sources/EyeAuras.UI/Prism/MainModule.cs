using System.Reactive.Disposables;
using EyeAuras.Shared;
using EyeAuras.UI.Core.Models;
using EyeAuras.UI.Core.ViewModels;
using log4net;
using PoeShared;
using PoeShared.Scaffolding;
using PoeShared.Squirrel.Prism;
using PoeShared.Wpf.Scaffolding;
using Prism.Ioc;
using Prism.Modularity;
using Unity;

namespace EyeAuras.UI.Prism
{
    internal sealed class MainModule : IModule
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MainModule));

        private readonly IUnityContainer container;

        public MainModule(IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            container.AddExtension(new UpdaterRegistrations());
            container.AddExtension(new UiRegistrations());
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            container.RegisterOverlayController();

            var auraManager = container.Resolve<IAuraRegistrator>();

            auraManager.Register<OverlayAuraPropertiesEditorViewModel, OverlayAuraModelBase>();
        }
    }
}