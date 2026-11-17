using System.Numerics;
using Rin.Core.Graphics;
using Rin.Graphics.Null;
using Rin.World.Graphics.ImageBasedLighting;

namespace Rin.World.Tests.Graphics.ImageBasedLighting;

public class EnvironmentMapsTests
{
    private static readonly Vector3 Sky = new(0.25f, 0.5f, 1f);

    private NullGraphicsModule _graphics = null!;
    private EnvironmentMaps _maps = null!;

    [SetUp]
    public void SetUp()
    {
        _graphics = new NullGraphicsModule();
        _maps = new EnvironmentMaps();
    }

    [TearDown]
    public void TearDown()
    {
        _maps.Dispose();
    }

    [Test]
    public void CreateMakesTheCubemapAndPanoramaAndKeepsTheIrradiance()
    {
        var map = TestEnvironments.Map(Sky);

        var uploaded = _maps.Get(_maps.Create(map, _graphics))!;

        Assert.Multiple(() =>
        {
            Assert.That(uploaded.Cubemap.Type, Is.EqualTo(ResourceType.Cubemap));
            Assert.That(_graphics.GetExtent(uploaded.Cubemap), Is.EqualTo(new Extent2D(16u, 16u)));
            Assert.That(_graphics.GetFormat(uploaded.Cubemap), Is.EqualTo(ImageFormat.RGBA16));
            Assert.That(uploaded.FaceSize, Is.EqualTo(16u));
            Assert.That(uploaded.MipCount, Is.EqualTo((uint)map.MipCount));
            Assert.That(_graphics.GetExtent(uploaded.Panorama), Is.EqualTo(map.PanoramaExtent));
            Assert.That(_graphics.GetFormat(uploaded.Panorama), Is.EqualTo(ImageFormat.RGBA16));
            Assert.That(uploaded.IsFiltered, Is.False, "a render pass fills the cubemap later");
            Assert.That(uploaded.Irradiance[0], Is.EqualTo(map.Irradiance[0]));
        });
    }

    [Test]
    public void TheDefaultHandleIsNoMap()
    {
        Assert.Multiple(() =>
        {
            Assert.That(default(EnvironmentMapHandle).IsValid, Is.False);
            Assert.That(_maps.Get(default), Is.Null);
        });
    }

    [Test]
    public void AFreedHandleGoesStaleEvenAfterItsSlotIsReused()
    {
        var first = _maps.Create(TestEnvironments.Map(Sky), _graphics);
        _maps.Free(first);

        var second = _maps.Create(TestEnvironments.Map(Sky), _graphics);

        Assert.Multiple(() =>
        {
            Assert.That(_maps.Get(first), Is.Null);
            Assert.That(_maps.Get(second), Is.Not.Null);
            Assert.That(second, Is.Not.EqualTo(first));
        });
    }

    [Test]
    public void DisposingFreesEveryMapAndTheSharedTable()
    {
        var handle = _maps.Create(TestEnvironments.Map(Sky), _graphics);
        var uploaded = _maps.Get(handle)!;

        _maps.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(_maps.Get(handle), Is.Null);
            Assert.That(_graphics.IsValidResourceHandle(uploaded.Cubemap), Is.False);
            Assert.That(_graphics.IsValidResourceHandle(uploaded.Panorama), Is.False);
            Assert.That(_graphics.IsValidResourceHandle(uploaded.ReflectionLut), Is.False);
        });
    }

    [Test]
    public void DisposingAnEmptyStoreDoesNothing()
    {
        Assert.DoesNotThrow(() => new EnvironmentMaps().Dispose());
    }

    [Test]
    public void DescribingALightCopiesItsSettingsAndItsMap()
    {
        var handle = _maps.Create(TestEnvironments.Map(Sky), _graphics);
        var light = new EnvironmentLight
        {
            Map = handle,
            Intensity = 3f,
            Extent = new Vector3(1f, 2f, 3f),
            BlendDistance = 0.5f,
            Priority = 7
        };
        var transform = Matrix4x4.CreateTranslation(4f, 5f, 6f);

        var described = _maps.TryDescribe(light, transform, out var volume);

        Assert.Multiple(() =>
        {
            Assert.That(described, Is.True);
            Assert.That(volume.Map, Is.SameAs(_maps.Get(handle)));
            Assert.That(volume.Intensity, Is.EqualTo(3f));
            Assert.That(volume.Extent, Is.EqualTo(new Vector3(1f, 2f, 3f)));
            Assert.That(volume.BlendDistance, Is.EqualTo(0.5f));
            Assert.That(volume.Priority, Is.EqualTo(7));
            Assert.That(volume.Transform, Is.EqualTo(transform));
        });
    }

    [Test]
    public void ALightWithNoLiveMapCannotBeDescribed()
    {
        Assert.That(_maps.TryDescribe(new EnvironmentLight(), Matrix4x4.Identity, out _), Is.False);
    }
}
