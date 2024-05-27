using System.Runtime.InteropServices;
using aerox.Runtime.Graphics.Material;
using aerox.Runtime.Math;
using aerox.Runtime.Scene.Graphics;
using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Scene.Components;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct StaticMeshPushConstants
{
    public Matrix4 Transform;
    public VkBufferDeviceAddressInfo vertexBufferAddress;
}

public class StaticMeshComponent : RenderedComponent
{
    private StaticMesh? _mesh;

    public MaterialInstance?[] MaterialOverrides = [];

    public StaticMesh? Mesh
    {
        get => _mesh;
        set
        {
            _mesh = value;
            MaterialOverrides = _mesh?.Materials.ToArray() ?? [];
        }
    }

    public override void Draw(SceneFrame frame, Matrix4 parentSpace)
    {
        base.Draw(frame, parentSpace);
    }
}