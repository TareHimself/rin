using JetBrains.Annotations;
using rin.Framework.Graphics;
using rin.Framework.Graphics.FrameGraph;

namespace rin.Framework.Views.Graphics.Commands;

/// <summary>
/// Base class for custom commands that will run in the ViewsPass
/// </summary>
public abstract class CustomCommand : Command
{
    public abstract void Run(ViewsFrame frame, uint stencilMask, IDeviceBufferView? view = null);

    /// <summary>
    /// Does this command plan to draw anything ?
    /// </summary>
    /// <returns></returns>
    public virtual bool WillDraw => false;

    public virtual ulong MemoryNeeded => 0;
    
    public virtual bool CombineWith(CustomCommand other) => false;
}