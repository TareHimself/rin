using Rin.Framework.Views.Graphics.Commands;

namespace Rin.Framework.Views.Graphics;

public interface IBatch
{
    IEnumerable<ulong> GetMemoryNeeded();
    IBatcher GetBatcher();
    void AddFromCommand(ICommand command);
}