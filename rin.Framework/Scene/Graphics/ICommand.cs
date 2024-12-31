namespace rin.Framework.Scene.Graphics;

public interface ICommand : IDisposable
{
    public ulong GetRequiredMemory();
}