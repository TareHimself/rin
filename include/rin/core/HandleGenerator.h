#pragma once
#include <unordered_set>
#include <mutex>
namespace rin
{
    
    class HandleGenerator
    {
        uint64_t _current = 0;
        std::mutex _mutex;
        std::unordered_set<uint64_t> _freeHandles{};
    public:
        uint64_t CreateHandle();
        void DeleteHandle(uint64_t handle);
    };
}
