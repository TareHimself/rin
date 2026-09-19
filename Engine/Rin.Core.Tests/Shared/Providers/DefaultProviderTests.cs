using Rin.Core.Shared.Providers;

namespace Rin.Core.Tests.Shared.Providers;

public interface IFoo
{
    int Value { get; }
}

public class Foo(int value) : IFoo
{
    public int Value { get; } = value;
}

public class DefaultProviderTests
{
    [Test]
    public void GetThrowsWhenNothingRegistered()
    {
        // VSTest intercepts the Debug.Assert that fires first and rewraps it, not the real NullReferenceException.
        var provider = new DefaultProvider();
        Assert.That(() => provider.Get<IFoo>(), Throws.Exception);
    }

    [Test]
    public void AddSingleWithInstanceReturnsThatExactInstance()
    {
        var provider = new DefaultProvider();
        var instance = new Foo(1);
        provider.AddSingle<IFoo>(instance);

        Assert.That(provider.Get<IFoo>(), Is.SameAs(instance));
    }

    [Test]
    public void AddSingleWithFactoryResolvesLazilyAndCachesTheResult()
    {
        var provider = new DefaultProvider();
        var calls = 0;
        provider.AddSingle<IFoo>(_ =>
        {
            calls++;
            return new Foo(1);
        });

        Assert.That(calls, Is.EqualTo(0), "the factory should not run until the first Get");

        var first = provider.Get<IFoo>();
        var second = provider.Get<IFoo>();

        Assert.That(calls, Is.EqualTo(1), "a singleton factory should only ever run once");
        Assert.That(second, Is.SameAs(first));
    }

    [Test]
    public void AddWithFactoryReturnsAFreshInstanceEveryGet()
    {
        var provider = new DefaultProvider();
        var calls = 0;
        provider.Add<IFoo>(_ =>
        {
            calls++;
            return new Foo(calls);
        });

        var first = provider.Get<IFoo>();
        var second = provider.Get<IFoo>();

        Assert.That(calls, Is.EqualTo(2), "a transient factory should run on every Get, not just the first");
        Assert.That(second, Is.Not.SameAs(first));
    }

    [Test]
    public void InstanceTakesPriorityOverASingleFactoryForTheSameType()
    {
        var provider = new DefaultProvider();
        provider.AddSingle<IFoo>(_ => new Foo(1));
        var instance = new Foo(2);
        provider.AddSingle<IFoo>(instance);

        Assert.That(provider.Get<IFoo>(), Is.SameAs(instance));
    }

    [Test]
    public void ClearSingleDropsTheCachedInstanceButKeepsTheFactory()
    {
        var provider = new DefaultProvider();
        var calls = 0;
        provider.AddSingle<IFoo>(_ =>
        {
            calls++;
            return new Foo(calls);
        });

        var first = provider.Get<IFoo>();
        provider.ClearSingle<IFoo>();
        var second = provider.Get<IFoo>();

        Assert.That(calls, Is.EqualTo(2), "clearing the cached singleton should let the factory run again");
        Assert.That(second, Is.Not.SameAs(first));
    }

    [Test]
    public void RemoveSingleDropsBothTheFactoryAndTheCachedInstance()
    {
        var provider = new DefaultProvider();
        provider.AddSingle<IFoo>(_ => new Foo(1));
        provider.Get<IFoo>();

        provider.RemoveSingle<IFoo>();

        Assert.That(() => provider.Get<IFoo>(), Throws.Exception);
    }
}
