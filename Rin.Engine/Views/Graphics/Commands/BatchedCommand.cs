namespace Rin.Engine.Views.Graphics.Commands;

public abstract class BatchedCommand : Command
{
    public abstract IBatcher GetBatchRenderer();
}