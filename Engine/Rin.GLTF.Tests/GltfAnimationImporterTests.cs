using Rin.World.Mesh.Skinning;

namespace Rin.GLTF.Tests;

// The Fox asset has no CUBICSPLINE channels; that lossy-fallback path isn't exercised here.
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
    public void ImportsBothFullyAndSparselyAnimatedBones()
    {
        var clips = GltfAnimationImporter.LoadAnimationClips(FoxPath);

        var run = clips["Run"];
        var hip = run["b_Hip_01"];
        Assert.That(hip, Is.Not.Null, "the hip drives both translation and rotation");
        Assert.That(hip!.PositionCurve.PointCount, Is.GreaterThan(0));
        Assert.That(hip.RotationCurve.PointCount, Is.GreaterThan(0));

        var head = run["b_Head_05"];
        Assert.That(head, Is.Not.Null, "most bones in this asset only drive rotation");
        Assert.That(head!.RotationCurve.PointCount, Is.GreaterThan(0));
        Assert.That(head.PositionCurve.PointCount, Is.EqualTo(0),
            "no position channel should be fabricated for a bone that never authored one");
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
