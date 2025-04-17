namespace Rin.Engine.Graphics.Shaders;

public interface IComputeShader : IShader
{
    public uint GroupSizeX { get; }
    public uint GroupSizeY { get; }
    public uint GroupSizeZ { get; }
}