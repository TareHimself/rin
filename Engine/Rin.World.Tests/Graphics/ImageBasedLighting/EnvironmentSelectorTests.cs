using System.Numerics;
using Rin.World.Graphics.ImageBasedLighting;

namespace Rin.World.Tests.Graphics.ImageBasedLighting;

public class EnvironmentSelectorTests
{
    private static readonly Vector3 Sky = new(1f, 0f, 0f);
    private static readonly Vector3 Room = new(0f, 1f, 0f);
    private static readonly Vector3 RoomCentre = new(100f, 0f, 0f);

    [Test]
    public void NoVolumesMeansNoEnvironment()
    {
        var environment = EnvironmentSelector.Select([], Vector3.Zero);

        Assert.Multiple(() =>
        {
            Assert.That(environment.HasCubemap, Is.False);
            Assert.That(environment.Info.Irradiance.L00, Is.EqualTo(Vector3.Zero));
            Assert.That(environment.Info.VolumeWeight, Is.EqualTo(0f));
        });
    }

    [Test]
    public void AnUnboundedLightIsTheFallback()
    {
        var sky = TestEnvironments.Unbounded(1, Sky);
        sky.Intensity = 2f;

        var environment = EnvironmentSelector.Select([sky], Vector3.Zero);

        Assert.Multiple(() =>
        {
            Assert.That(environment.FallbackCubemap, Is.EqualTo(sky.Map.Cubemap));
            Assert.That(environment.VolumeCubemap.Id, Is.Zero);
            Assert.That(environment.Info.Irradiance.L00, Is.EqualTo(Sky * 2f));
            Assert.That(environment.Info.Fallback.MaxMip, Is.EqualTo(1f));
            Assert.That(environment.Info.Fallback.Intensity, Is.EqualTo(2f));
            Assert.That(environment.Info.Fallback.Cubemap.Id, Is.EqualTo(1u));
        });
    }

    [Test]
    public void TheHighestPriorityUnboundedLightWins()
    {
        var low = TestEnvironments.Unbounded(1, Sky);
        var high = TestEnvironments.Unbounded(2, Room, 5);

        var environment = EnvironmentSelector.Select([low, high], Vector3.Zero);

        Assert.That(environment.FallbackCubemap, Is.EqualTo(high.Map.Cubemap));
    }

    [Test]
    public void AViewInsideABoxUsesItOverTheSky()
    {
        var sky = TestEnvironments.Unbounded(1, Sky);
        var room = TestEnvironments.Box(2, Room, RoomCentre, 4f);

        var environment = EnvironmentSelector.Select([sky, room], RoomCentre);

        Assert.Multiple(() =>
        {
            Assert.That(environment.VolumeCubemap, Is.EqualTo(room.Map.Cubemap));
            Assert.That(environment.FallbackCubemap, Is.EqualTo(sky.Map.Cubemap));
            Assert.That(environment.Info.VolumeWeight, Is.EqualTo(1f));
            Assert.That(environment.Info.Irradiance.L00, Is.EqualTo(Sky), "surfaces find the box per pixel");
        });
    }

    [Test]
    public void AViewOutsideEveryBoxUsesOnlyTheSky()
    {
        var sky = TestEnvironments.Unbounded(1, Sky);
        var room = TestEnvironments.Box(2, Room, RoomCentre, 4f);

        var environment = EnvironmentSelector.Select([sky, room], Vector3.Zero);

        Assert.Multiple(() =>
        {
            Assert.That(environment.VolumeCubemap.Id, Is.Zero);
            Assert.That(environment.Info.Volume.Cubemap.Id, Is.EqualTo(0u));
            Assert.That(environment.Info.VolumeWeight, Is.EqualTo(0f));
            Assert.That(environment.Info.Irradiance.L00, Is.EqualTo(Sky));
        });
    }

    [Test]
    public void AViewInTheBorderBlendsTheBackdropBetweenTheBoxAndTheSky()
    {
        var sky = TestEnvironments.Unbounded(1, Sky);
        var room = TestEnvironments.Box(2, Room, RoomCentre, 4f, 2f);

        var environment = EnvironmentSelector.Select([sky, room], RoomCentre + new Vector3(3f, 0f, 0f));

        Assert.Multiple(() =>
        {
            Assert.That(environment.Info.VolumeWeight, Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(environment.Info.Volume.Cubemap.Id, Is.EqualTo(2u));
            Assert.That(environment.Info.Fallback.Cubemap.Id, Is.EqualTo(1u));
        });
    }

    [Test]
    public void ABoxWithNoSkyStillSuppliesTheReflectionTable()
    {
        var room = TestEnvironments.Box(2, Room, RoomCentre, 4f, 2f);

        var environment = EnvironmentSelector.Select([room], RoomCentre + new Vector3(3f, 0f, 0f));

        Assert.Multiple(() =>
        {
            Assert.That(environment.FallbackCubemap.Id, Is.Zero);
            Assert.That(environment.Info.Irradiance.L00, Is.EqualTo(Vector3.Zero));
            Assert.That(environment.ReflectionLut, Is.EqualTo(TestEnvironments.ReflectionLutHandle));
        });
    }

    [Test]
    public void ABoxTheViewIsOutsideStillSuppliesTheReflectionTable()
    {
        var room = TestEnvironments.Box(2, Room, RoomCentre, 4f);

        var environment = EnvironmentSelector.Select([room], Vector3.Zero);

        Assert.Multiple(() =>
        {
            Assert.That(environment.HasCubemap, Is.False);
            Assert.That(environment.Info.ReflectionLut.Id, Is.EqualTo(TestEnvironments.ReflectionLutHandle.Id));
        });
    }

    [Test]
    public void TheHigherPriorityBoxWinsAnOverlap()
    {
        var outer = TestEnvironments.Box(1, Sky, RoomCentre, 8f);
        var inner = TestEnvironments.Box(2, Room, RoomCentre, 2f, 0f, 1);

        var environment = EnvironmentSelector.Select([inner, outer], RoomCentre);

        Assert.That(environment.VolumeCubemap, Is.EqualTo(inner.Map.Cubemap));
    }

    [Test]
    public void BoxesOfEqualPriorityGoToTheOneTheViewIsDeeperInside()
    {
        var shallow = TestEnvironments.Box(1, Sky, RoomCentre + new Vector3(3f, 0f, 0f), 4f, 4f);
        var deep = TestEnvironments.Box(2, Room, RoomCentre, 4f, 4f);

        var environment = EnvironmentSelector.Select([shallow, deep], RoomCentre);

        Assert.That(environment.VolumeCubemap, Is.EqualTo(deep.Map.Cubemap));
    }
}
