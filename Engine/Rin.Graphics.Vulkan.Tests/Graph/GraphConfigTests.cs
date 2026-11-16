using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Graphics.Vulkan.Graph;

namespace Rin.Graphics.Vulkan.Tests.Graph;

public class GraphConfigTests
{
    private static readonly Extent2D Extent = new(4, 4);
    private GraphConfig _config = null!;

    [SetUp]
    public void CreateConfig()
    {
        var builder = new GraphBuilder(null!, null!, new UploadLanes(), null!, null!);
        _config = new GraphConfig(builder) { CurrentPassId = 100 };
    }

    private uint CreateTexture()
    {
        return _config.CreateTexture(Extent, ImageFormat.R8, ImageLayout.ColorAttachment);
    }

    [Test]
    public void PrependTextureWritePutsADiscardingWriteAheadOfTheOtherActions()
    {
        var id = CreateTexture();

        _config.PrependTextureWrite(id, true);

        var actions = _config.ResourceActions[id];
        Assert.Multiple(() =>
        {
            Assert.That(actions, Has.Count.EqualTo(2));
            Assert.That(actions[0].ImageLayout, Is.EqualTo(ImageLayout.TransferDst));
            Assert.That(actions[0].Discard, Is.True);
            Assert.That(actions[1].Discard, Is.False);
        });
    }

    [Test]
    public void PrependedActionsKeepTheOrderTheyWereAddedIn()
    {
        var id = CreateTexture();

        _config.PrependTextureWrite(id, false);
        _config.PrependTextureRead(id);

        var actions = _config.ResourceActions[id];
        Assert.Multiple(() =>
        {
            Assert.That(actions[0].Operation, Is.EqualTo(ResourceOperation.Write));
            Assert.That(actions[1].Operation, Is.EqualTo(ResourceOperation.Read));
            Assert.That(actions[2].ImageLayout, Is.EqualTo(ImageLayout.ColorAttachment));
        });
    }

    [Test]
    public void UseTextureAddsToTheUsageOfAnImageTheConfigCreated()
    {
        var id = CreateTexture();
        _config.UseTexture(id, ImageLayout.ShaderReadOnly, ResourceOperation.Read);

        _config.FillResources();

        var descriptor = (TextureResourceDescriptor)_config.Resources[id];
        Assert.That(descriptor.Usage & ImageCreateFlags.Sampled, Is.EqualTo(ImageCreateFlags.Sampled));
    }

    [Test]
    public void ResetForgetsThePreviousFrame()
    {
        var id = CreateTexture();
        _config.PrependTextureWrite(id, true);
        _config.FillResources();

        _config.Reset();
        _config.CurrentPassId = 200;
        var next = CreateTexture();

        Assert.Multiple(() =>
        {
            Assert.That(_config.ResourceActions.Keys, Is.EqualTo(new[] { next }));
            Assert.That(_config.ResourceActions[next], Has.Count.EqualTo(1));
            Assert.That(_config.ResourceActions[next][0].PassId, Is.EqualTo(200));
            Assert.That(_config.PassDependencies.Keys, Is.EqualTo(new uint[] { 200 }));
            Assert.That(_config.Resources, Is.Empty);
        });
    }
}
