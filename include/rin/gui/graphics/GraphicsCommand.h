#pragma once
#include <cstdint>

#include "Command.h"
#include "CommandType.h"
#include "SurfaceFrame.h"
#include "rin/core/memory.h"
#include "rin/rhi/IDeviceBuffer.h"

namespace rin::gui
{
    class GraphicsCommand : public Command
    {
    public:
        virtual bool RequiresStencil() const;
    };
}
