using System;
using System.Threading;

namespace PoeShared.Squirrel.Utility
{
    internal static class Disposable
    {
        public static IDisposable Create(Action action)
        {
            return new AnonDisposable(action);
        }

        private class AnonDisposable : IDisposable
        {
            private static readonly Action DummyBlock = () => { };
            private Action block;

            public AnonDisposable(Action b)
            {
                block = b;
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref block, DummyBlock)();
            }
        }
    }
}