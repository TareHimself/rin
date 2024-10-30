namespace rin.Widgets.Graphics.Commands;

public abstract  class BatchedCommand : Command
{
    public abstract IBatchRenderer GetBatchRenderer();
}