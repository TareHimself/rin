using Rin.Framework.Graphics.Windows;
using Rin.Framework.Views.Events;
using Rin.Framework.Views.Graphics;

namespace Rin.Framework.Views.Window;

public class WindowHitTestEvent(ISurface surface) : IHandleableEvent
{
    public WindowHitTestResult HitResult { get; set; }
    public ISurface Surface { get; } = surface;
    public bool Handled => HitResult != WindowHitTestResult.None;
}