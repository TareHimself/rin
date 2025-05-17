using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics.Shaders;

public interface IComputeShader : IShader
{
    public uint GroupSizeX { get; }
    public uint GroupSizeY { get; }
    public uint GroupSizeZ { get; }
    
    /// <summary>
    /// Dispatches this compute shader using the provided work group counts
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    public void Dispatch(in VkCommandBuffer cmd,uint x, uint y = 1, uint z = 1);
    
    
    /// <summary>
    /// same as <see cref="Dispatch"/> but will figure out the correct group counts based in the invocation counts
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    public void Invoke(in VkCommandBuffer cmd,uint x, uint y = 1, uint z = 1);
}