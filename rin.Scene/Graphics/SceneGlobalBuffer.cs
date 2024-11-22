using System.Runtime.InteropServices;
using rin.Core.Math;

namespace rin.Scene.Graphics;

[System.Runtime.CompilerServices.InlineArray(1024)]
public struct LightsBuffer
{
    private SceneLight _element;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SceneGlobalBuffer
{
    // mat4 viewMatrix;
    // mat4 projectionMatrix;
    // float4 ambientColor;
    // float4 lightDirection;
    // float4 cameraLocation;
    // float4 numLights;
    // Light lights[1024];
    public Matrix4 ViewMatrix = Matrix4.Identity;
    public Matrix4 ProjectionMatrix = Matrix4.Identity;
    public Vector4<float> ambient = 0.0f;
    public Vector4<float> sunDirection = 0.0f;
    public Vector4<float> CameraLocation = 0.0f;

    /*[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
    public SceneLight[] Lights = new SceneLight[1024];*/
    public int LightsCount = 0;
    public LightsBuffer Lights;
    public SceneGlobalBuffer()
    {
    }
}