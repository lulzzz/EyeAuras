using System;
using JetBrains.Annotations;

namespace EyeAuras.Shared.Services
{
    public interface IMainWindowBlocksRepository
    {
        [NotNull] 
        IDisposable AddStatusBarItem([NotNull] object item);
    }
}