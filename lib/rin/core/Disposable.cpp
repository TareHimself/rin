#include "rin/core/Disposable.h"
#include <mutex>

namespace rin
{
    void Disposable::Dispose()
    {
        if(!_disposed)
        {
            _disposed = true;
            OnDispose();
        }
    }
}
