using System.Numerics;
using rin.Examples.Common.Views;
using Rin.Core.Extensions;
using Rin.Core.Shared.Math;
using Rin.Core.Views;
using Rin.Core.Views.Animation;

namespace rin.Examples.AudioPlayer.Views;

public class TrackImageView : AsyncWebImageView
{
    public TrackImageView(string uri) : base(uri)
    {
        OnLoaded += _ =>
        {
            Parent?.Parent?.Mutate(c =>
            {
                c.Visibility = Visibility.Visible;
                c.PivotTo(new Vector2(0.0f, 0.0f), 1.0f, easingFunction: EasingFunctions.EaseInExpo);
            });
        };
    }
}