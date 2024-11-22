using rin.Widgets.Graphics.Commands;

namespace rin.Widgets.Graphics;

public interface IBatch
{
    abstract IEnumerable<ulong> GetMemoryNeeded();
    abstract IBatcher GetRenderer();
    abstract void AddFromCommand(BatchedCommand command);
}