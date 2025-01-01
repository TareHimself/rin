using rin.Framework.Core;
using rin.Framework.Graphics;
using rin.Framework.Graphics.Windows;
using rin.Framework.Scene;
using rin.Framework.Views;
using rin.Framework.Views.Composite;
using rin.Framework.Views.Content;

using SceneTest.entities;

namespace SceneTest;

[Module(typeof(SSceneModule),typeof(SViewsModule))]
public class SSceneTestModule : IModule
{
    public void Startup(SRuntime runtime)
    {
        runtime.OnTick += (_) =>
        {
            SGraphicsModule.Get().PollWindows();
        };
        
        Task.Factory.StartNew(() =>
        {
            while (SRuntime.Get().IsRunning)
            {
                SGraphicsModule.Get().DrawWindows();
            }
        },TaskCreationOptions.LongRunning);
        
        var window = SGraphicsModule.Get().CreateWindow(500, 500, "Rin Scene Test", new CreateOptions()
        {
            Visible = true,
            Decorated = true,
            Transparent = true
        });
        
        var scene = SSceneModule.Get().CreateScene();

        if (SViewsModule.Get().GetWindowSurface(window) is { } surf)
        {
            
        }
        
        // var text = new TextBox();
        //
        // var viewport = surf?.Add(new Viewport(scene,text));
        //
        // var panel = surf?.Add(new Panel());
        //
        // var textSlot = panel?.AddChild(text);
        //
        // textSlot?.Mutate((c) =>
        // {
        //     c.SizeToContent = true;
        //     c.MinAnchor = 0.0f;
        //     c.MaxAnchor = 0.0f;
        // });
        //
        // var entity = scene.AddEntity<CameraEntity>();
        // var meshEntity = scene.AddEntity<MeshEntity>();
        // scene.SetViewTarget(entity);
        //
        // window.OnKey += (k) =>
        // {
        //     if (k.Key is Key.Left or Key.Right)
        //     {
        //         if (k.Key is Key.Left)
        //         {
        //             
        //         }
        //         else
        //         {
        //             
        //         }
        //     }
        // };
    }

    public void Shutdown(SRuntime runtime)
    {
        
    }
}