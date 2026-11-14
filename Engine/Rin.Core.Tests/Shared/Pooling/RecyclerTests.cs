using Rin.Core.Shared.Pooling;

namespace Rin.Core.Tests.Shared.Pooling;

public class RecyclerTests
{
    [Test]
    public void TakeCreatesAnObjectWhenThereIsNoSpare()
    {
        var recycler = new Recycler<Item>(() => new Item());

        Assert.That(recycler.Take(), Is.Not.SameAs(recycler.Take()));
    }

    [Test]
    public void AReturnedObjectIsResetAndTakenAgain()
    {
        var recycler = new Recycler<Item>(() => new Item());
        var item = recycler.Take();

        recycler.Return(item);

        Assert.Multiple(() =>
        {
            Assert.That(item.Resets, Is.EqualTo(1));
            Assert.That(recycler.Take(), Is.SameAs(item));
        });
    }

    [Test]
    public void ObjectsBeyondTheSpareLimitAreResetButNotKept()
    {
        var recycler = new Recycler<Item>(() => new Item(), 1);
        var kept = recycler.Take();
        var dropped = recycler.Take();

        recycler.Return(kept);
        recycler.Return(dropped);

        Assert.Multiple(() =>
        {
            Assert.That(dropped.Resets, Is.EqualTo(1));
            Assert.That(recycler.Take(), Is.SameAs(kept));
            Assert.That(recycler.Take(), Is.Not.SameAs(dropped));
        });
    }

    private sealed class Item : IResettable
    {
        public int Resets { get; private set; }

        public void Reset()
        {
            Resets++;
        }
    }
}
