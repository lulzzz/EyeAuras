using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Media;
using EyeAuras.Shared;
using EyeAuras.Shared.Services;
using EyeAuras.UI.Core.Services;
using JetBrains.Annotations;
using Color = System.Windows.Media.Color;

namespace EyeAuras.UI.Core.Models
{
    internal sealed class OverlayAuraProperties : IAuraProperties
    {
        public static readonly OverlayAuraProperties Default = new OverlayAuraProperties
        {
            OverlayBounds = new Rectangle(100, 100, 200, 200),
            SourceRegionBounds = Rectangle.Empty,
            IsEnabled = true
        };

        public IList<IAuraProperties> TriggerProperties { [CanBeNull] get; [CanBeNull] set; } =
            new List<IAuraProperties>();

        public IList<IAuraProperties> OnEnterActionProperties { [CanBeNull] get; [CanBeNull] set; } =
            new List<IAuraProperties>();

        public string Id { get; set; }

        public string Name { get; set; }

        public WindowMatchParams WindowMatch { get; set; }

        public Rectangle OverlayBounds { get; set; }

        public Rectangle SourceRegionBounds { get; set; }

        public double BorderThickness { get; set; }

        public Color BorderColor { get; set; } = Colors.AntiqueWhite;

        public bool IsClickThrough { get; set; }

        public bool IsEnabled { get; set; }

        public double ThumbnailOpacity { get; set; } = 1;

        public bool MaintainAspectRatio { get; set; } = true;

        public int Version { get; set; } = 1;
    }
}