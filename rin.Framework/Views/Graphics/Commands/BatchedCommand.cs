namespace rin.Framework.Views.Graphics.Commands;

public abstract  class BatchedCommand : GraphicsCommand
{
    public abstract IBatcher GetBatchRenderer();
}