#pragma once
#include <cstdint>
#include "BatchedCommand.h"
#include "rin/core/memory.h"

namespace rin::gui
{
    
    class IBatch
    {
    public:
        virtual ~IBatch() = default;
        virtual uint64_t GetRequiredMemory() const = 0;
        virtual IBatcher * GetBatcher() const = 0;
        virtual void Add(const Shared<BatchedCommand>& command) = 0;
    };
}
