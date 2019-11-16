using System.Drawing;
using System.Runtime.InteropServices;

namespace EyeAuras.OnTopReplica.Native
{
    /// <summary>
    ///     Native Point structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct NPoint
    {
        public int X, Y;

        public NPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public NPoint(NPoint copy)
        {
            X = copy.X;
            Y = copy.Y;
        }

        public static NPoint FromPoint(Point point)
        {
            return new NPoint(point.X, point.Y);
        }

        public override string ToString()
        {
            return "{" + X + "," + Y + "}";
        }
    }
}