using aerox.Runtime.Graphics;
using aerox.Runtime.Math;
using aerox.Runtime.Widgets.Events;
using aerox.Runtime.Windows;
using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Widgets;

/// <summary>
/// A surface that displays widgets on a window
/// </summary>
public class WidgetWindowSurface : WidgetSurface
{
    private readonly Graphics.WindowRenderer _renderer;
    public Vector2<int> Size;
    public bool Minimized = false;

    public Window Window => _renderer.GetWindow();

    public WidgetWindowSurface(WindowRenderer renderer) : base()
    {
        _renderer = renderer;
        Size = new((int)Window.PixelSize.width, (int)Window.PixelSize.height);
    }

    public void CopyToSwapchain(Frame frame, VkImage swapchainImage, VkExtent2D extent)
    {
        var drawImage = GetDrawImage();
        GraphicsModule.CopyImageToImage(frame.GetCommandBuffer(), drawImage.Image, swapchainImage,
            drawImage.Extent, _renderer.GetSwapchainExtent());
    }

    public override void Init()
    {
        base.Init();
        Window.OnResized += OnWindowResize;
        Window.OnMouseButton += OnMouseButton;
        Window.OnMouseMoved += OnMouseMove;
        Window.OnScrolled += OnScroll;
        _renderer.OnDrawSecondary += Draw;
        _renderer.OnCopyToSwapchain += CopyToSwapchain;
    }

    protected void OnWindowResize(Window.ResizeEvent e)
    {
        Size = new((int)e.Width, (int)e.Height);
        Minimized = Size.X == 0 || Size.Y == 0;
        if (!Minimized)
        {
            ReceiveResize(new ResizeEvent(this, Size.Clone()));
        }
    }

    protected void OnMouseButton(Window.MouseButtonEvent e)
    {
        switch (e.State)
        {
            case EKeyState.Released:
                ReceiveCursorUp(new CursorUpEvent(this, e.Position.Cast<float>()));
                break;
            case EKeyState.Pressed:
                ReceiveCursorDown(new CursorDownEvent(this, e.Position.Cast<float>()));
                break;
            case EKeyState.Repeat:
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

        _renderer.OnDrawSecondary -= Draw;
        _renderer.OnCopyToSwapchain -= CopyToSwapchain;
    }

    public override void Draw(Frame frame)
    {
        if (!Minimized)
        {
            base.Draw(frame);
        }
    }

    public override Vector2<float> GetCursorPosition() => Window.GetMousePosition().Cast<float>();

    public override Vector2<int> GetDrawSize() => Size;
}