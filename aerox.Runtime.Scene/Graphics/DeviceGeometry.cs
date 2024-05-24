using System.Runtime.InteropServices;
using aerox.Runtime.Graphics;
using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Scene.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct DeviceGeometry : IDisposable
{
    public DeviceBuffer IndexBuffer;
    public DeviceBuffer VertexBuffer;
    public VkBufferDeviceAddressInfo VertexBufferAddress;

    public void Dispose()
    {
        IndexBuffer.Dispose();
        VertexBuffer.Dispose();
        
    }
}