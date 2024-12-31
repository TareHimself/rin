using rin.Framework.Graphics;

namespace rin.Framework.Views.Graphics.Commands;

public abstract class CustomCommand : GraphicsCommand
{
    public abstract void Run(ViewsFrame frame, uint stencilMask, IDeviceBuffer? buffer = null);

    /// <summary>
    /// Does this command plan to draw anything ?
    /// </summary>
    /// <returns></returns>
    public virtual bool WillDraw => false;

    public virtual ulong MemoryNeeded => 0;

    public virtual CommandStage Stage => CommandStage.Maintain;
    public virtual bool CombineWith(CustomCommand other) => false;
}