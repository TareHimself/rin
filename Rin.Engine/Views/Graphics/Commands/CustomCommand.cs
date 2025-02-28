using Rin.Engine.Graphics;

namespace Rin.Engine.Views.Graphics.Commands;

/// <summary>
///     Base class for custom commands that will run in the ViewsPass
/// </summary>
public abstract class CustomCommand : Command
{
    public abstract void Run(ViewsFrame frame, uint stencilMask, IDeviceBufferView? view = null);

    public abstract ulong GetRequiredMemory();

    public abstract bool WillDraw();

    public virtual bool CombineWith(CustomCommand other)
    {
        return false;
    }
}