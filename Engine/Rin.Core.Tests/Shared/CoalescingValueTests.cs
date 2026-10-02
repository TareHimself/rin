using Rin.Core.Shared;

namespace Rin.Core.Tests.Shared;

public class CoalescingValueTests
{
    [Test]
    public void InitialStateHasNoPendingChange()
    {
        var latent = new CoalescingValue<uint>(5);

        Assert.That(latent.Current, Is.EqualTo(5u));
        Assert.That(latent.Pending, Is.EqualTo(5u));
        Assert.That(latent.HasPendingChange, Is.False);
    }

    [Test]
    public void SetPublishesAPendingValueWithoutChangingCurrent()
    {
        var latent = new CoalescingValue<uint>(1);

        latent.Set(2);

        Assert.That(latent.Current, Is.EqualTo(1u), "Set should not affect Current until consumed");
        Assert.That(latent.Pending, Is.EqualTo(2u));
        Assert.That(latent.HasPendingChange, Is.True);
    }

    [Test]
    public void TryConsumeAppliesAPendingChangeAndReturnsTrue()
    {
        var latent = new CoalescingValue<uint>(1);
        latent.Set(2);

        var consumed = latent.TryConsume(out var value);

        Assert.That(consumed, Is.True);
        Assert.That(value, Is.EqualTo(2u));
        Assert.That(latent.Current, Is.EqualTo(2u));
        Assert.That(latent.HasPendingChange, Is.False, "the change should no longer be pending once consumed");
    }

    [Test]
    public void TryConsumeReturnsFalseWhenNothingWasPublished()
    {
        var latent = new CoalescingValue<uint>(7);

        var consumed = latent.TryConsume(out var value);

        Assert.That(consumed, Is.False);
        Assert.That(value, Is.EqualTo(7u), "value should still report Current even when nothing was consumed");
        Assert.That(latent.Current, Is.EqualTo(7u));
    }

    [Test]
    public void OnlyTheLatestSetSurvivesToTheNextConsume()
    {
        var latent = new CoalescingValue<uint>(1);

        latent.Set(2);
        latent.Set(3);
        latent.Set(4);

        var consumed = latent.TryConsume(out var value);

        Assert.That(consumed, Is.True);
        Assert.That(value, Is.EqualTo(4u), "only the most recent Set should be observed");
        Assert.That(latent.Current, Is.EqualTo(4u));
    }

    [Test]
    public void SettingTheSameValueAsCurrentIsNotTreatedAsAChange()
    {
        var latent = new CoalescingValue<uint>(5);

        latent.Set(5);

        Assert.That(latent.HasPendingChange, Is.False);
        Assert.That(latent.TryConsume(out var value), Is.False);
        Assert.That(value, Is.EqualTo(5u));
    }

    [Test]
    public void SecondConsumeWithNoNewSetReturnsFalse()
    {
        var latent = new CoalescingValue<uint>(1);
        latent.Set(2);
        latent.TryConsume(out _);

        var consumedAgain = latent.TryConsume(out var value);

        Assert.That(consumedAgain, Is.False);
        Assert.That(value, Is.EqualTo(2u));
    }

    [Test]
    public void WorksWithASmallMultiFieldUnmanagedStruct()
    {
        var latent = new CoalescingValue<Pair>(new Pair(1, 2));

        latent.Set(new Pair(3, 4));
        var consumed = latent.TryConsume(out var value);

        Assert.That(consumed, Is.True);
        Assert.That(value, Is.EqualTo(new Pair(3, 4)));
        Assert.That(latent.Current, Is.EqualTo(new Pair(3, 4)));
    }

    private readonly record struct Pair(int A, int B);
}
