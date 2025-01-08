using rin.Framework.Core;
using rin.Framework.Graphics;
using rin.Framework.Graphics.Windows;
using rin.Framework.Scene;
using rin.Framework.Views;
using rin.Framework.Views.Composite;
using rin.Framework.Views.Content;
using rin.Framework.Views.Layouts;
using SceneTest.entities;

namespace SceneTest;

[Module(typeof(SSceneModule), typeof(SViewsModule)),AlwaysLoad]
public class SSceneTestModule : IModule
{
    public void Startup(SRuntime runtime)
    {
        
        var scene = SSceneModule.Get().CreateScene();
        var camera = scene.AddEntity<CameraEntity>();
        
        runtime.OnTick += (_) => { SGraphicsModule.Get().PollWindows(); };

        SViewsModule.Get().OnSurfaceCreated += (surf) =>
        {
            var window = surf.GetRenderer().GetWindow();
            
            window.OnCloseRequested += (_) =>
            {
                if (window.Parent != null)
                {
                    window.Dispose();
                }
                else
                {
                    SRuntime.Get().RequestExit();
                }
            };
            
            var text = new TextBox("Selected Channel Text", inFontSize: 50);
            
            surf.Add(new Panel()
            {
                Slots =
                [
                    new PanelSlot()
                    {
                        Child = new Viewport(camera, text),
                        MinAnchor = 0.0f,
                        MaxAnchor = 1.0f,
                    },
                    new PanelSlot()
                    {
                        Child = text,
                        SizeToContent = true
                    }
                ]
            });
        };
        
        Task.Factory.StartNew(() =>
        {
            while (SRuntime.Get().IsRunning)
            {
                SGraphicsModule.Get().DrawWindows();
            }
        }, TaskCreationOptions.LongRunning);

        SGraphicsModule.Get().CreateWindow(500, 500, "Rin Scene Test", new CreateOptions()
        {
            Visible = true,
            Decorated = true,
            Transparent = true
        });
    }

    public void Shutdown(SRuntime runtime)
    {
    }
}