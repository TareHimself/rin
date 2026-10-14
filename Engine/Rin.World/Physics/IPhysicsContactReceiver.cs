namespace Rin.World.Physics;

/// <summary>
///     Implemented by a physics owner (see <see cref="World.RegisterPhysicsBody" />) that wants the contact events of
///     its body. <paramref name="other" /> is null when the other body has no registered owner or was already destroyed.
/// </summary>
public interface IPhysicsContactReceiver
{
    void OnPhysicsContact(in PhysicsContactEvent contact, Components.IWorldComponent? other);
}
