using EyeAuras.Shared;
using EyeAuras.Shared.Services;
using log4net;
using PoeShared.Native;
using PoeShared.Scaffolding;

namespace EyeAuras.DefaultAuras.Actions.WinActivate
{
    internal sealed class WinActivateAction : AuraActionBase<WinActivateActionProperties>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WinActivateAction));

        public WinActivateAction(IWindowSelectorViewModel windowSelector)
        {
            WindowSelector = windowSelector;
            this.RaiseWhenSourceValue(x => x.TargetWindow, windowSelector, x => x.TargetWindow).AddTo(Anchors);
        }

        public IWindowSelectorViewModel WindowSelector { get; }

        public WindowMatchParams TargetWindow
        {
            get => WindowSelector.TargetWindow;
            set => WindowSelector.TargetWindow = value;
        }

        public override string ActionName { get; } = "Win Activate";

        public override string ActionDescription { get; } = "activates selected window";

        protected override void Load(WinActivateActionProperties source)
        {
            TargetWindow = source.WindowMatchParams;
        }

        protected override WinActivateActionProperties Save()
        {
            return new WinActivateActionProperties
            {
                WindowMatchParams = TargetWindow
            };
        }

        public override void Execute()
        {
            var activeWindow = WindowSelector.ActiveWindow;
            if (activeWindow == null)
            {
                return;
            }

            Log.Debug($"Bringing window {activeWindow} to foreground");
            UnsafeNative.SetForegroundWindow(activeWindow.Handle);
        }
    }
}