namespace Rin.Core.Views.Graphics.Commands;

public interface IBatchedCommand : ICommand
{
    public IBatcher GetBatcher();
}