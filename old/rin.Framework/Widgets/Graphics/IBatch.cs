using rin.Framework.Widgets.Graphics.Commands;

namespace rin.Framework.Widgets.Graphics;

public interface IBatch
{
    abstract IEnumerable<ulong> GetMemoryNeeded();
    abstract IBatcher GetRenderer();
    abstract void AddFromCommand(BatchedCommand command);
}