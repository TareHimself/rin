using Rin.Engine.Graphics.Windows;
using Rin.Engine.Views.Events;
using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views.Window;

public class WindowHitTestEvent(Surface surface) : IHandleableEvent
{
    public Surface Surface { get; } = surface;
    public bool Handled => HitResult != WindowHitTestResult.None;
    
    public WindowHitTestResult HitResult { get; set; }
}