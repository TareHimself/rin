using System.Runtime.InteropServices;
using rin.Runtime.Graphics;
using TerraFX.Interop.Vulkan;

namespace rin.Scene.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct DeviceGeometry : IDisposable
{
    public DeviceBuffer IndexBuffer;
    public DeviceBuffer VertexBuffer;

    public void Dispose()
    {
        IndexBuffer.Dispose();
        VertexBuffer.Dispose();
    }
}