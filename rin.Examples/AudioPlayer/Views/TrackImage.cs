using System.Numerics;
using Rin.Framework.Extensions;
using Rin.Framework.Views;
using Rin.Framework.Views.Animation;
using rin.Examples.Common.Views;
using Rin.Framework.Shared.Math;

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