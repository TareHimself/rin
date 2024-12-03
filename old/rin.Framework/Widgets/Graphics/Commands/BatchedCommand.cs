namespace rin.Framework.Widgets.Graphics.Commands;

public abstract  class BatchedCommand : GraphicsCommand
{
    public abstract IBatcher GetBatchRenderer();
}