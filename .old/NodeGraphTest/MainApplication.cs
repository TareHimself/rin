using System.Numerics;
using Rin.Framework;
using Rin.Framework.Audio;
using Rin.Framework.Audio.Bass;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Vulkan;
using Rin.Framework.Graphics.Windows;
using Rin.Framework.Views;
using Rin.Framework.Views.Composite;
using Rin.Framework.Views.Content;
using Rin.Framework.Views.Graphics.Quads;
using Rin.Framework.Views.Layouts;
using Rin.Framework.Views.Window;

namespace NodeGraphTest;

public class MainApplication : Application
{
    public override IGraphicsModule CreateGraphicsModule() => new VulkanGraphicsModule();

    public override IViewsModule CreateViewsModule() => new ViewsModule();

    public override IAudioModule CreateAudioModule() => new BassAudioModule();

    protected override void OnStartup()
    {
        IGraphicsModule.Get().OnWindowCreated += OnWindowCreated;
        IViewsModule.Get().OnSurfaceCreated += OnSurfaceCreated;
        IGraphicsModule.Get().CreateWindow("Yoga Demo", new Extent2D(500), WindowFlags.Resizable | WindowFlags.Visible);
    }

    protected override void OnShutdown()
    {
    }

    protected void OnWindowCreated(IWindow window)
    {
        window.OnClose += _ =>
        {
            if (window.Parent is null)
            {
                RequestExit();
            }
        };
    }


    protected void OnSurfaceCreated(IWindowSurface surface)
    {
        // surface.Add(new FlexBoxView
        // {
        //     Axis = Axis.Row,
        //     Slots =
        //     [
        //         new FlexBoxSlot
        //         {
        //             Child = new GraphView(),
        //             Flex = 1,
        //             Fit = CrossFit.Fill
        //         },
        //         new FlexBoxSlot
        //         {
        //             Child = new GraphView(),
        //             Flex = 1,
        //             Fit = CrossFit.Fill
        //         }
        //     ]
        // });
        surface.Add(new GraphView());
        // surface.Add(new CanvasView
        // {
        //     Paint = (self, transform, cmds) =>
        //     {
        //         //cmds.AddRect(Matrix4x4.Identity.Translate(new Vector2(0)).ChildOf(transform),new Vector2(100f), Color.Red, borderRadius: new Vector4(50.0f));
        //         //cmds.AddCircle(transform,new Vector2(50), 50, Color.Green);
        //         // cmds.AddLine(transform, new Vector2(50f), new Vector2(50,100),thickness: 8,color: Color.Blue);
        //         // cmds.AddQuadraticCurve(transform,new Vector2(2.5f),new Vector2(100f - 2.5f),new Vector2(0,100f),thickness: 5,color: Color.White);
        //         cmds.AddCubicCurve(transform,new Vector2(2.5f),new Vector2(100f - 2.5f),new Vector2(100,0),new Vector2(0,100),thickness: 5,color: Color.White);
        //     }
        // });
        // surface.Add(new CanvasView
        // {
        //     Paint = (self, transform, commands) =>
        //     {
        //         var size = new Vector2(400.0f);
        //         commands.AddRect(transform.Translate(new Vector2(50.0f)),size);
        //         commands.AddCircle(transform.Translate(new Vector2(50.0f)), size.X / 2f,Color.Green);
        //     }
        // });
    }
}