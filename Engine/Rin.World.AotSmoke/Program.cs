using System.Numerics;
using Rin.World;
using Rin.World.Transforms;

var world = new World();
var snapshot = new WorldSnapshot();

var parent = world.Spawn();
world.Add(parent, new Health(10));
world.SetPosition(parent, new Vector3(1, 0, 0));

var child = world.Spawn();
world.Add(child, new Health(20));
world.AddTag<Poisoned>(child);
world.SetPosition(child, new Vector3(0, 2, 0));
world.SetParent(child, parent);

var queued = world.Commands.Spawn();
world.Commands.Add(queued, new Health(30));
world.Commands.AddTag<Poisoned>(queued);
world.Commands.Playback();

world.Update(world.TickInterval);
Check(world.Get<WorldTransform>(child).Matrix.Translation == new Vector3(1, 2, 0), "the child's world transform follows its parent");

foreach (var chunk in world.Query<Health>())
    for (var i = 0; i < chunk.Length; i++)
        if (world.HasTag<Poisoned>(chunk.Entities[i])) chunk.Components1[i].Value -= 5;

Check(world.Get<Health>(child).Value == 15 && world.Get<Health>(queued).Value == 25, "the query wrote through its span");
Check(world.Get<Health>(parent).Value == 10, "the untagged entity was left alone");

world.SaveSnapshot(snapshot);
world.Despawn(parent);
Check(!world.IsAlive(child) && world.EntityCount == 1, "despawning the parent despawned the child");

world.RestoreSnapshot(snapshot);
Check(world.EntityCount == 3 && world.GetParent(child) == parent, "the restore brought back the hierarchy");
Check(world.Get<Health>(child).Value == 15 && world.HasTag<Poisoned>(child), "the restore brought back components and tags");

Console.WriteLine("Rin.World AOT smoke test passed.");
return 0;

static void Check(bool condition, string what)
{
    if (condition) return;
    Console.Error.WriteLine($"Rin.World AOT smoke test failed: expected {what}.");
    Environment.Exit(1);
}

internal record struct Health(int Value) : IComponentData;

internal struct Poisoned : ITagData;
