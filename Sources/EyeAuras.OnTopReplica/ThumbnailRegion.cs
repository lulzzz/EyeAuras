using System.Windows;
using PoeShared.Scaffolding;
using ReactiveUI;
using System;
using System.Drawing;
using Size = System.Drawing.Size;

namespace EyeAuras.OnTopReplica
{
    public sealed class ThumbnailRegion : DisposableReactiveObject
    {
        private int regionHeight;
        private int regionWidth;
        private int regionX;
        private int regionY;
        private Rectangle clientBounds;

        private readonly ObservableAsPropertyHelper<Rectangle> bounds;

        /// <summary>
        ///     Creates a ThumbnailRegion from a bounds rectangle (in absolute terms).
        /// </summary>
        public ThumbnailRegion(Rectangle rectangle)
        {
            SetValue(rectangle);

            bounds = this.WhenAnyProperty(x => x.RegionX, x => x.RegionY, x => x.RegionHeight, x => x.RegionWidth)
                .Select(() => RegionWidth <= 0 || RegionHeight <= 0
                        ? Rectangle.Empty
                        : new Rectangle(RegionX, RegionY, RegionWidth, RegionHeight))
                .ToPropertyHelper(this, x => x.Bounds)
                .AddTo(Anchors);
        }

        public Rectangle Bounds => bounds.Value;

        public int RegionX
        {
            get => regionX;
            set => RaiseAndSetIfChanged(ref regionX, value);
        }

        public int RegionY
        {
            get => regionY;
            set => RaiseAndSetIfChanged(ref regionY, value);
        }

        public int RegionWidth
        {
            get => regionWidth;
            set => RaiseAndSetIfChanged(ref regionWidth, value);
        }

        public int RegionHeight
        {
            get => regionHeight;
            set => RaiseAndSetIfChanged(ref regionHeight, value);
        }

        public void SetValue(Rectangle rectangle)
        {
            var previousState = new { RegionX, RegionY, RegionHeight, RegionWidth };
            regionWidth = rectangle.Width;
            regionHeight = rectangle.Height;
            regionX = rectangle.X;
            regionY = rectangle.Y;

            this.RaiseIfChanged(nameof(RegionX), previousState.RegionX, RegionX);
            this.RaiseIfChanged(nameof(RegionY), previousState.RegionY, RegionY);
            this.RaiseIfChanged(nameof(RegionHeight), previousState.RegionHeight, RegionHeight);
            this.RaiseIfChanged(nameof(RegionWidth), previousState.RegionWidth, RegionWidth);
        }

        /// <summary>
        ///     Computes the effective region representing the bounds inside a source thumbnail of a certain size.
        /// </summary>
        /// <param name="sourceSize">Size of the full thumbnail source.</param>
        /// <returns>Bounds inside the thumbnail.</returns>
        private Rectangle ComputeRegion(Size sourceSize)
        {
            try
            {
                var result = Bounds;
                var sourceBounds = new Rectangle(result.X, result.Y, sourceSize.Width, sourceSize.Height);
                result.Intersect(sourceBounds);
                return result;
            }
            catch (Exception e)
            {
                throw new ApplicationException($"Failed to compute Region size, sourceSize: {sourceSize}, current state: {new { Bounds }}", e);
            }
        }

        /// <summary>
        ///     Computes a value representing the size of the region inside a source thumbnail of a certain size.
        /// </summary>
        /// <param name="sourceSize">Size of the full thumbnail source.</param>
        /// <returns>Size of the bounds inside the thumbnail.</returns>
        public Size ComputeRegionSize(Size sourceSize)
        {
            return ComputeRegion(sourceSize).Size;
        }

        public override string ToString()
        {
            return $"Region({Bounds})";
        }
    }
}