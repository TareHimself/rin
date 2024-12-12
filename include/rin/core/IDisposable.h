#pragma once
#include <atomic>

namespace rin
{
    
    class IDisposable
    {
        std::atomic<bool> _disposed = false;
    protected:
        virtual void OnDispose() = 0;
    public:
        virtual ~IDisposable() = default;

        void Dispose();
    };
}