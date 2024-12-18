using rin.Framework.Views.Graphics.Commands;

namespace rin.Framework.Views.Graphics;

public interface IBatch
{
    abstract IEnumerable<ulong> GetMemoryNeeded();
    abstract IBatcher GetRenderer();
    abstract void AddFromCommand(BatchedCommand command);
}