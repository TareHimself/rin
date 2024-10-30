using rin.Graphics;
using rin.Widgets.Graphics;
using rin.Widgets.Graphics.Commands;

namespace rin.Widgets;

public interface IBatch
{
    abstract IEnumerable<ulong> GetMemoryNeeded();
    abstract IBatchRenderer GetRenderer();
    abstract void AddFromCommand(BatchedCommand command);
}