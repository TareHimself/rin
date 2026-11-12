namespace Rin.Core.Graphics.Shaders;

public interface IShader : IDisposable
{
    public bool Ready { get; }

    /// <summary>
    ///     Should only be called by the <see cref="IShaderManager" />
    /// </summary>
    public void Compile(ICompilationContext context);
}