using Rin.Core.Shared;

namespace Rin.Core.Tests.Shared;

public class IdFactoryTests
{
    [Test]
    public void NewIdsAreSequentialWhenNoneHaveBeenFreed()
    {
        var factory = new IdFactory();

        Assert.That(factory.NewId(), Is.EqualTo(0));
        Assert.That(factory.NewId(), Is.EqualTo(1));
        Assert.That(factory.NewId(), Is.EqualTo(2));
    }

    [Test]
    public void FreedIdIsHandedBackOutBeforeAllocatingANewOne()
    {
        var factory = new IdFactory();
        _ = factory.NewId(); // 0
        var one = factory.NewId(); // 1
        _ = factory.NewId(); // 2

        factory.FreeId(one);

        Assert.That(factory.NewId(), Is.EqualTo(one), "a freed id should be reused before minting a new one");
        Assert.That(factory.NewId(), Is.EqualTo(3), "once the free list is empty, allocation should resume from CurrentId");
    }

    [Test]
    public void NewIdOutParamReportsWhetherTheIdWasFreshlyMinted()
    {
        var factory = new IdFactory();
        var fresh = factory.NewId(out var isNewFresh);
        factory.FreeId(fresh);
        var reused = factory.NewId(out var isNewReused);

        Assert.That(isNewFresh, Is.True);
        Assert.That(isNewReused, Is.False);
        Assert.That(reused, Is.EqualTo(fresh));
    }

    [Test]
    public void IsFreeIsTrueForIdsNeverAllocatedAndForExplicitlyFreedIds()
    {
        var factory = new IdFactory();
        var allocated = factory.NewId();

        Assert.That(factory.IsFree(allocated), Is.False, "a currently-held id should not report as free");
        Assert.That(factory.IsFree(allocated + 100), Is.True, "an id beyond CurrentId has never been handed out");

        factory.FreeId(allocated);
        Assert.That(factory.IsFree(allocated), Is.True);
    }
}
