namespace Rin.Core.Graphics.Windows;

public enum WindowHitTestResult
{
    None,
    DragArea,
    TopResize,
    LeftResize,
    RightResize,
    BottomResize,
    CloseButton,
    MinimizeButton,
    MaximizeButton
}