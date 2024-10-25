using aerox.Runtime.Graphics;
using aerox.Runtime.Widgets.Graphics;
using aerox.Runtime.Widgets.Graphics.Commands;

namespace aerox.Runtime.Widgets;

public interface IBatch
{
    abstract IEnumerable<ulong> GetMemoryNeeded();
    abstract IBatchRenderer GetRenderer();
    abstract void AddFromCommand(BatchedCommand command);
}