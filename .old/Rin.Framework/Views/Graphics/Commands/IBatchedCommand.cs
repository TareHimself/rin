namespace Rin.Framework.Views.Graphics.Commands;

public interface IBatchedCommand : ICommand
{
    public IBatcher GetBatcher();
}