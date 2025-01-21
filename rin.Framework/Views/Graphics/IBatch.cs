using rin.Framework.Views.Graphics.Commands;

namespace rin.Framework.Views.Graphics;

public interface IBatch
{
    IEnumerable<int> GetMemoryNeeded();
    IBatcher GetRenderer();
    void AddFromCommand(BatchedCommand command);
}