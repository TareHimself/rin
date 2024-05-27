using System.Runtime.InteropServices;
using aerox.Runtime.Math;

namespace aerox.Runtime.Scene.Graphics.Drawers.Deferred;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SceneGlobalBuffer
{
    public Matrix4 ViewMatrix = Matrix4.Identity;
    public Matrix4 ProjectionMatrix = Matrix4.Identity;
    public Vector4<float> CameraLocation = 0.0f;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
    public SceneLight[] Lights = new SceneLight[1024];

    public int LightsCount = 0;

    public SceneGlobalBuffer()
    {
    }
}