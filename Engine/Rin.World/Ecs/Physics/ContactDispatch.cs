using Rin.World.Ecs.Scheduling;
using Rin.World.Physics;

namespace Rin.World.Ecs.Physics;

/// <summary>
///     Maps the step's <see cref="IPhysicsSystem.ContactEvents" /> to entities, in the physics system's order. A contact
///     whose bodies both have no entity is dropped.
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
    }
}
