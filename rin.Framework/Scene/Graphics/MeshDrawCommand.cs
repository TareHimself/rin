using rin.Framework.Graphics;
using rin.Framework.Graphics.Shaders;

namespace rin.Framework.Scene.Graphics;

/// <summary>
/// Base class for all draws involving <see cref="DeviceGeometry"/>, each <see cref="MeshDrawCommand.Shader"/> should support instancing
/// </summary>
public abstract class MeshDrawCommand : ICommand
{
    
    public abstract uint Priority { get; set; }
    public abstract bool CastShadows { get; set; }
    public abstract bool Translucent { get; set; }
    public abstract IGraphicsShader Shader { get; }
    
    public abstract DeviceGeometry Geometry { get; }
    public abstract ulong RequiredMemory { get; }
    
    /// <summary>
    /// Write per batch data to the buffer, the buffer will be the same size provided in <see cref="RequiredMemory"/>
    /// </summary>
    /// <param name="buffer"></param>
    public abstract void Write(IDeviceBuffer buffer);
    
    public abstract void ResolveDescriptorSets(IGraphicsShader shader);
    
    public virtual void Dispose()
    {
    }
}