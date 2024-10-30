using rin.Editor.Assets;
using rin.Editor.Modules;
using rin.Core;
using rin.Core.Extensions;
using rin.Scene;
using rin.Scene.Graphics;

namespace rin.Editor;

[RuntimeModule(typeof(SSceneModule), typeof(AssetsModule))]
public class ProgramSceneTestModule : RuntimeModule
{
    public override void Startup(SRuntime runtime)
    {
        base.Startup(runtime);
        var smFactory = AssetsModule.Get().NewFactory<StaticMeshFactory>();
        AssetsModule.Get().NewFactory<MaterialFactory>();
        smFactory.Import(@"D:\test.glb").Then(smAsset =>
        {
            var smInstance = (StaticMesh?)smAsset?.Resolve();
            Console.WriteLine("YOOOOOOOO");
            smInstance?.Dispose();
        });
    }
}