using System.Collections.Concurrent;
using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Shared.Pooling;
using Rin.World.Mesh.Skinning;

namespace Rin.World.Graphics.Default;

/// <summary>
///     Proxy table fed by a command queue drained once per <see cref="Snapshot" />; the render thread
///     never touches the live table, only the frozen copy each scene snapshot holds.
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

    private enum CommandKind
    {
        CreateStaticMesh,
        CreateSkinnedMesh,
        CreateLight,
        UpdateTransform,
        UpdatePose,
        UpdateLight,
        Destroy
    }

    /// <summary>
    ///     One queued change to the proxy table. Only the fields its kind uses are set.
    /// </summary>
    private struct ProxyCommand
    {
        public CommandKind Kind;
        public RenderProxyHandle Handle;
        public Matrix4x4 Transform;
        public StaticMeshProxyDesc StaticDesc;
        public SkinnedMeshProxyDesc SkinnedDesc;
        public LightInfo Light;
        public SkeletalPose Pose;
    }

    /// <summary>
    ///     How views taken from this system issue mesh draws. Defaults to the device's capability.
    /// </summary>
    public MeshDrawMode DrawMode { get; set; } = MeshDrawMode.Auto;

    private readonly List<ProxyRow?> _slots = [];
    private readonly List<uint> _versions = [];
    private readonly Stack<uint> _freeIndices = new();
    private readonly ConcurrentQueue<ProxyCommand> _commands = new();
    private float _interpolationAlpha = 1f;
    private float _time;
    private DefaultWorldSnapshot? _scene;
    private readonly Recycler<DefaultWorldSnapshot> _scenes;
    private float _sceneAlpha;

    public DefaultRenderSystem()
    {
        _scenes = new Recycler<DefaultWorldSnapshot>(() => new DefaultWorldSnapshot(this));
    }

    public RenderProxyHandle CreateStaticMeshProxy(in StaticMeshProxyDesc desc)
    {
        var handle = Reserve();
        _commands.Enqueue(new ProxyCommand { Kind = CommandKind.CreateStaticMesh, Handle = handle, StaticDesc = desc });
        return handle;
    }

    public RenderProxyHandle CreateSkinnedMeshProxy(in SkinnedMeshProxyDesc desc)
    {
        var handle = Reserve();
        _commands.Enqueue(new ProxyCommand
            { Kind = CommandKind.CreateSkinnedMesh, Handle = handle, SkinnedDesc = desc });
        return handle;
    }

    public RenderProxyHandle CreateLightProxy(in LightInfo desc)
    {
        var handle = Reserve();
        _commands.Enqueue(new ProxyCommand { Kind = CommandKind.CreateLight, Handle = handle, Light = desc });
        return handle;
    }

    public void UpdateProxyTransform(RenderProxyHandle handle, in Matrix4x4 worldTransform)
    {
        _commands.Enqueue(new ProxyCommand
            { Kind = CommandKind.UpdateTransform, Handle = handle, Transform = worldTransform });
    }

    public void UpdateSkinnedProxyPose(RenderProxyHandle handle, in SkeletalPose pose)
    {
        _commands.Enqueue(new ProxyCommand { Kind = CommandKind.UpdatePose, Handle = handle, Pose = pose });
    }

    public void SetInterpolationAlpha(float alpha)
    {
        _interpolationAlpha = float.Clamp(alpha, 0f, 1f);
    }

    public void SetTime(float seconds)
    {
        _time = seconds;
    }

    public void UpdateLightProxy(RenderProxyHandle handle, in LightInfo desc)
    {
        _commands.Enqueue(new ProxyCommand { Kind = CommandKind.UpdateLight, Handle = handle, Light = desc });
    }

    public void DestroyProxy(RenderProxyHandle handle)
    {
        _commands.Enqueue(new ProxyCommand { Kind = CommandKind.Destroy, Handle = handle });
    }

    public IWorldCollectedData Snapshot(in CameraView view, in Extent2D extent)
    {
        var changed = false;
        while (_commands.TryDequeue(out var command))
        {
            Apply(command);
            changed = true;
        }

        if (changed || _scene is null || _sceneAlpha != _interpolationAlpha)
        {
            var scene = BuildScene();
            scene.Acquire();
            _scene?.Release();
            _scene = scene;
            _sceneAlpha = _interpolationAlpha;
        }

        return new DefaultWorldViewData(view, extent, _scene) { DrawMode = DrawMode, Time = _time };
    }

    private DefaultWorldSnapshot BuildScene()
    {
        var scene = _scenes.Take();
        foreach (var slot in _slots)
        {
            if (slot is not { } row) continue;
            switch (row.Kind)
            {
                case ProxyKind.StaticMesh:
                    scene.AddStaticMesh(row.StaticDesc,
                        InterpolateTransform(row.PreviousTransform, row.Transform, _interpolationAlpha));
                    break;
                case ProxyKind.SkinnedMesh:
                    scene.AddSkinnedMesh(row.SkinnedDesc,
                        InterpolateTransform(row.PreviousTransform, row.Transform, _interpolationAlpha));
                    break;
                case ProxyKind.Light:
                    scene.AddLight(row.Light);
                    break;
            }
        }

        return scene;
    }

    /// <summary>
    ///     Takes back a scene nothing holds any more. Called from whichever thread released it last.
    /// </summary>
    internal void Recycle(DefaultWorldSnapshot scene)
    {
        _scenes.Return(scene);
    }

    public void Dispose()
    {
        _scene?.Release();
        _scene = null;
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

    private void Apply(in ProxyCommand command)
    {
        var handle = command.Handle;
        switch (command.Kind)
        {
            case CommandKind.CreateStaticMesh:
                Set(handle, new ProxyRow(ProxyKind.StaticMesh, command.StaticDesc.Transform)
                    { StaticDesc = command.StaticDesc });
                break;
            case CommandKind.CreateSkinnedMesh:
                Set(handle, new ProxyRow(ProxyKind.SkinnedMesh, command.SkinnedDesc.Transform)
                    { SkinnedDesc = command.SkinnedDesc });
                break;
            case CommandKind.CreateLight:
                Set(handle, new ProxyRow(ProxyKind.Light, Matrix4x4.Identity) { Light = command.Light });
                break;
            case CommandKind.UpdateTransform:
                if (TryGet(handle, out var moved)) Set(handle, WithTransform(moved, command.Transform));
                break;
            case CommandKind.UpdatePose:
                if (!TryGet(handle, out var posed)) break;
                posed.SkinnedDesc = posed.SkinnedDesc with { Pose = command.Pose };
                Set(handle, posed);
                break;
            case CommandKind.UpdateLight:
                if (!TryGet(handle, out var lit)) break;
                lit.Light = command.Light;
                Set(handle, lit);
                break;
            case CommandKind.Destroy:
                Free(handle);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(command), command.Kind, null);
        }
    }

    private static ProxyRow WithTransform(ProxyRow row, in Matrix4x4 transform)
    {
        // Only roll Previous forward on a real change.
        if (row.Transform != transform) row.PreviousTransform = row.Transform;
        row.Transform = transform;
        if (row.Kind == ProxyKind.StaticMesh) row.StaticDesc = row.StaticDesc with { Transform = transform };
        if (row.Kind == ProxyKind.SkinnedMesh) row.SkinnedDesc = row.SkinnedDesc with { Transform = transform };
        return row;
    }

    private bool IsCurrent(RenderProxyHandle handle)
    {
        return handle.Index < _versions.Count && _versions[(int)handle.Index] == handle.Version;
    }

    private void Set(RenderProxyHandle handle, ProxyRow row)
    {
        if (IsCurrent(handle)) _slots[(int)handle.Index] = row;
    }

    private bool TryGet(RenderProxyHandle handle, out ProxyRow row)
    {
        row = default;
        if (!IsCurrent(handle) || _slots[(int)handle.Index] is not { } existing) return false;
        row = existing;
        return true;
    }

    private void Free(RenderProxyHandle handle)
    {
        if (!IsCurrent(handle)) return;

        _slots[(int)handle.Index] = null;
        _freeIndices.Push(handle.Index);
    }

    /// <summary>
    ///     Blends the last two committed physics poses by how far into the current fixed step we are.
    /// </summary>
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
