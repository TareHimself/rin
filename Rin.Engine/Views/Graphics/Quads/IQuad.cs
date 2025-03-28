﻿using System.Numerics;
using Rin.Engine.Core.Math;

namespace Rin.Engine.Views.Graphics.Quads;

public interface IQuad
{
    public Vec4<int> Opts { get; set; }
    public Vector2 Size { get; set; }
    public Mat3 Transform { get; set; }
}