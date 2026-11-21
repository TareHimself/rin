namespace Rin.World.Benchmarks;

internal static class WorldFixture
{
    public static Entity SpawnWithThree(World world, int index)
    {
        var entity = world.Spawn();
        world.Add(entity, new Position { X = index });
        world.Add(entity, new Velocity { X = 1f });
        world.Add(entity, new Health { Value = 100 });
        return entity;
    }

    public static Entity[] Populate(World world, int count)
    {
        var entities = new Entity[count];
        for (var i = 0; i < count; i++) entities[i] = SpawnWithThree(world, i);
        return entities;
    }

    public static Entity[] PopulateFour(World world, int count)
    {
        var entities = new Entity[count];
        for (var i = 0; i < count; i++)
        {
            var entity = SpawnWithThree(world, i);
            world.Add(entity, new Mass { Value = 2f });
            entities[i] = entity;
        }

        return entities;
    }

    public static int[] ShuffledIndices(int count)
    {
        var indices = new int[count];
        for (var i = 0; i < count; i++) indices[i] = i;
        new Random(1234).Shuffle(indices);
        return indices;
    }
}
