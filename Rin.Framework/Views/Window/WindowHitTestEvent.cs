using Rin.Framework.Graphics.Windows;
using Rin.Framework.Views.Events;
using Rin.Framework.Views.Graphics;

namespace Rin.Framework.Views.Window;

public class WindowHitTestEvent(Surface surface) : IHandleableEvent
{
    public WindowHitTestResult HitResult { get; set; }
    public Surface Surface { get; } = surface;
    public bool Handled => HitResult != WindowHitTestResult.None;
}