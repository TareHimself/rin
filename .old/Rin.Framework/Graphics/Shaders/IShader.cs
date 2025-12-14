namespace Rin.Framework.Graphics.Shaders;

public interface IShader : IDisposable
{
    public bool Ready { get; }

    /// <summary>
    /// Should only be called by the <see cref="IShaderManager" />
    /// </summary>
    /// <param name="context"></param>
    public void Compile(ICompilationContext context);
}