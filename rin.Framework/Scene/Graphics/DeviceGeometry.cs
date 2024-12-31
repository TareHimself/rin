using System.Runtime.InteropServices;
using rin.Framework.Graphics;

namespace rin.Framework.Scene.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct DeviceGeometry : IDisposable
{
    public IDeviceBuffer IndexBuffer;
    public IDeviceBuffer VertexBuffer;

    public void Dispose()
    {
        IndexBuffer.Dispose();
        VertexBuffer.Dispose();
    }
}