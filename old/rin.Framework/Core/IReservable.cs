namespace rin.Framework.Core;

/// <summary>
/// An interface for <see cref="IDisposable"/> that supports reservations such that n calls to <see cref="Reserve"/> will require n calls to <see cref="IDisposable.Dispose"/>
/// </summary>
public interface IReservable : IDisposable
{
    public void Reserve();
}