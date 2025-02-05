using rin.Runtime.Core;

namespace rin.Scene.Graphics.Commands;

public abstract class Command : Disposable
{
    public abstract void Run(SceneFrame frame);

    protected override void OnDispose(bool isManual)
    {
        
    }
}