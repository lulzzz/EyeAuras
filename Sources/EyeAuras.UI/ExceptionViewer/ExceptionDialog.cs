using System;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Input;
using log4net;
using PoeShared;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;

namespace EyeAuras.UI.ExceptionViewer
{
    internal sealed class ExceptionDialog : DisposableReactiveObject
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ExceptionDialog));
        
        private string exceptionText;
        private ExceptionDialogConfig config;
        
        private readonly SerialDisposable activeWindowAnchors = new SerialDisposable();

        public ExceptionDialog()
        {
            activeWindowAnchors.AddTo(Anchors);
            
            this.RaiseWhenSourceValue(x => x.Title, this, x => x.Config).AddTo(Anchors);
            this.RaiseWhenSourceValue(x => x.AppName, this, x => x.AppName).AddTo(Anchors);
            
            CloseCommand = CommandWrapper.Create(() => activeWindowAnchors.Disposable = null);
        }

        public ExceptionDialogConfig Config
        {
            get => config;
            set => this.RaiseAndSetIfChanged(ref config, value);
        }

        public string Title => config.Title;

        public string AppName => config.AppName;
        
        public ICommand CloseCommand { get; }
        
        public string ExceptionText
        {
            get => exceptionText;
            private set => this.RaiseAndSetIfChanged(ref exceptionText, value);
        }

        public void Show(Exception exception)
        {
            Guard.ArgumentNotNull(exception, nameof(exception));

            ExceptionText = exception.ToString();
            
            var windowAnchors = new CompositeDisposable().AssignTo(activeWindowAnchors);

            var window = new ExceptionDialogView
            {
                Owner = Application.Current.MainWindow,
                DataContext = this
            };
            
            Disposable.Create(
                    () =>
                    {
                        Log.Debug($"Closing ExceptionViewer, value: {exception}");
                        window.Close();
                    })
                .AddTo(windowAnchors);
            
            Log.Debug($"Showing ExceptionViewer, value: {exception}");
            window.ShowDialog();
        }
    }
}