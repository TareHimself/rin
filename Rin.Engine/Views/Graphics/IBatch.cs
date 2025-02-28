using Rin.Engine.Views.Graphics.Commands;

namespace Rin.Engine.Views.Graphics;

public interface IBatch
{
    IEnumerable<ulong> GetMemoryNeeded();
    IBatcher GetRenderer();
    void AddFromCommand(BatchedCommand command);
}