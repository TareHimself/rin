namespace Rin.Engine.Views.Graphics;

public interface ISurface : IDisposable
{
    public void Update(double deltaTime);
}