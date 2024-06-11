using aerox.Runtime;
using aerox.Runtime.Graphics;
using aerox.Runtime.Math;
using aerox.Runtime.Scene;
using aerox.Runtime.Widgets;
using aerox.Runtime.Windows;
using SceneTest.entities;

namespace SceneTest;

[RuntimeModule(typeof(SSceneModule),typeof(SWidgetsModule))]
public class SSceneTestModule : RuntimeModule
{
    public override void Startup(SRuntime runtime)
    {
        base.Startup(runtime);

        SWindowsModule.Get().CreateWindow(500, 500, "Aerox Scene Test");
        var scene = SSceneModule.Get().AddScene<TestScene>();

        var surf = SWidgetsModule.Get().GetWindowSurface();

        var viewport = surf?.Add(new Viewport(scene));
        var entity = scene.AddEntity<CameraEntity>();
        var meshEntity = scene.AddEntity<MeshEntity>();
        scene.SetViewTarget(entity);

    }
}