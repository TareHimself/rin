﻿namespace rin.Widgets.Graphics.Commands;

public abstract  class BatchedCommand : GraphicsCommand
{
    public abstract IBatchRenderer GetBatchRenderer();
}