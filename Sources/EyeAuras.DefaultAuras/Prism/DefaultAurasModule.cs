using EyeAuras.DefaultAuras.Actions.PlaySound;
using EyeAuras.DefaultAuras.Actions.WinActivate;
using EyeAuras.DefaultAuras.Triggers.Default;
using EyeAuras.DefaultAuras.Triggers.HotkeyIsActive;
using EyeAuras.DefaultAuras.Triggers.WinActive;
using EyeAuras.Shared;
using log4net;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using PoeShared.Wpf.Scaffolding;
using Prism.Ioc;
using Unity;

namespace EyeAuras.DefaultAuras.Prism
{
    public sealed class DefaultAurasModule : DisposableReactiveObject, IDynamicModule
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DefaultAurasModule));

        private readonly IUnityContainer container;

        public DefaultAurasModule(IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            container.RegisterOverlayController();

            var auraManager = container.Resolve<IAuraRegistrator>();
            auraManager.Register<DefaultTrigger>();
            auraManager.Register<WinActiveTrigger>();
            auraManager.Register<HotkeyIsActiveTrigger>();
            auraManager.Register<WinActiveTriggerEditor, WinActiveTrigger>();
            auraManager.Register<DefaultTriggerEditor, DefaultTrigger>();
            auraManager.Register<HotkeyIsActiveTriggerEditor, HotkeyIsActiveTrigger>();
            
            auraManager.Register<PlaySoundAction>();
            auraManager.Register<PlaySoundActionEditor, PlaySoundAction>();
            
            auraManager.Register<WinActivateAction>();
            auraManager.Register<WinActivateActionEditor, WinActivateAction>();
        }
    }
}