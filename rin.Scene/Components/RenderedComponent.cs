﻿using rin.Core.Math;
using rin.Scene.Graphics;

namespace rin.Scene.Components;

public abstract class RenderedComponent : SceneComponent
{
    protected override void CollectSelf(SceneFrame frame, Matrix4 parentTransform, Matrix4 myTransform)
    {
        base.CollectSelf(frame, parentTransform, myTransform);
    }
}