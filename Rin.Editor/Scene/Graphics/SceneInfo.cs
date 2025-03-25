using System.Runtime.InteropServices;
using Rin.Engine.Core.Math;

namespace Rin.Editor.Scene.Graphics;


public struct SceneInfo
{
    public Mat4 View;
    public Mat4 Projection;
    public Mat4 ViewProjection;
    public ulong LightsAddress;
    public int NumLights;
}