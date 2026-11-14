namespace Rin.Core.Shared.Pooling;

/// <summary>
///     An object that can be emptied and used again.
/// </summary>
public interface IResettable
{
    /// <summary>
    ///     Empties the object as if it were newly created, but keeps the memory it has grown, so using it again
    ///     does not allocate.
    /// </summary>
    void Reset();
}
