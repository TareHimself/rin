using System.Collections.Concurrent;
using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.World.Components;
using Rin.World.Graphics.Default.Passes;

namespace Rin.World.Graphics.Default;

/// <summary>
///     Proxy table fed by a command queue drained once per <see cref="Snapshot" /> — the render thread
///     never touches the live table, only the frozen arrays each snapshot copies out.
/// </summary>
public class DefaultRenderSystem : IRenderSystem
{
    private enum ProxyKind
    {
        StaticMesh,
        SkinnedMesh,
        Light
    }

    private record struct ProxyRow(ProxyKind Kind, Matrix4x4 Transform)
    {
        public Matrix4x4 PreviousTransform = Transform;
        public StaticMeshProxyDesc StaticDesc;
        public SkinnedMeshProxyDesc SkinnedDesc;
        public LightInfo Light;
    }

    private readonly List<ProxyRow?> _slots = [];
    private readonly List<uint> _versions = [];
    private readonly Stack<uint> _freeIndices = new();
    private readonly ConcurrentQueue<Action> _commands = new();
    private float _interpolationAlpha = 1f;

    public RenderProxyHandle CreateStaticMeshProxy(in StaticMeshProxyDesc desc)
    {
        var handle = Reserve();
        var captured = desc;
        _commands.Enqueue(() => Set(handle, new ProxyRow(ProxyKind.StaticMesh, captured.Transform)
            { StaticDesc = captured }));
        return handle;
    }

    public RenderProxyHandle CreateSkinnedMeshProxy(in SkinnedMeshProxyDesc desc)
    {
        var handle = Reserve();
        var captured = desc;
        _commands.Enqueue(() => Set(handle, new ProxyRow(ProxyKind.SkinnedMesh, captured.Transform)
            { SkinnedDesc = captured }));
        return handle;
    }

    public RenderProxyHandle CreateLightProxy(in LightInfo desc)
    {
        var handle = Reserve();
        var captured = desc;
        _commands.Enqueue(() => Set(handle, new ProxyRow(ProxyKind.Light, Matrix4x4.Identity) { Light = captured }));
        return handle;
    }

    public void UpdateProxyTransform(RenderProxyHandle handle, in Matrix4x4 worldTransform)
    {
        var captured = worldTransform;
        _commands.Enqueue(() => Mutate(handle, row =>
        {
            // Owners re-push every frame even when unchanged; only roll Previous forward on a real change.
            if (row.Transform != captured) row.PreviousTransform = row.Transform;
            row.Transform = captured;
            if (row.Kind == ProxyKind.StaticMesh) row.StaticDesc = row.StaticDesc with { Transform = captured };
            if (row.Kind == ProxyKind.SkinnedMesh) row.SkinnedDesc = row.SkinnedDesc with { Transform = captured };
            return row;
        }));
    }

    public void SetInterpolationAlpha(float alpha)
    {
        _interpolationAlpha = float.Clamp(alpha, 0f, 1f);
    }

    public void UpdateStaticMeshProxy(RenderProxyHandle handle, in StaticMeshProxyDesc desc)
    {
        var captured = desc;
        _commands.Enqueue(() => Mutate(handle, row =>
        {
            row.StaticDesc = captured with { Transform = row.Transform };
            return row;
        }));
    }

    public void UpdateLightProxy(RenderProxyHandle handle, in LightInfo desc)
    {
        var captured = desc;
        _commands.Enqueue(() => Mutate(handle, row =>
        {
            row.Light = captured;
            return row;
        }));
    }

    public void DestroyProxy(RenderProxyHandle handle)
    {
        _commands.Enqueue(() => Free(handle));
    }

