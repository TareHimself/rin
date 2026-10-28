using Rin.Core.Graphics;
using Rin.Graphics.Vulkan.Images;

namespace Rin.Graphics.Vulkan.Tests.Images;

public class ResourceTableTests
{
    private ResourceTable<BindlessBuffer> _table = null!;

    [SetUp]
    public void CreateTable()
    {
        _table = new ResourceTable<BindlessBuffer>(ResourceType.Buffer, 16);
    }

    private ResourceHandle Add(BindlessResourceState state = BindlessResourceState.Ready)
    {
        var (id, generation) = _table.Reserve();
        var handle = new ResourceHandle(ResourceType.Buffer, id, generation: generation);
        _table.Set(id, new BindlessBuffer { Handle = handle, Generation = generation, State = state });
        return handle;
    }

    [Test]
    public void SlotZero_IsNeverHandedOut()
    {
        var first = Add();

        Assert.Multiple(() =>
        {
            Assert.That(first.Id, Is.EqualTo(1));
            Assert.That(first.Generation, Is.Zero);
            Assert.That(_table.Find(new ResourceHandle(ResourceType.Buffer, 0)), Is.Null);
            Assert.That(_table.Count, Is.EqualTo(2));
        });
    }

    [Test]
    public void Resolve_ReturnsOnlyReadyOccupants()
    {
        var ready = Add();
        var pending = Add(BindlessResourceState.PendingBind);

        Assert.Multiple(() =>
        {
            Assert.That(_table.Resolve(ready), Is.Not.Null);
            Assert.That(_table.Resolve(pending), Is.Null);
            Assert.That(_table.Find(pending), Is.Not.Null);
        });
    }

    [Test]
    public void Find_RejectsAHandleOfAnotherType()
    {
        var handle = Add();
        var texture = new ResourceHandle(ResourceType.Texture, handle.Id, generation: handle.Generation);

        Assert.That(_table.Find(texture), Is.Null);
    }

    [Test]
    public void Free_ReturnsTheOccupantOnce_AndTheHandleNeverResolvesAgain()
    {
        var handle = Add();

        var freed = _table.Free(handle);

        Assert.Multiple(() =>
        {
            Assert.That(freed?.Handle, Is.EqualTo(handle));
            Assert.That(_table.Free(handle), Is.Null);
            Assert.That(_table.Find(handle), Is.Null);
            Assert.That(_table.Resolve(handle), Is.Null);
            Assert.That(_table.Get(handle.Id)?.Generation, Is.EqualTo(handle.Generation + 1));
        });
    }

    [Test]
    public void ReusedSlot_GetsANewerGenerationThanTheStaleHandle()
    {
        var stale = Add();
        _table.Free(stale);

        var reused = Add();

        Assert.Multiple(() =>
        {
            Assert.That(reused.Id, Is.EqualTo(stale.Id));
            Assert.That(reused.Generation, Is.GreaterThan(stale.Generation));
            Assert.That(_table.Find(stale), Is.Null);
            Assert.That(_table.Resolve(reused), Is.Not.Null);
        });
    }

    [Test]
    public void Reserve_AfterFreeingTwoSlots_HandsThemBackBeforeNewOnes()
    {
        var first = Add();
        var second = Add();
        _table.Free(first);
        _table.Free(second);

        var ids = new[] { Add().Id, Add().Id, Add().Id };

        Assert.That(ids, Is.EqualTo(new uint[] { first.Id, second.Id, 3 }));
    }
}
