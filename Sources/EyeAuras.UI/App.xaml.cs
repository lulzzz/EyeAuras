using System;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using EyeAuras.UI.ExceptionViewer;
using EyeAuras.UI.Prism;
using log4net;
using PoeShared;
using PoeShared.Native;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace EyeAuras.UI
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly CompositeDisposable anchors = new CompositeDisposable();
        private readonly EyeAurasBootstrapper aurasBootstrapper;

        public App()
        {
            try
            {
                var arguments = Environment.GetCommandLineArgs();
                AppArguments.Instance.AppName = "EyeAuras";

                if (!AppArguments.Parse(arguments))
                {
                    SharedLog.Instance.InitializeLogging("Startup", AppArguments.Instance.AppName);
                    throw new ApplicationException($"Failed to parse command line args: {string.Join(" ", arguments)}");
                }

                InitializeLogging();

                Log.Debug($"[App..ctor] Arguments: {arguments.DumpToText()}");
                Log.Debug($"[App..ctor] Parsed args: {AppArguments.Instance.DumpToText()}");
                Log.Debug($"[App..ctor] OS Version: {Environment.OSVersion}, is64bit: {Environment.Is64BitProcess} (OS: {Environment.Is64BitOperatingSystem})");
                Log.Debug($"[App..ctor] Is Elevated: {AppArguments.Instance.IsElevated}");
                Log.Debug($"[App..ctor] Culture: {Thread.CurrentThread.CurrentCulture}, UICulture: {Thread.CurrentThread.CurrentUICulture}");

                Log.Debug($"[App..ctor] UI Scheduler: {RxApp.MainThreadScheduler}");
                RxApp.MainThreadScheduler = DispatcherScheduler.Current;
                RxApp.TaskpoolScheduler = TaskPoolScheduler.Default;
                Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                Log.Debug($"[App..ctor] New UI Scheduler: {RxApp.MainThreadScheduler}");
                
                Log.Debug($"[App..ctor] Configuring AllowSetForegroundWindow permissions");
                UnsafeNative.AllowSetForegroundWindow();
                
                aurasBootstrapper = new EyeAurasBootstrapper();
                Disposable.Create(
                        () =>
                        {
                            Log.Info("Disposing bootstrapper...");
                            aurasBootstrapper.Dispose();
                        })
                    .AddTo(anchors);
            }
            catch (Exception ex)
            {
                Log.HandleException(ex);
                throw;
            }
        }

        private static ILog Log => SharedLog.Instance.Log;

        private void SingleInstanceValidationRoutine()
        {
            var mutexId = $"{AppArguments.Instance.AppName}{(AppArguments.Instance.IsDebugMode ? "DEBUG" : "RELEASE")}{{B74259C4-0F20-4EC2-9538-BA8A176FDF7D}}";
            Log.Debug($"[App] Acquiring mutex {mutexId}...");
            var mutex = new Mutex(true, mutexId);
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                Log.Debug($"[App] Mutex {mutexId} was successfully acquired");

                AppDomain.CurrentDomain.DomainUnload += delegate
                {
                    Log.Debug($"[App.DomainUnload] Detected DomainUnload, disposing mutex {mutexId}");
                    mutex.ReleaseMutex();
                    Log.Debug("[App.DomainUnload] Mutex was successfully disposed");
                };
            }
            else
            {
                Log.Warn($"[App] Application is already running, mutex: {mutexId}");
                ShowShutdownWarning();
            }
        }

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ReportCrash(e.ExceptionObject as Exception, "CurrentDomainUnhandledException");
        }

        private void DispatcherOnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            ReportCrash(e.Exception, "DispatcherUnhandledException");
        }

        private void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            ReportCrash(e.Exception, "TaskSchedulerUnobservedTaskException");
        }

        private void InitializeLogging()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            Dispatcher.CurrentDispatcher.UnhandledException += DispatcherOnUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
            RxApp.DefaultExceptionHandler = SharedLog.Instance.Errors;
            if (AppArguments.Instance.IsDebugMode)
            {
                SharedLog.Instance.InitializeLogging("Debug", AppArguments.Instance.AppName);
            }
            else
            {
                SharedLog.Instance.InitializeLogging("Release", AppArguments.Instance.AppName);
            }

            var logFileConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config");
            SharedLog.Instance.LoadLogConfiguration(new FileInfo(logFileConfigPath));
            SharedLog.Instance.AddTraceAppender().AddTo(anchors);
            SharedLog.Instance.Errors.Subscribe(
                ex =>
                {
                    ReportCrash(ex);
                }).AddTo(anchors);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Log.Debug("Application startup detected");

            SingleInstanceValidationRoutine();

            Log.Info("Starting bootstrapper...");
            aurasBootstrapper.Run();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            Log.Debug("Application exit detected");
            anchors.Dispose();
        }

        private void ShowShutdownWarning()
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName();
            var window = MainWindow;
            var title = $"{assemblyName.Name} v{assemblyName.Version}";
            var message = "Application is already running !";
            if (window != null)
            {
                MessageBox.Show(window, message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            Log.Warn("Shutting down...");
            Environment.Exit(0);
        }
        
        private void ReportCrash(Exception exception, string developerMessage = "")
        {
            Log.Error($"Unhandled application exception({developerMessage})", exception);

            AppDomain.CurrentDomain.UnhandledException -= CurrentDomainOnUnhandledException;
            TaskScheduler.UnobservedTaskException -= TaskSchedulerOnUnobservedTaskException;
            Dispatcher.CurrentDispatcher.UnhandledException -= DispatcherOnUnhandledException;
            
            var appDispatcher = Application.Current?.Dispatcher;
            if (appDispatcher != null && Dispatcher.CurrentDispatcher != appDispatcher)
            {
                Log.Warn("Exception occurred on non-UI thread, rescheduling to UI");
                appDispatcher.BeginInvoke(() => ReportCrash(exception, developerMessage), DispatcherPriority.Send);
                Log.Debug($"Sent signal to UI thread to report crash related to exception {exception.Message}");

                return;
            }
            
            try
            {
                var reporter = new ExceptionDialog();

                var config = new ExceptionDialogConfig()
                {
                    AppName = AppArguments.Instance.AppName,
                    Title = $"{AppArguments.Instance.AppName} Error Report"
                };

                var configurationFilesToInclude = Directory
                    .EnumerateFiles(AppArguments.Instance.AppDataDirectory, "*.cfg", SearchOption.TopDirectoryOnly);

                var logFilesToInclude = new DirectoryInfo(AppArguments.Instance.AppDataDirectory)
                    .GetFiles("*.log", SearchOption.AllDirectories)
                    .OrderByDescending(x => x.LastWriteTime)
                    .Take(2)
                    .Select(x => x.FullName)
                    .ToArray();

                config.FilesToAttach = new[]
                    {
                        logFilesToInclude,
                        configurationFilesToInclude
                    }.SelectMany(x => x)
                    .ToArray();
                reporter.Config = config;
                
                reporter.Show(exception);
                
                Log.Warn($"Forcefully terminating Environment due to unrecoverable error");
                Environment.Exit(-1);
            }
            catch (Exception e)
            {
                Log.HandleException(new ApplicationException("Exception in ExceptionReporter :-(", e));
            }
        }
    }
}