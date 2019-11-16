using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData.Binding;
using EyeAuras.OnTopReplica;
using EyeAuras.Shared.Services;
using JetBrains.Annotations;
using log4net;
using PoeShared;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using ReactiveUI;
using Unity;

namespace EyeAuras.UI.Core.ViewModels
{
    internal sealed class WindowSelectorViewModel : DisposableReactiveObject, IWindowSelectorViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WindowSelectorViewModel));
        private static readonly TimeSpan ThrottlingPeriod = TimeSpan.FromMilliseconds(100);

        private readonly ObservableAsPropertyHelper<bool> enableOverlaySelector;

        private WindowHandle activeWindow;
        private WindowHandle[] matchingWindowList = Array.Empty<WindowHandle>();
        private WindowMatchParams targetWindow;
        private string windowTitle;
        private bool windowTitleIsRegex;
        
        public WindowSelectorViewModel(
            [NotNull] IWindowListProvider windowListProvider,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            WindowList = windowListProvider.WindowList;

            this.WhenAnyValue(x => x.WindowTitle)
                .WithPrevious((prev, curr) => new {prev, curr})
                .DistinctUntilChanged()
                .Where(x => !string.IsNullOrEmpty(x.prev) && x.curr == null)
                .Subscribe(x => WindowTitle = x.prev)
                .AddTo(Anchors);
        
            windowListProvider.WindowList.ToObservableChangeSet()
                .ToUnit()
                .Merge(this.WhenAnyValue(x => x.TargetWindow).ToUnit())
                .Throttle(ThrottlingPeriod)
                .ObserveOn(uiScheduler)
                .Subscribe(x => MatchingWindowList = BuildMatches(windowListProvider.WindowList))
                .AddTo(Anchors);
                
            this.WhenAnyValue(x => x.ActiveWindow)
                .Where(x => x != null)
                .Where(x => !IsMatch(x, TargetWindow))
                .Subscribe(
                    x =>
                    {
                        var newTargetWindow = new WindowMatchParams
                        {
                            Title = x.Title,
                        };
                        Log.Debug($"Selected non-matching Overlay source, changing TargetWindow, {TargetWindow} => {newTargetWindow}");
                        TargetWindow = newTargetWindow;
                    })
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.MatchingWindowList)
                .Where(items => !items.Contains(ActiveWindow))
                .Select(items => items.FirstOrDefault(x => x.Handle == ActiveWindow?.Handle) ?? items.FirstOrDefault())
                .Where(x => !Equals(ActiveWindow, x))
                .Subscribe(
                    x =>
                    {
                        Log.Debug(
                            $"Setting new Overlay Window(target: {TargetWindow}): {(ActiveWindow == null ? "null" : ActiveWindow.ToString())} => {(x == null ? "null" : x.ToString())}\n\t{MatchingWindowList.DumpToTable()}");
                        ActiveWindow = x;
                    })
                .AddTo(Anchors);
            
            enableOverlaySelector = this.WhenAnyProperty(x => x.MatchingWindowList)
                .Select(change => MatchingWindowList.Length > 1)
                .ToPropertyHelper(this, x => x.EnableOverlaySelector)
                .AddTo(Anchors);
                
            this.WhenAnyValue(x => x.TargetWindow)
                .Subscribe(
                    x =>
                    {
                        WindowTitle = x.Title;
                        WindowTitleIsRegex = x.IsRegex;
                    })
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.WindowTitle, x => x.WindowTitleIsRegex)
                .Select(
                    x => new WindowMatchParams
                    {
                        Title = WindowTitle,
                        IsRegex = WindowTitleIsRegex
                    })
                .DistinctUntilChanged()
                .Throttle(ThrottlingPeriod)
                .ObserveOn(uiScheduler)
                .Subscribe(x => TargetWindow = x)
                .AddTo(Anchors);
            
            SetWindowTitleCommand = CommandWrapper.Create<WindowHandle>(SetWindowTitleCommandExecuted);
        }

        public string WindowTitle
        {
            get => windowTitle;
            set => RaiseAndSetIfChanged(ref windowTitle, value);
        }

        public bool WindowTitleIsRegex
        {
            get => windowTitleIsRegex;
            set => RaiseAndSetIfChanged(ref windowTitleIsRegex, value);
        }

        public ReadOnlyObservableCollection<WindowHandle> WindowList { get; }

        public ICommand SetWindowTitleCommand { get; }

        public bool EnableOverlaySelector => enableOverlaySelector.Value;

        public WindowHandle ActiveWindow
        {
            get => activeWindow;
            set => RaiseAndSetIfChanged(ref activeWindow, value);
        }

        public WindowHandle[] MatchingWindowList
        {
            get => matchingWindowList;
            private set => RaiseAndSetIfChanged(ref matchingWindowList, value);
        }

        public WindowMatchParams TargetWindow
        {
            get => targetWindow;
            set => RaiseAndSetIfChanged(ref targetWindow, value);
        }
        
        private void SetWindowTitleCommandExecuted(WindowHandle handle)
        {
            WindowTitle = handle.Title;
        }

        private WindowHandle[] BuildMatches(IEnumerable<WindowHandle> source)
        {
            var comparer = new SortExpressionComparer<WindowHandle>().ThenByAscending(x => x.Title.Length).ThenByAscending(x => x.Title);
            var windowList = source
                .Where(x => IsMatch(x, targetWindow))
                .OrderBy(x => x, comparer)
                .ToArray();
            return windowList;
        }

        private bool IsMatch(WindowHandle window, WindowMatchParams matchParams)
        {
            Guard.ArgumentNotNull(window, nameof(window));

            if (string.IsNullOrEmpty(matchParams.Title))
            {
                return false;
            }

            if (string.IsNullOrEmpty(window.Title))
            {
                return false;
            }

            return window.Title.Contains(matchParams.Title, StringComparison.OrdinalIgnoreCase);
        }
    }
}