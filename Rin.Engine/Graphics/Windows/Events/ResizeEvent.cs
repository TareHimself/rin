﻿using Rin.Engine.Math;

namespace Rin.Engine.Graphics.Windows.Events;

public class ResizeEvent : WindowEvent
{
    public required Vector2<uint> Size;
}