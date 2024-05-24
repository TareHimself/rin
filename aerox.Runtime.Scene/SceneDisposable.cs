namespace aerox.Runtime.Scene;

public class SceneDisposable : Disposable, ITickable
{
    public readonly string InstanceId = Guid.NewGuid().ToString();
    protected bool HasStarted = false;
    public bool WillEverTick = true;
    
    public virtual void Start()
    {
        OnStart();
        HasStarted = true;
    }


    protected virtual void OnStart()
    {
        
    }
    protected override void OnDispose(bool isManual)
    {
        throw new NotImplementedException();
    }

    public void Tick(double deltaSeconds)
    {
        OnTick(deltaSeconds);
    }

    protected virtual void OnTick(double deltaSeconds)
    {
        
    }
}