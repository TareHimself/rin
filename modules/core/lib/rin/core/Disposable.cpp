#include "rin/core/Disposable.hpp"

void Disposable::OnDispose(bool manual)
{
}

void Disposable::Dispose(bool manual)
{
    if (_disposed) return;
    _disposed = true;
    OnDispose(manual);
}

bool Disposable::IsDisposed() const
{
    return _disposed;
}

Shared<Disposable> Disposable::GetShared()
{
    return this->shared_from_this();
}

Weak<Disposable> Disposable::GetWeak()
{
    return this->weak_from_this();
}
