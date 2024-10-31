namespace rin.Widgets.Graphics.Commands;

public abstract class CustomCommand : GraphicsCommand
{
    public abstract void Run(WidgetFrame frame,uint stencilMask);
    
    /// <summary>
    /// Does this command plan to draw anything ?
    /// </summary>
    /// <returns></returns>
    public abstract bool WillDraw { get;  }
    
    public abstract CommandStage Stage { get; }
    public virtual bool CombineWith(CustomCommand other) => false;
}