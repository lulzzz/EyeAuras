using System.Collections.Generic;

namespace EyeAuras.OnTopReplica.WindowSeekers
{
    /// <summary>
    ///     Interface for window seekers.
    /// </summary>
    public interface IWindowSeeker
    {
        /// <summary>
        ///     Get the list of matching windows, ordered by priority (optionally).
        /// </summary>
        IReadOnlyCollection<WindowHandle> Windows { get; }

        /// <summary>
        ///     Refreshes the list of windows.
        /// </summary>
        void Refresh();
    }
}