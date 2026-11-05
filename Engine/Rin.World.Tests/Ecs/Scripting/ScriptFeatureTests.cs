using Rin.World.Ecs;
using Rin.World.Ecs.Scheduling;
using Rin.World.Ecs.Scripting;
using Rin.World.Ecs.Tasks;

namespace Rin.World.Tests.Ecs.Scripting;

public class ScriptFeatureTests
{
    private const float Interval = 0.125f;

    private EntityWorld _world = null!;
    private ScriptFeature _scripts = null!;
    private List<string> _log = null!;

    [SetUp]
    public void SetUp()
    {
        _world = new EntityWorld { TickInterval = Interval };
        _scripts = new ScriptFeature(_world);
        _log = [];
    }

    [Test]
    public void StartRunsOnAttachTickEveryTickAndStopOnDetach()
    {
        var entity = _world.Spawn();
        var script = new Recorder("a", _log);

        _scripts.Attach(entity, script);
        Assert.That(_world.Has<ScriptRef>(entity), Is.True);
        Assert.That(_scripts.GetScript(entity), Is.SameAs(script));
        Tick(2);
        _scripts.Detach(entity);
        Tick(1);

        Assert.That(_log, Is.EqualTo(new[] { "a.start", "a.tick0", "a.tick1", "a.stop" }));
        Assert.That(script.IsAttached, Is.False);
        Assert.That(_scripts.Count, Is.Zero);
    }

    [Test]
    public void ScriptsTickInAttachOrder()
    {
        var entities = new Entity[4];
        for (var i = 0; i < entities.Length; i++) entities[i] = _world.Spawn();
        _scripts.Attach(entities[2], new Recorder("c", _log));
        _scripts.Attach(entities[0], new Recorder("a", _log));
        _scripts.Attach(entities[1], new Recorder("b", _log));
        _scripts.Detach(entities[0]);
        _scripts.Attach(entities[3], new Recorder("d", _log));
        _log.Clear();

        Tick(1);

        Assert.That(_log, Is.EqualTo(new[] { "c.tick0", "b.tick0", "d.tick0" }));
    }

    [Test]
    public void AScriptAttachedDuringATickStartsTickingOnTheNextTick()
    {
        var late = _world.Spawn();
        _scripts.Attach(_world.Spawn(), new Recorder("spawner", _log)
        {
            OnTick = tick =>
            {
                if (tick == 0) _scripts.Attach(late, new Recorder("late", _log));
            }
        });
        _log.Clear();

        Tick(2);

        Assert.That(_log, Is.EqualTo(new[] { "spawner.tick0", "late.start", "spawner.tick1", "late.tick1" }));
    }

    [Test]
    public void ATickDetachingAnotherScriptSkipsIt()
    {
        var victim = _world.Spawn();
        _scripts.Attach(_world.Spawn(), new Recorder("killer", _log) { OnTick = _ => _world.Despawn(victim) });
        _scripts.Attach(victim, new Recorder("victim", _log));
        _log.Clear();

        Tick(2);

        Assert.That(_log, Is.EqualTo(new[] { "killer.tick0", "victim.stop", "killer.tick1" }));
    }

    [Test]
    public void DespawnStopsTheScriptAndCancelsItsTasks()
    {
        var entity = _world.Spawn();
        _scripts.Attach(entity, new Waiter(_log));

        Tick(1);
        _world.Despawn(entity);
        Tick(5);

        Assert.That(_log, Is.EqualTo(new[] { "waited@0", "stop" }));
        Assert.That(_scripts.Tasks.RunningCount, Is.Zero);
    }

    [Test]
    public void DetachCancelsTheScriptsTasksButNotOtherTasksOfTheEntity()
    {
        var entity = _world.Spawn();
        _scripts.Attach(entity, new Waiter(_log));
        _scripts.Tasks.Run(entity, async ctx =>
        {
            await ctx.Ticks(2);
            _log.Add("entity task");
        });

        _scripts.Detach(entity);
        Tick(3);

        Assert.That(_log, Is.EqualTo(new[] { "stop", "entity task" }));
    }

    [Test]
    public void RestoreCancelsEveryTaskThenCallsRestoreInAttachOrder()
    {
        var first = _world.Spawn();
        var second = _world.Spawn();
        _world.Add(first, new Health(2));
        _world.Add(second, new Health(1));
        _scripts.Attach(first, new Countdown(_log));
        _scripts.Attach(second, new Countdown(_log));
        _scripts.Tasks.Run(default, async ctx =>
        {
            await ctx.NextTick();
            _log.Add("unowned");
        });

        _world.Get<Health>(first) = new Health(1);
        _scripts.Restore();
        Tick(3);

        Assert.That(_log, Is.EqualTo(new[] { $"restore {first}", $"restore {second}", $"{first} done@0", $"{second} done@0" }));
    }

    private void Tick(int count)
    {
        for (var i = 0; i < count; i++) _world.Update(Interval);
    }

    private sealed class Recorder(string name, List<string> log) : Script
    {
        public Action<long>? OnTick { get; init; }

        protected override void Start() => log.Add($"{name}.start");

        protected override void Tick(in TickContext context)
        {
            log.Add($"{name}.tick{context.Tick}");
            OnTick?.Invoke(context.Tick);
        }

        protected override void Stop() => log.Add($"{name}.stop");
    }

    private sealed class Waiter(List<string> log) : Script
    {
        protected override void Start() => Tasks.Run(Wait);

        protected override void Stop() => log.Add("stop");

        private async GameTask Wait(TaskContext ctx)
        {
            await ctx.NextTick();
            log.Add($"waited@{World.TickIndex}");
            await ctx.Ticks(2);
            log.Add("never");
        }
    }

    // The count lives in Health so a restore can resume it.
    private sealed class Countdown(List<string> log) : Script
    {
        protected override void Start() => Tasks.Run(CountDown);

        protected override void Restore()
        {
            log.Add($"restore {Entity}");
            Tasks.Run(CountDown);
        }

        private async GameTask CountDown(TaskContext ctx)
        {
            while (World.Get<Health>(Entity).Value > 0)
            {
                await ctx.NextTick();
                World.Get<Health>(Entity).Value--;
            }

            log.Add($"{Entity} done@{World.TickIndex}");
        }
    }
}
