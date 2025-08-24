using ManagedBass;

namespace Rin.Framework.Audio.BassAudio;

public class Sync
{
    private readonly SyncProcedure _managedBassSyncProcedure;


    public Sync()
    {
        _managedBassSyncProcedure = Call;
    }

    public event Action<int, int, int, IntPtr>? OnSync;

    public static implicit operator SyncProcedure(Sync s)
    {
        return s._managedBassSyncProcedure;
    }

    private void Call(int handle, int channel, int data, IntPtr user)
    {
        OnSync?.Invoke(handle, channel, data, user);
    }
}