using System.Numerics;
using rin.Examples.Common.Views;
using Rin.Engine.Core.Extensions;
using Rin.Engine.Core.Math;
using Rin.Engine.Views.Animation;
using Rin.Engine.Views;

namespace rin.Examples.AudioPlayer.Views;

public class TrackImage : AsyncWebImage
{
    public TrackImage(string uri) : base(uri)
    {
        OnLoaded += (_) =>
        {
            Parent?.Parent?.Mutate(c =>
            {
                c.Visibility = Visibility.Visible;
                c.PivotTo(new Vector2(0.0f, 0.0f), 1.0f, easingFunction: EasingFunctions.EaseInExpo);
            });
        };
    }
}