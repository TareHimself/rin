using Rin.Core.Graphics.Windows;
using Rin.Core.Views.Events;
using Rin.Core.Views.Graphics;

namespace Rin.Core.Views.Window;

public class WindowHitTestEvent(ISurface surface) : IHandleableEvent
{
    public WindowHitTestResult HitResult { get; set; }
    public ISurface Surface { get; } = surface;
    public bool Handled => HitResult != WindowHitTestResult.None;
}