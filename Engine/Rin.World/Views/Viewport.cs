using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Shaders;
using Rin.Core.Graphics.Windows;
using Rin.Core.Shared.Math;
using Rin.Core.Views;
using Rin.Core.Views.Events;
using Rin.Core.Views.Graphics.CommandHandlers;
using Rin.Core.Views.Graphics.Commands;
using Rin.Core.Views.Graphics.PassConfigs;
using Rin.Core.Views.Graphics.Quads;
using Rin.World.Graphics;
using Rin.World.Views.Shaders;
using CommandList = Rin.Core.Views.Graphics.CommandList;

namespace Rin.World.Views;

public class Viewport : ContentView
{
    /// <summary>
    ///     Frames the content size must hold steady before the render target follows it.
    /// </summary>
    public static int SettleFrames = 5;

    private readonly RenderFeature _render;
    private readonly Entity _cameraEntity;
    private bool _captureMouse;
    private RenderChannel _channel = RenderChannel.Scene;
    private bool _ignoreNextMove;
    private Vector2 _mousePosition;

    // Render-target size is debounced: it only follows the content size once that size has
    // settled, so a live resize (splitter / window drag) stretches the last render instead of
    // reallocating the render targets every frame.
    private Vector2 _renderSize;
    private Vector2 _lastContentSize;
    private int _settleFrames;

    /// <summary>
    ///     Renders <paramref name="render" />'s world from <paramref name="camera" />, an entity with a
    ///     <see cref="Camera" /> component.
    /// </summary>
    public Viewport(RenderFeature render, Entity camera)
    {
        _render = render;
        _cameraEntity = camera;
    }

    public override bool IsFocusable => true;

    protected Vector2 GetAbsoluteCenter()
    {
        return (GetContentSize() / 2.0f).Transform(ComputeAbsoluteContentTransform());
    }

    private string GetModeText()
    {
        return _channel switch
        {
            RenderChannel.Scene => "Default",
            RenderChannel.Color => "Color",
            RenderChannel.Location => "Location",
            RenderChannel.Normal => "Normal",
            RenderChannel.RoughnessMetallicSpecular => "Roughness Metallic Specular",
            RenderChannel.Emissive => "Emissive",
            RenderChannel.Radiance => "Radiance",
            RenderChannel.LightCount => "Light Count",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        return availableSpace;
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        var content = GetContentSize();
        if (content is { X: > 0f, Y: > 0f })
        {
            if (content == _lastContentSize)
            {
                if (_settleFrames < SettleFrames) _settleFrames++;
                if (_settleFrames >= SettleFrames) _renderSize = content;
            }
            else
            {
                _settleFrames = 0;
            }

            _lastContentSize = content;
            if (_renderSize is not { X: > 0f, Y: > 0f }) _renderSize = content;
        }
    }

    /// <summary>
    ///     Debounced size the world is actually rendered at.
    /// </summary>
    protected Vector2 GetRenderSize()
    {
        var content = GetContentSize();
        return _renderSize is { X: > 0f, Y: > 0f } ? _renderSize : content;
    }

    public override void OnCursorUp(CursorUpSurfaceEvent e)
    {
        if (_captureMouse)
        {
            _captureMouse = false;
            _ignoreNextMove = false;
            if (IsFocused) e.Surface.ClearFocus();
        }

        base.OnCursorUp(e);
    }

    public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
    {
        switch (e.Button)
        {
            case CursorButton.One:
            {
                var currentIdx = (int)_channel;
                currentIdx = (currentIdx + 1) % ((int)RenderChannel.LightCount + 1);
                _channel = (RenderChannel)currentIdx;
                e.Target = this;
                break;
            }
            case CursorButton.Two:
                _captureMouse = true;
                _ignoreNextMove = true;
                _mousePosition = GetAbsoluteCenter();
                e.Surface.SetCursorPosition(_mousePosition);
                e.Surface.RequestFocus(this);
                e.Target = this;
                break;
        }
    }

    public override void OnCursorMove(CursorMoveSurfaceEvent e, in Matrix4x4 transform)
    {
        if (_captureMouse && !_ignoreNextMove)
        {
            var delta = e.Position - _mousePosition;
            if (!(float.Abs(delta.X) > 0) && !(float.Abs(delta.Y) > 0)) return;

            OnMouseDelta(delta);

            _mousePosition = GetAbsoluteCenter();
            _ignoreNextMove = true;
            e.Surface.SetCursorPosition(_mousePosition);
            e.Target = this;
            return;
        }

        if (_ignoreNextMove)
        {
            _ignoreNextMove = false;
            _mousePosition = e.Position;
        }

        base.OnCursorMove(e, transform);
    }

    public override Vector2 ComputeDesiredContentSize()
    {
        return new Vector2();
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        // Runs on the collect (main) thread, inside the render barrier, so reading the world is safe.
        var context = Snapshot(GetRenderSize().ToExtent());
        commands.Add(new DrawViewportCommand(context, GetContentSize(), transform));
        commands.AddText(transform, "Noto Sans", GetModeText());
    }

    private IWorldCollectedData Snapshot(in Extent2D extent) =>
        _render.RenderSystem.Snapshot(_render.World.GetCameraView(_cameraEntity) with { Channel = _channel }, extent);

    protected virtual void OnMouseDelta(Vector2 delta)
    {
    }
}
