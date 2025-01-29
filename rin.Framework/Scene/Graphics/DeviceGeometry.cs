using System.Runtime.InteropServices;
using rin.Framework.Graphics;

namespace rin.Framework.Scene.Graphics;


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