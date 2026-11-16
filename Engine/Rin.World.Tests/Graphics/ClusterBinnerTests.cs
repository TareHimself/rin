using System.Numerics;
using Rin.World.Graphics.Default;
using Rin.World.Graphics.Default.Shaders;

namespace Rin.World.Tests.Graphics;

public class ClusterBinnerTests
{
    private const float Near = 0.1f;
    private const float Far = 100f;

    private static ClusterBinner Begin()
    {
        var binner = new ClusterBinner();
        binner.Begin(Matrix4x4.Identity, Near, Far);
        return binner;
    }

    private static Vector3 At(float depth)
    {
        return new Vector3(0f, 0f, depth);
    }

    [Test]
    public void ItemsAreGroupedByKindThenSortedNearestFirst()
    {
        var binner = Begin();
        binner.Add(ClusterItemType.EnvironmentVolume, 0, At(5f), 1f);
        binner.Add(ClusterItemType.PointLight, 0, At(30f), 1f);
        binner.Add(ClusterItemType.EnvironmentVolume, 1, At(2f), 1f);
        binner.Add(ClusterItemType.PointLight, 1, At(10f), 1f);

        binner.Build();

        var order = binner.Items.ToArray().Select(item => (item.Type, item.Index)).ToArray();
        Assert.That(order, Is.EqualTo(new[]
        {
            (ClusterItemType.PointLight, 1u),
            (ClusterItemType.PointLight, 0u),
            (ClusterItemType.EnvironmentVolume, 1u),
            (ClusterItemType.EnvironmentVolume, 0u)
        }));
    }

    [Test]
    public void EachKindOwnsOneBlockOfTheList()
    {
        var binner = Begin();
        binner.Add(ClusterItemType.EnvironmentVolume, 0, At(5f), 1f);
        binner.Add(ClusterItemType.PointLight, 0, At(30f), 1f);
        binner.Add(ClusterItemType.PointLight, 1, At(10f), 1f);

        binner.Build();

        Assert.Multiple(() =>
        {
            Assert.That(binner.StartOf(ClusterItemType.PointLight), Is.EqualTo(0));
            Assert.That(binner.CountOf(ClusterItemType.PointLight), Is.EqualTo(2));
            Assert.That(binner.StartOf(ClusterItemType.EnvironmentVolume), Is.EqualTo(2));
            Assert.That(binner.CountOf(ClusterItemType.EnvironmentVolume), Is.EqualTo(1));
        });
    }

    [Test]
    public void AKindsBinsOnlyHoldIndicesFromItsOwnBlock()
    {
        var binner = Begin();
        binner.Add(ClusterItemType.PointLight, 0, At(20f), 5f);
        binner.Add(ClusterItemType.EnvironmentVolume, 0, At(20f), 5f);

        binner.Build();

        var bin = ClusterBinner.BinOf(20f, Near, Far);
        Assert.Multiple(() =>
        {
            Assert.That(ClusterBinner.TryGetRange(binner.BinsOf(ClusterItemType.PointLight)[bin], out var first,
                out var last));
            Assert.That((first, last), Is.EqualTo((0, 0)));
            Assert.That(ClusterBinner.TryGetRange(binner.BinsOf(ClusterItemType.EnvironmentVolume)[bin], out first,
                out last));
            Assert.That((first, last), Is.EqualTo((1, 1)));
        });
    }

    [Test]
    public void BinsOfEveryKindSitOneAfterAnother()
    {
        var binner = Begin();
        binner.Add(ClusterItemType.EnvironmentVolume, 0, At(20f), 5f);

        binner.Build();

        var bin = ClusterBinner.BinOf(20f, Near, Far);
        var volumeBins = (int)ClusterItemType.EnvironmentVolume * ClusterBinner.BinCount;
        Assert.Multiple(() =>
        {
            Assert.That(binner.Bins.Length, Is.EqualTo(ClusterBinner.TypeCount * ClusterBinner.BinCount));
            Assert.That(binner.Bins[volumeBins + bin], Is.EqualTo(binner.BinsOf(ClusterItemType.EnvironmentVolume)[bin]));
            Assert.That(ClusterBinner.TryGetRange(binner.Bins[bin], out _, out _), Is.False, "no point light there");
        });
    }

    [Test]
    public void ItemsBehindTheCameraSortBeforeItemsInFront()
    {
        var binner = Begin();
        binner.Add(ClusterItemType.PointLight, 0, At(3f), 10f);
        binner.Add(ClusterItemType.PointLight, 1, At(-3f), 10f);

        binner.Build();

        Assert.That(binner.Items[0].Index, Is.EqualTo(1u));
    }

    [Test]
    public void ItemsEntirelyOutOfRangeAreDropped()
    {
        var binner = Begin();
        binner.Add(ClusterItemType.PointLight, 0, At(-5f), 1f);
        binner.Add(ClusterItemType.EnvironmentVolume, 0, At(Far + 5f), 1f);

        binner.Build();

        Assert.That(binner.Items.Length, Is.Zero);
    }

    [Test]
    public void BeginStartsAgainFromNothing()
    {
        var binner = Begin();
        binner.Add(ClusterItemType.PointLight, 0, At(20f), 1f);
        binner.Build();

        binner.Begin(Matrix4x4.Identity, Near, Far);
        binner.Build();

        Assert.Multiple(() =>
        {
            Assert.That(binner.Items.Length, Is.Zero);
            foreach (var bin in binner.Bins) Assert.That(ClusterBinner.TryGetRange(bin, out _, out _), Is.False);
        });
    }
}
