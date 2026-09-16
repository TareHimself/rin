using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Graphics;
using Rin.World.Math;

namespace Rin.World.Components.Lights;

public class DirectionalLightComponent : LightComponent
{
    private RenderProxyHandle _proxy = RenderProxyHandle.Invalid;
    private uint _lastPushedVersion;

    public override void Start()
    {
        base.Start();
        _proxy = Owner!.World!.RenderSystem.CreateLightProxy(BuildLightInfo());
        _lastPushedVersion = TransformVersion;
    }

    public override void Stop()
    {
        if (_proxy.IsValid)
        {
            Owner!.World!.RenderSystem.DestroyProxy(_proxy);
            _proxy = RenderProxyHandle.Invalid;
        }

        base.Stop();
    }

    public override void LateUpdate(float deltaSeconds)
    {
        base.LateUpdate(deltaSeconds);
        if (!_proxy.IsValid) return;
        GetTransform(Space.World);
        if (TransformVersion != _lastPushedVersion)
        {
            Owner!.World!.RenderSystem.UpdateLightProxy(_proxy, BuildLightInfo());
            _lastPushedVersion = TransformVersion;
        }
    }

    private LightInfo BuildLightInfo()
    {
        var worldTransform = GetTransform(Space.World);
        return new LightInfo
        {
            Color = Color,
            Direction = worldTransform.Orientation.GetForward(),
            Radiance = Radiance,
            Radius = Radius,
            LightType = LightType.Directional,
            Location = worldTransform.Position
        };
    }

    public override void Collect(CommandList commandList, Matrix4x4 parentTransform)
    {
        var sceneTransform = Transform.From(GetTransform().ToMatrix() * parentTransform);
        commandList.AddLight(new LightInfo
        {
            Color = Color,
            Direction = sceneTransform.Orientation.GetForward(),
            Radiance = Radiance,
            Radius = Radius,
            LightType = LightType.Directional,
            Location = sceneTransform.Position
        });
        base.Collect(commandList, parentTransform);
    }
}
