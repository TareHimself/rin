namespace Rin.Engine.Views.Graphics.Commands;

public interface IBatchedCommand : ICommand
{
    public IBatcher GetBatcher();
}