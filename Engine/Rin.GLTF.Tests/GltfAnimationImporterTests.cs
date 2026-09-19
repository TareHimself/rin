using Rin.World.Mesh.Skinning;

namespace Rin.GLTF.Tests;

// The Fox asset only has STEP/LINEAR channels; the CUBICSPLINE lossy-fallback path isn't exercised here.
public class GltfAnimationImporterTests
{
    private static string FoxPath => Path.Combine(AppContext.BaseDirectory, "Assets", "fox.glb");

    [Test]
    public void LoadsEveryAnimationInTheFileByName()
    {
        var clips = GltfAnimationImporter.LoadAnimationClips(FoxPath);

        Assert.That(clips.Keys, Is.EquivalentTo(new[] { "Run", "Survey", "Walk" }));
    }

    [Test]
    public void EachClipAnimatesEveryJointOnAllThreeChannels()
    {
        var clips = GltfAnimationImporter.LoadAnimationClips(FoxPath);

        var run = clips["Run"];
        Assert.That(run["b_Hip_01"], Is.Not.Null, "a channel with LINEAR translation/rotation should import");
        Assert.That(run["b_Root_00"], Is.Not.Null, "a channel with STEP translation/rotation should import too");
    }

    [Test]
    public void BoundClipEvaluatesWithoutThrowingAcrossTheClipDuration()
    {
        var clips = GltfAnimationImporter.LoadAnimationClips(FoxPath);
        var run = clips["Run"];

        var bones = run.BoneCurves.Keys.Select(name => new Bone { Name = name }).ToArray();
        var skeleton = new Skeleton(bones);
        var bound = run.Bind(skeleton);

        Assert.That(() =>
        {
            for (var t = 0f; t <= 1.125f; t += 0.1f)
            {
                using var pose = bound.Evaluate(t);
            }
        }, Throws.Nothing);
    }
}
