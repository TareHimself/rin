namespace Rin.Framework.Graphics.Shaders;

public interface IComputeShader : IShader
{
    public uint GroupSizeX { get; }
    public uint GroupSizeY { get; }
    public uint GroupSizeZ { get; }

    public IComputeBindContext? Bind(IExecutionContext ctx, bool wait = true);
}