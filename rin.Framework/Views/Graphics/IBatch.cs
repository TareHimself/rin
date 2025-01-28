using rin.Framework.Views.Graphics.Commands;

namespace rin.Framework.Views.Graphics;

public interface IBatch
{
    IEnumerable<ulong> GetMemoryNeeded();
    IBatcher GetRenderer();
    void AddFromCommand(BatchedCommand command);
}