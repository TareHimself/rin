#pragma once
#include <cstdint>

#include "Command.h"
#include "CommandType.h"
#include "rin/core/memory.h"
#include "rin/rhi/IDeviceBuffer.h"

namespace rin::gui
{
    class SurfaceFrame;
    class CustomCommand : public Command
    {
    public:
        virtual void Run(SurfaceFrame * frame,const uint32_t& stencilMask, const Shared<rhi::IDeviceBuffer>& view) = 0;
        virtual uint64_t GetRequiredMemory() const = 0;
        CommandType GetCommandType() const override;
    };
}
