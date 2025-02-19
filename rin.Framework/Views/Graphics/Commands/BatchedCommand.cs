namespace rin.Framework.Views.Graphics.Commands;

public abstract class BatchedCommand : Command
{
    public abstract IBatcher GetBatchRenderer();
}