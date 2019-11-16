using System;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;

namespace PoeShared.Squirrel.Updater
{
    public interface IApplicationUpdaterViewModel : IDisposableReactiveObject
    {
        [NotNull] CommandWrapper CheckForUpdatesCommand { get; }

        [NotNull] CommandWrapper RestartCommand { get; }

        [NotNull] CommandWrapper ApplyUpdate { get; }

        string Error { get; set; }

        string StatusText { get; set; }

        bool IsOpen { get; set; }

        Version UpdatedVersion { get; }

        Version LatestVersion { get; }
    }
}