#pragma once
#include <atomic>

namespace rin
{
    
    class Disposable
    {
        std::atomic<bool> _disposed = false;
    protected:
        virtual void OnDispose() = 0;
    public:
        virtual ~Disposable() = default;

        void Dispose();
    };
}