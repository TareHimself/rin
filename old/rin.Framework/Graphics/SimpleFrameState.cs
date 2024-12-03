namespace rin.Framework.Graphics;

public class SimpleFrameState(Action<Frame> apply) : FrameState
{
    public override void Apply(Frame frame) => apply.Invoke(frame);
}