using rin.Examples.Common.Views;
using rin.Framework.Core.Extensions;
using rin.Framework.Core.Math;
using rin.Framework.Views.Animation;
using rin.Framework.Views;

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
                c.PivotTo(new Vec2<float>(0.0f, 0.0f), 1.0f, easingFunction: EasingFunctions.EaseInExpo);
            });
        };
    }
}