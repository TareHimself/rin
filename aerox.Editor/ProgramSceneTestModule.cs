using aerox.Editor.Assets;
using aerox.Editor.Modules;
using aerox.Runtime;
using aerox.Runtime.Extensions;
using aerox.Runtime.Scene;
using aerox.Runtime.Scene.Graphics;

namespace aerox.Editor;

[RuntimeModule(typeof(SceneModule),typeof(AssetsModule))]
public class ProgramSceneTestModule : RuntimeModule
{
    public override void Startup(Runtime.Runtime runtime)
    {
        base.Startup(runtime);
        var smFactory = AssetsModule.Get().NewFactory<StaticMeshFactory>();
        AssetsModule.Get().NewFactory<MaterialFactory>();
        smFactory.Import(@"D:\test.glb").Then((smAsset) =>
        {
            var smInstance = (StaticMesh?)smAsset?.Resolve();
            Console.WriteLine("YOOOOOOOO");
            smInstance?.Dispose();
        });
    }
}