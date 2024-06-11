namespace aerox.Runtime.Scene;

public class SceneDisposable : Disposable, ITickable
{
    public readonly string InstanceId = Guid.NewGuid().ToString();
    public bool Active { get; protected set; }
    public bool WillEverTick = true;

    public void Tick(double deltaSeconds)
    {
        OnTick(deltaSeconds);
    }

    public virtual void Start()
    {
        OnStart();
        Active = true;
    }


    protected virtual void OnStart()
    {
    }

    protected override void OnDispose(bool isManual)
    {
        Active = false;
    }

    protected virtual void OnTick(double deltaSeconds)
    {
    }
}