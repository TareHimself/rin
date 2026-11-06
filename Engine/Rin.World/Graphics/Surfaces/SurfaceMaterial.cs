using Rin.Core.Graphics;
using Rin.World.Graphics.Default;

namespace Rin.World.Graphics.Surfaces;

/// <summary>
///     Draws an <see cref="ISurfaceMaterial{TSelf}" /> struct. Several meshes can share one instance, and setting
///     <see cref="Data" /> changes all of them from the next frame on.
/// </summary>
public sealed class SurfaceMaterial<TMaterial> : IMeshMaterial where TMaterial : unmanaged, ISurfaceMaterial<TMaterial>
{
    private const int MaxTextures = 32;

    private TMaterial _data;

    public SurfaceMaterial(in TMaterial data)
    {
        Data = data;
    }

    public TMaterial Data
    {
        get => _data;
        set
        {
            _data = value;
            Textures = ResolveTextures(value);
        }
    }

    /// <summary>
    ///     The textures <see cref="Data" /> samples, resolved when it was set so a texture freed later is detected.
    /// </summary>
    public ResourceHandle[] Textures { get; private set; } = [];

    public bool Translucent => false;
    public IMaterialPass ColorPass => SurfaceColorPass<TMaterial>.Shared;
    public IMaterialPass DepthPass =>
        TMaterial.DepthShader is null ? MeshDepthPass.Shared : SurfaceDepthPass<TMaterial>.Shared;

    public float MaxVertexOffset => TMaterial.MaxVertexOffset;

    internal SurfaceDraw<TMaterial> ToDraw(ProcessedMesh mesh) => new() { Instance = mesh.ToInstance(), Material = _data };

    private static ResourceHandle[] ResolveTextures(in TMaterial data)
    {
        var textures = new TextureList(stackalloc DeviceHandle[MaxTextures]);
        TMaterial.GetTextures(in data, ref textures);
        if (textures.Count == 0) return [];

        var module = IGraphicsModule.Get();
        var resolved = new ResourceHandle[textures.Count];
        for (var i = 0; i < resolved.Length; i++) resolved[i] = module.ResolveResourceHandle(textures.AsSpan()[i]);
        return resolved;
    }
}
