using Rin.Core.Graphics;

namespace Rin.Graphics.Vulkan.Images;

public class BindlessResource : IBindlessResource
{
    public ResourceHandle Handle { get; set; }
    public BindlessResourceState State { get; set; }

    /// <summary>
    ///     Bumped every time this slot's occupant changes (create or free). Stamped onto every
    ///     <see cref="ResourceHandle" /> handed out for this slot, so a handle captured before a free -
    ///     and possibly a later reuse of the same slot id - can be told apart from the slot's current
    ///     occupant without needing to hold a reference to the original object.
    /// </summary>
    public uint Generation { get; set; }

    /// <summary>
    ///     The owner's reference plus one per graph currently using this resource; it's destroyed at zero.
    /// </summary>
    public int References { get; set; } = 1;

    /// <summary>
    ///     Set once the owner frees it - no new references can be taken after that.
    /// </summary>
    public bool Retired { get; set; }
}