﻿using System.Numerics;
using Rin.Engine.Views;
using Rin.Engine.Views.Graphics;

namespace rin.Examples.AudioPlayer.Views;

public class AudioVisualizer : ContentView
{
    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        return availableSpace;
    }

    protected override Vector2 ComputeDesiredContentSize()
    {
        throw new NotImplementedException();
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        throw new NotImplementedException();
    }
}