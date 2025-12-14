using System.Numerics;
using Rin.Framework.Graphics;
using Rin.Framework.Views;

namespace Rin.Engine.World.Components;
using Framework.Views.Graphics;
/// <summary>
/// Renders a 3d <see cref="Surface"/>
/// </summary>
public class SurfaceComponent : WorldComponent
{
    public class VirtualSurface(SurfaceComponent component) : Surface
    {
        public override Vector2 GetCursorPosition()
        {
            return Vector2.Zero;
        }

        public override void SetCursorPosition(Vector2 position)
        {
            
        }

        public override void StartTyping(View view)
        {
            
        }

        public override void StopTyping(View view)
        {
            
        }

        public override Vector2 GetSize()
        {
            return new Vector2(component.Extent.Width, component.Extent.Height);
        }

        // public SurfacePassContext? Build()
        // {
        //     return; //BuildPasses()
        // }
    }
    /// <summary>
    /// The size to render the surface at
    /// </summary>
    public Extent2D Extent { get; set; }
    
    public VirtualSurface Surface { get; }


    public SurfaceComponent()
    {
        Surface = new VirtualSurface(this);
    }

    public override void Collect(Graphics.CommandList commandList, Matrix4x4 parentTransform)
    {
        base.Collect(commandList, parentTransform);
    }
}