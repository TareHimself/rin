namespace Rin.World.Tests;

public class SimRandomTests
{
    // The seed fills the state with the reference splitmix64.c's first four outputs from 1477776061723855037, as
    // published in rand_xoshiro's tests. The expected values step that state with the reference xoshiro256starstar.c
    // next(), whose output for the state {1, 2, 3, 4} was checked against rand_xoshiro's published values.
    [Test]
    public void MatchesTheReferenceImplementations()
    {
        var random = new SimRandom(1477776061723855037);
        ulong[] expected =
        [
            5157112160024420127, 1500938876607723663, 16125041519135623862, 10010941967492962366,
            14230262757846880097, 12778055244858650149
        ];

        foreach (var value in expected) Assert.That(random.NextUInt64(), Is.EqualTo(value));
    }

    [Test]
    public void TheSameSeedGivesTheSameSequence()
    {
        Assert.That(Draw(new SimRandom(42)), Is.EqualTo(Draw(new SimRandom(42))));
        Assert.That(Draw(new SimRandom(42)), Is.Not.EqualTo(Draw(new SimRandom(43))));
    }

    [Test]
    public void SeedRestartsTheSequence()
    {
        var random = new SimRandom(7);
        var first = Draw(random);

        random.Seed(7);

        Assert.That(Draw(random), Is.EqualTo(first));
    }

    [Test]
    public void ValuesStayInTheirRanges()
    {
        var random = new SimRandom(1);
        for (var i = 0; i < 10_000; i++)
        {
            Assert.That(random.NextInt(7), Is.InRange(0, 6));
            Assert.That(random.NextInt(-3, 3), Is.InRange(-3, 2));
            var value = random.NextFloat();
            Assert.That(value, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
        }
    }

    [Test]
    public void AnEmptyRangeThrows()
    {
        var random = new SimRandom();

        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextInt(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextInt(5, 5));
    }

    private static ulong[] Draw(SimRandom random)
    {
        var values = new ulong[16];
        for (var i = 0; i < values.Length; i++) values[i] = random.NextUInt64();
        return values;
    }
}
