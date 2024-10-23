using aerox.Runtime.Graphics;
using aerox.Runtime.Math;
using aerox.Runtime.Widgets.Events;
using aerox.Runtime.Windows;
using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Widgets.Graphics;

/// <summary>
///     A surface that displays widgets on a window
/// </summary>
public class WindowSurface : Surface
{
    private readonly WindowRenderer _renderer;
    public bool Minimized;
    public Vector2<int> Size;

    public WindowSurface(WindowRenderer renderer)
    {
        _renderer = renderer;
        Size = new Vector2<int>((int)Window.PixelSize.X, (int)Window.PixelSize.Y);
    }

    public Window Window => _renderer.GetWindow();

    public void CopyToSwapchain(Frame frame, VkImage swapchainImage, VkExtent2D extent)
    {
        var drawImage = GetDrawImage();
        SGraphicsModule.CopyImageToImage(frame.GetCommandBuffer(), drawImage.Image, swapchainImage,
            drawImage.Extent, _renderer.GetSwapchainExtent());
    }

    public override void Init()
    {
        base.Init();
        Window.OnResized += OnWindowResize;
        Window.OnMouseButton += OnMouseButton;
        Window.OnMouseMoved += OnMouseMove;
        Window.OnScrolled += OnScroll;
        _renderer.OnDraw += Draw;
        _renderer.OnCopyToSwapchain += CopyToSwapchain;
    }

    protected void OnWindowResize(Window.ResizeEvent e)
    {
        Size = new Vector2<int>((int)e.Width, (int)e.Height);
        Minimized = Size.X == 0 || Size.Y == 0;
        if (!Minimized) ReceiveResize(new ResizeEvent(this, Size.Clone()));
    }

    protected void OnMouseButton(Window.MouseButtonEvent e)
    {
        switch (e.State)
        {
            case KeyState.Released:
                ReceiveCursorUp(new CursorUpEvent(this,e.Button,e.Position.Cast<float>()));
                break;
            case KeyState.Pressed:
                ReceiveCursorDown(new CursorDownEvent(this,e.Button, e.Position.Cast<float>()));
                break;
            case KeyState.Repeat:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected void OnMouseMove(Window.MouseMoveEvent e)
    {
        ReceiveCursorMove(new CursorMoveEvent(this, e.Position.Cast<float>()));
    }

    protected void OnScroll(Window.ScrollEvent e)
    {
        ReceiveScroll(new ScrollEvent(this, e.Position.Cast<float>(), e.Delta.Cast<float>()));
    }


    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
        Window.OnResized -= OnWindowResize;
        Window.OnMouseButton -= OnMouseButton;
        Window.OnMouseMoved -= OnMouseMove;
        Window.OnScrolled -= OnScroll;

        _renderer.OnDraw -= Draw;
        _renderer.OnCopyToSwapchain -= CopyToSwapchain;
    }

    public override void Draw(Frame frame)
    {
        if (!Minimized) base.Draw(frame);
    }

    public override Vector2<float> GetCursorPosition()
    {
        return Window.GetMousePosition().Cast<float>();
    }

    public override void SetCursorPosition(Vector2<float> position)
    {
        Window.SetMousePosition(position.Cast<double>());
    }

    public override Vector2<int> GetDrawSize()
    {
        return Size;
    }
}