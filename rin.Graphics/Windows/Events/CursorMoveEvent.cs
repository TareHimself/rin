﻿using rin.Core.Math;

namespace rin.Graphics.Windows.Events;

public class CursorMoveEvent : CursorEvent
{
    public required Vector2<double> Delta;
}