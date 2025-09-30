using System.Numerics;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Graph;
using Rin.Framework.Graphics.Windows;
using Rin.Framework.Graphics.Windows.Events;
using Rin.Framework.Views.Events;
using Rin.Framework.Views.Graphics;
using Graphics_Windows_Events_CharacterEvent = Rin.Framework.Graphics.Windows.Events.CharacterEvent;
using Graphics_Windows_Events_CursorMoveEvent = Rin.Framework.Graphics.Windows.Events.CursorMoveEvent;
using Graphics_Windows_Events_ResizeEvent = Rin.Framework.Graphics.Windows.Events.ResizeEvent;
using Graphics_Windows_Events_ScrollEvent = Rin.Framework.Graphics.Windows.Events.ScrollEvent;

namespace Rin.Framework.Views.Window;

/// <summary>
///     A surface that displays views on a window
/// </summary>
public class WindowSurface : Surface, IWindowSurface
{
    private bool _minimized;
    private Extent2D _size;

    public WindowSurface(IWindowRenderer renderer)
    {
        Renderer = renderer;
        _size = Renderer.GetRenderExtent();
    }

    public IWindow Window => Renderer.GetWindow();

    public IWindowRenderer Renderer { get; }

    public override void Init()
    {
        base.Init();
        Window.OnCursorButton += OnMouseButton;
        Window.OnCursorMoved += OnMouseMove;
        Window.OnScroll += OnScroll;
        Window.OnKey += OnKeyboard;
        Window.OnCharacter += OnCharacter;
        Renderer.OnCollect += Collect;
        Window.OnResize += OnWindowResized;
        Window.OnCursorFocus += OnCursorFocus;
    }

    private void Collect(IGraphCollector collector)
    {
        if (CollectCommands() is { } cmds)
        {
            collector.Add(new WindowSurfaceCollectedData(cmds));
        }
    }

    protected void OnWindowResized(ResizeEvent e)
    {
        _size = Renderer.GetRenderExtent();
        _minimized = _size.Width == 0 || _size.Height == 0;
        if (!_minimized) ReceiveResize(new ResizeSurfaceEvent(this, new Vector2(_size.Width,_size.Height)));
    }

    protected void OnKeyboard(KeyEvent e)
    {
        ReceiveKeyboard(new KeyboardSurfaceEvent(this, e.Key, e.State));
    }

    protected void OnCharacter(CharacterEvent e)
    {
        ReceiveCharacter(new CharacterSurfaceEvent(this, e.Data, e.Modifiers));
    }

    protected void OnMouseButton(CursorButtonEvent e)
    {
        switch (e.State)
        {
            case InputState.Released:
                ReceiveCursorUp(new CursorUpSurfaceEvent(this, e.Button, e.Position));
                break;
            case InputState.Pressed:
                ReceiveCursorDown(new CursorDownSurfaceEvent(this, e.Button, e.Position));
                break;
            case InputState.Repeat:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected void OnMouseMove(CursorMoveEvent e)
    {
        ReceiveCursorMove(new CursorMoveSurfaceEvent(this, e.Position));
    }

    protected void OnScroll(ScrollEvent e)
    {
        ReceiveScroll(new ScrollSurfaceEvent(this, e.Position, e.Delta));
    }

    protected void OnCursorFocus(FocusEvent e)
    {
        if (e.IsFocused)
            ReceiveCursorEnter(new CursorMoveSurfaceEvent(this, Window.GetCursorPosition()));
        else
            ReceiveCursorLeave();
    }

    public override void Dispose()
    {
        base.Dispose();
        Window.OnResize -= OnWindowResized;
        Renderer.OnCollect -= Collect;
        Window.OnCursorButton -= OnMouseButton;
        Window.OnCursorMoved -= OnMouseMove;
        Window.OnScroll -= OnScroll;
        Window.OnKey -= OnKeyboard;
        Window.OnCharacter -= OnCharacter;
        Window.OnCursorFocus -= OnCursorFocus;
    }

    public override Vector2 GetCursorPosition()
    {
        return Window.GetCursorPosition();
    }

    public override void SetCursorPosition(Vector2 position)
    {
        Window.SetCursorPosition(position);
    }

    public override void StartTyping(View view)
    {
        Window.StartTyping();
    }

    public override void StopTyping(View view)
    {
        Window.StopTyping();
    }

    public override Vector2 GetSize()
    {
        return new Vector2(_size.Width, _size.Height);
    }
}