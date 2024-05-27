namespace aerox.Runtime.Scene;

public class SceneDisposable : Disposable, ITickable
{
    public readonly string InstanceId = Guid.NewGuid().ToString();
    protected bool HasStarted;
    public bool WillEverTick = true;

    public void Tick(double deltaSeconds)
    {
        OnTick(deltaSeconds);
    }

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

    protected virtual void OnTick(double deltaSeconds)
    {
    }
}