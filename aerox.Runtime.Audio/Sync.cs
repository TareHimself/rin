using ManagedBass;

namespace aerox.Runtime.Audio;

public class Sync
{
    private readonly SyncProcedure _managedBassSyncProcedure;
    public event Action<int,int,int,IntPtr>? OnSync;
    
    public static implicit operator SyncProcedure(Sync s) => s._managedBassSyncProcedure;


    public Sync()
    { 
        _managedBassSyncProcedure = Call;
    }
    
    private void Call(int handle, int channel, int data, IntPtr user)
    {
        OnSync?.Invoke(handle,channel,data,user);
    }
}