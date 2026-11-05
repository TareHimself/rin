using Rin.World.Scripting;
using Rin.World.Tasks;

namespace Rin.World.Tests.Scripting;

public class ScriptFeatureRestoreTests
{
    private World _world = null!;
    private ScriptFeature _scripts = null!;
    private WorldSnapshot _snapshot = null!;
    private List<string> _log = null!;

    [SetUp]
    public void SetUp()
    {
        _world = new World();
        _scripts = new ScriptFeature(_world);
        _snapshot = new WorldSnapshot();
        _log = [];
    }

    [Test]
    public void AScriptOnARestoredEntityStaysAttachedAndRestartsItsTasksFromComponents()
    {
        var entity = _world.Spawn();
        _world.Add(entity, new Health(0));
        var script = new Counter(_log);
        _scripts.Attach(entity, script);
        Tick(3);
        _world.SaveSnapshot(_snapshot);
        Tick(4);

        _world.RestoreSnapshot(_snapshot);
        Assert.That(_world.Get<Health>(entity).Value, Is.EqualTo(3));
        Tick(2);

        Assert.That(_scripts.GetScript(entity), Is.SameAs(script));
        Assert.That(_scripts.Tasks.RunningCount, Is.EqualTo(1));
        Assert.That(_world.Get<Health>(entity).Value, Is.EqualTo(5));
        Assert.That(_log, Is.EqualTo(new[] { "start", "restore" }));
    }

    [Test]
    public void AScriptWhoseEntityDidNotExistAtTheSaveIsStoppedAndDetached()
    {
        _world.SaveSnapshot(_snapshot);
        var script = new Counter(_log);
        var entity = _world.Spawn();
        _world.Add(entity, new Health(0));
        _scripts.Attach(entity, script);

        _world.RestoreSnapshot(_snapshot);
        Tick(2);

        Assert.That(script.IsAttached, Is.False);
        Assert.That(_scripts.Count, Is.Zero);
        Assert.That(_scripts.Tasks.RunningCount, Is.Zero);
        Assert.That(_log, Is.EqualTo(new[] { "start", "stop" }));
    }

    [Test]
    public void AScriptAttachedAfterTheSaveToAnOlderEntityIsStoppedAndDetached()
    {
        var entity = _world.Spawn();
        _world.Add(entity, new Health(0));
        _world.SaveSnapshot(_snapshot);
        var script = new Counter(_log);
        _scripts.Attach(entity, script);

        _world.RestoreSnapshot(_snapshot);

        Assert.That(script.IsAttached, Is.False);
        Assert.That(_world.Has<ScriptRef>(entity), Is.False);
        Assert.That(_log, Is.EqualTo(new[] { "start", "stop" }));
    }

    [Test]
    public void AnEntityWhoseScriptWasDetachedSinceTheSaveComesBackWithoutAScript()
    {
        var entity = _world.Spawn();
        _world.Add(entity, new Health(0));
        _scripts.Attach(entity, new Counter(_log));
        _world.SaveSnapshot(_snapshot);
        _scripts.Detach(entity);

        _world.RestoreSnapshot(_snapshot);

        Assert.That(_world.Has<ScriptRef>(entity), Is.False);
        Assert.That(_scripts.GetScript(entity), Is.Null);
        Assert.That(_scripts.Count, Is.Zero);
    }

    [Test]
    public void ScriptSlotsAreNotPartOfTheSnapshot()
    {
        var first = _world.Spawn();
        var second = _world.Spawn();
        _scripts.Attach(first, new Counter(_log));
        _scripts.Attach(second, new Counter(_log));
        _world.SaveSnapshot(_snapshot);
        _scripts.Detach(first);
        _scripts.Detach(second);
        _scripts.Attach(first, new Counter(_log));
        _scripts.Attach(second, new Counter(_log));
        var swapped = new WorldSnapshot();

        _world.SaveSnapshot(swapped);

        Assert.That(swapped.Bytes.ToArray(), Is.EqualTo(_snapshot.Bytes.ToArray()));
    }

    private void Tick(int count)
    {
        for (var i = 0; i < count; i++) _world.Update(_world.TickInterval);
    }

    // The count lives in Health, so Restore only has to start the loop again.
    private sealed class Counter(List<string> log) : Script
    {
        protected override void Start()
        {
            log.Add("start");
            Tasks.Run(Count);
        }

        protected override void Restore()
        {
            log.Add("restore");
            Tasks.Run(Count);
        }

        protected override void Stop() => log.Add("stop");

        private async GameTask Count(TaskContext ctx)
        {
            while (true)
            {
                await ctx.NextTick();
                if (World.Has<Health>(Entity)) World.Get<Health>(Entity).Value++;
            }
        }
    }
}
