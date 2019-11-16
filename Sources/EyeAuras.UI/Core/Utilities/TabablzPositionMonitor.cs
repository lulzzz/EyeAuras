using System.Collections.Generic;
using System.Linq;
using Dragablz;
using log4net;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.Core.Utilities
{
    internal sealed class TabablzPositionMonitor<T> : VerticalPositionMonitor
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(TabablzPositionMonitor<T>));

        public TabablzPositionMonitor()
        {
            Items = Enumerable.Empty<T>();
            OrderChanged += OnOrderChanged;
        }

        public IEnumerable<T> Items { get; private set; }

        private void OnOrderChanged(object sender, OrderChangedEventArgs orderChangedEventArgs)
        {
            if (sender == null || orderChangedEventArgs == null)
            {
                return;
            }

            Log.Debug(
                $"Items order has changed, \nOld:\n\t{orderChangedEventArgs.PreviousOrder.EmptyIfNull().Select(x => x?.ToString() ?? "(null)").DumpToTable()}, \nNew:\n\t{orderChangedEventArgs.NewOrder.EmptyIfNull().Select(x => x?.ToString() ?? "(null)").DumpToTable()}");
            Items = orderChangedEventArgs.NewOrder
                .EmptyIfNull()
                .OfType<T>()
                .ToArray();
        }
    }
}