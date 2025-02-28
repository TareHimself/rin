using System.Runtime.InteropServices;
using Rin.Engine.Graphics;

namespace Rin.Editor.Scene.Graphics;


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