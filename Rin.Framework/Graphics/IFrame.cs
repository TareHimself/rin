namespace Rin.Framework.Graphics;

public interface IFrame : IDisposable
{
    public event Action<Frame>? OnReset;
}