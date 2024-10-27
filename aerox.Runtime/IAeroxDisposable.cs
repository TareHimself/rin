namespace aerox.Runtime;

public interface IAeroxDisposable : IDisposable
{
    public bool Disposed { get; protected set; } 
    
    public abstract string DisposeId { get;}
}