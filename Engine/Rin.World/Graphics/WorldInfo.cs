using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Shade;
using Rin.World.Graphics.Default.Shaders;

namespace Rin.World.Graphics;

[ShadeExport]
[NoReorder]
public struct WorldInfo
{
    public Matrix4x4 View;
    public Matrix4x4 Projection;
    public Matrix4x4 ViewProjection;
    public Vector3 CameraPosition;
    public float Time;
    public DeviceSpan<LightGridInfo> Grid;
    public RenderChannel Channel;
}