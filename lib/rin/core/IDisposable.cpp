#include "rin/core/IDisposable.h"
#include <mutex>

namespace rin
{
    void IDisposable::Dispose()
    {
        if(!_disposed)
        {
            _disposed = true;
            OnDispose();
        }
    }
}
