using System.Runtime.InteropServices;
using rin.Runtime.Core.Extensions;
using rin.Runtime.Graphics.Material;
using rin.Runtime.Core.Math;
using rin.Scene.Graphics;
using rin.Scene.Graphics.Commands;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;
namespace rin.Scene.Components;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct StaticMeshPushConstants
{
    public Matrix4 Transform;
    public ulong vertexBufferAddress;
}

internal class StaticMeshDrawCommand : Command
{

    protected StaticMesh Mesh;
    protected MaterialInstance?[] Materials;
    protected Matrix4 Transform;
    public StaticMeshDrawCommand(StaticMesh mesh, MaterialInstance?[] materials,Matrix4 transform)
    {
        foreach (var materialInstance in materials)
        {
            materialInstance?.Reserve();
        }

        mesh.Reserve();
        Mesh = mesh;
        Materials = materials;
        Transform = transform;
    }

    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
        foreach (var materialInstance in Materials)
        {
            materialInstance?.Dispose();
        }

        Mesh.Dispose();
    }

    public override void Run(SceneFrame frame)
    {
        
        var cmd = frame.Original.GetCommandBuffer();
        for (var i = 0; i < Mesh.Surfaces.Length; i++)
        {
            var surface = Mesh.Surfaces[i];
            var material = Materials.TryIndex(i) ?? Mesh.Materials.TryIndex(i) ?? frame.Drawer.GetDefaultMeshMaterial();
            
            if (frame.Drawer.GlobalBuffer is { } globalBuffer)
            {
                material.BindBuffer("scene", globalBuffer);
            }
            
            material.BindTo(frame);
            
            material.Push(cmd, new StaticMeshPushConstants()
            {
                Transform = Transform,
                vertexBufferAddress = Mesh.Geometry.VertexBufferAddress
            });
            
            vkCmdBindIndexBuffer(cmd,Mesh.Geometry.IndexBuffer._buffer,0,VkIndexType.VK_INDEX_TYPE_UINT32);
            
            vkCmdDrawIndexed(cmd,surface.Count,1,surface.StartIndex,0,0);
        }
    }
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
            _mesh?.Reserve();
            MaterialOverrides = _mesh?.Materials.Select(c => (MaterialInstance?)null).ToArray() ?? [];
        }
    }

    public void SetMaterialOverride(MaterialInstance material)
    {
        
    }

    protected override void CollectSelf(SceneFrame frame, Matrix4 parentTransform, Matrix4 myTransform)
    {
        base.CollectSelf(frame, parentTransform, myTransform);
        if (_mesh is { } mesh)
        {
            frame.AddCommand(new StaticMeshDrawCommand(mesh,[],myTransform));
        }
    }

    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
        _mesh?.Dispose();
        foreach (var materialOverride in MaterialOverrides)
        {
            materialOverride?.Dispose();
        }
    }
}