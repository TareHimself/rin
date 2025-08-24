﻿using System.Numerics;
using Rin.Framework.Extensions;
using Rin.Framework.Math;
using Rin.Framework.Views;
using Rin.Framework.Views.Animation;
using rin.Examples.Common.Views;

namespace rin.Examples.AudioPlayer.Views;

public class TrackImage : AsyncWebImage
{
    public TrackImage(string uri) : base(uri)
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