    public IWorldRenderContext Snapshot(CameraComponent view, in Extent2D extent)
    {
        while (_commands.TryDequeue(out var command)) command();

        List<StaticMeshInfo> staticMeshes = [];
        List<SkinnedMeshInfo> skinnedMeshes = [];
        List<LightInfo> lights = [];

        foreach (var slot in _slots)
        {
            if (slot is not { } row) continue;
            switch (row.Kind)
            {
                case ProxyKind.StaticMesh:
                    staticMeshes.Add(new StaticMeshInfo
                    {
                        Mesh = row.StaticDesc.Mesh,
                        Transform = InterpolateTransform(row.PreviousTransform, row.Transform, _interpolationAlpha),
                        SurfaceIndices = row.StaticDesc.SurfaceIndices,
                        Materials = row.StaticDesc.Materials
                    });
                    break;
                case ProxyKind.SkinnedMesh:
                    skinnedMeshes.Add(new SkinnedMeshInfo
                    {
                        Pose = row.SkinnedDesc.Pose,
                        Skeleton = row.SkinnedDesc.Skeleton,
                        Mesh = row.SkinnedDesc.Mesh,
                        Transform = InterpolateTransform(row.PreviousTransform, row.Transform, _interpolationAlpha),
                        SurfaceIndices = row.SkinnedDesc.SurfaceIndices,
                        Materials = row.SkinnedDesc.Materials
                    });
                    break;
                case ProxyKind.Light:
                    lights.Add(row.Light);
                    break;
            }
        }

        return new DefaultWorldRenderContext(view, extent, staticMeshes.ToArray(), skinnedMeshes.ToArray(),
            lights.ToArray());
    }

    public void Build(IGraphBuilder builder, IWorldRenderContext context)
    {
        if (context is not DefaultWorldRenderContext ctx)
            throw new ArgumentException($"Expected {nameof(DefaultWorldRenderContext)}", nameof(context));

        builder.AddPass(new InitWorldPass(ctx));

        if (ctx.WillDoSkinning)
        {
            builder.AddPass(new SkinningPass(ctx));
            builder.AddPass(new BoundsUpdatePass(ctx));
        }

        {
            var cullingPass = new CullingPass(ctx);
            builder.AddPass(cullingPass);
            builder.AddPass(new FillIndirectBuffersPass(cullingPass, ctx));
        }

        builder.AddPass(new DepthPrepassIndirectPass(ctx));
        builder.AddPass(new FillGBufferIndirectPass(ctx));

        builder.AddPass(new LightingPass(ctx));
    }

    public void Dispose()
    {
    }

    private RenderProxyHandle Reserve()
    {
        uint index;
        if (_freeIndices.Count > 0)
        {
            index = _freeIndices.Pop();
        }
        else
        {
            index = (uint)_slots.Count;
            _slots.Add(null);
            _versions.Add(0);
        }

        var version = _versions[(int)index] += 1;
        return new RenderProxyHandle(index, version);
    }

    private void Set(RenderProxyHandle handle, ProxyRow row)
    {
        var index = handle.Index;
        if (index >= _versions.Count || _versions[(int)index] != handle.Version) return;
        _slots[(int)index] = row;
    }

    private void Mutate(RenderProxyHandle handle, Func<ProxyRow, ProxyRow> mutate)
    {
        var index = handle.Index;
        if (index >= _versions.Count || _versions[(int)index] != handle.Version || _slots[(int)index] is not { } row)
            return;
        _slots[(int)index] = mutate(row);
    }

    private void Free(RenderProxyHandle handle)
    {
        var index = handle.Index;
        if (index >= _versions.Count || _versions[(int)index] != handle.Version) return;

        _slots[(int)index] = null;
        _freeIndices.Push(index);
    }

    /// <summary>Fixed-timestep render smoothing: blends the last two committed physics poses by how far into the current step interval we are.</summary>
    private static Matrix4x4 InterpolateTransform(in Matrix4x4 previous, in Matrix4x4 current, float alpha)
    {
        if (previous == current) return current;
        if (!Matrix4x4.Decompose(previous, out var prevScale, out var prevRotation, out var prevPosition)) return current;
        if (!Matrix4x4.Decompose(current, out var curScale, out var curRotation, out var curPosition)) return current;

        return Matrix4x4.CreateScale(Vector3.Lerp(prevScale, curScale, alpha))
               * Matrix4x4.CreateFromQuaternion(Quaternion.Slerp(prevRotation, curRotation, alpha))
               * Matrix4x4.CreateTranslation(Vector3.Lerp(prevPosition, curPosition, alpha));
    }
}
