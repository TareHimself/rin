using aerox.Runtime;
using aerox.Runtime.Widgets;
using aerox.Runtime.Widgets.Content;
using aerox.Runtime.Widgets.Events;
using aerox.Runtime.Widgets.Graphics;
using aerox.Runtime.Widgets.Graphics.Commands;
using MathNet.Numerics;

namespace AudioPlayer.Widgets;


public class FpsWidget : Text
{
    
    public class CheckInfoCommand(FpsWidget widget) : UtilityCommand
    {
        public override void Run(WidgetFrame frame)
        {
            widget.Content = $"""
                       {(1.0 / SRuntime.Get().GetLastDeltaSeconds()).Round(2)} FPS
                       {(frame.Raw.Renderer.LastFrameTime * 1000).Round(2)}ms
                       {frame.DrawCommandList.Count - 1} commands
                       """;
        }
    }
    public override void Collect(WidgetFrame frame, TransformInfo info)
    {
        frame.AddCommands(new CheckInfoCommand(this));
        base.Collect(frame, info);
    }
}