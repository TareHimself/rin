#include "rin/core/HandleGenerator.h"

namespace rin
{
    uint64_t HandleGenerator::CreateHandle()
    {
        std::lock_guard g(_mutex);
        if(_freeHandles.empty())
        {
            auto h = _current;
            _current++;
            return h;
        }

        const auto h = *_freeHandles.begin();
        _freeHandles.erase(_freeHandles.begin());
        return h;
    }
    void HandleGenerator::DeleteHandle(uint64_t handle)
    {
        std::lock_guard g(_mutex);
        _freeHandles.emplace(handle);
    }
}
