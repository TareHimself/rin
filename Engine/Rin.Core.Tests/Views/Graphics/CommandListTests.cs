using System.Numerics;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Commands;

namespace Rin.Core.Tests.Views.Graphics;

public class CommandListTests
{
    [Test]
    public void ResetForgetsCommandsAndClipsIncludingAClipLeftOpen()
    {
        var list = new CommandList { SurfaceSize = new Vector2(800f, 600f) };
        list.PushClip(Matrix4x4.Identity, new Vector2(10f));
        list.Add(new NoOpCommand());

        list.Reset();
        list.Add(new NoOpCommand());

        Assert.Multiple(() =>
        {
            Assert.That(list.Commands, Has.Count.EqualTo(1));
            Assert.That(list.ClipIds.Single().Ids, Is.Empty);
            Assert.That(list.Clips, Is.Empty);
            Assert.That(list.UniqueClipStacks, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void ClipsAfterAResetAreNumberedFromTheStart()
    {
        var list = new CommandList { SurfaceSize = new Vector2(800f, 600f) };
        list.PushClip(Matrix4x4.Identity, new Vector2(10f));
        list.Reset();

        list.PushClip(Matrix4x4.Identity, new Vector2(5f));
        list.Add(new NoOpCommand());

        Assert.That(list.ClipIds.Single().Ids, Is.EqualTo(new uint[] { 0 }));
    }
}
