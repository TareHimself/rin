namespace aerox.Runtime.Graphics;

public abstract class FrameState
{
    public abstract void Apply(Frame frame);
    
    public virtual void OnPush(Frame frame)
    {
        
    }
    
    public virtual void OnPop(Frame frame)
    {
        
    }
}