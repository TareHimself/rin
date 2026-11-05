using Rin.World.Scheduling;

namespace Rin.World.Physics;

/// <summary>
///     Maps the step's <see cref="IPhysicsSystem.ContactEvents" /> to entities and sorts them by entity ids, then kind,
///     because the physics system's own order can change between runs. A contact whose bodies both have no entity is
///     dropped.
/// </summary>
internal sealed class ContactDispatch(IPhysicsSystem physicsSystem, BodyEntityMap bodies) : TickSystem
{
    private EntityContact[] _contacts = new EntityContact[16];
    private int _count;

    public ReadOnlySpan<EntityContact> Contacts => _contacts.AsSpan(0, _count);

    public override void Tick(in TickContext context)
    {
        var events = physicsSystem.ContactEvents;
        if (events.Length > _contacts.Length) _contacts = new EntityContact[int.Max(events.Length, _contacts.Length * 2)];

        _count = 0;
        foreach (ref readonly var contact in events)
        {
            var a = bodies.Find(contact.A);
            var b = bodies.Find(contact.B);
            if (!a.IsValid && !b.IsValid) continue;
            _contacts[_count++] = new EntityContact(a, b, contact.Kind, contact.IsTrigger);
        }

        _contacts.AsSpan(0, _count).Sort(new StableOrder());
    }

    public void Clear() => _count = 0;

    private readonly struct StableOrder : IComparer<EntityContact>
    {
        public int Compare(EntityContact x, EntityContact y)
        {
            var order = Compare(x.A, y.A);
            if (order == 0) order = Compare(x.B, y.B);
            if (order == 0) order = x.Kind.CompareTo(y.Kind);
            return order != 0 ? order : x.IsTrigger.CompareTo(y.IsTrigger);
        }

        private static int Compare(Entity x, Entity y)
        {
            var order = x.Id.CompareTo(y.Id);
            return order != 0 ? order : x.Generation.CompareTo(y.Generation);
        }
    }
}
