#pragma once
#include "rin/core/memory.h"
#include "rin/rhi/IDeviceBuffer.h"

namespace rin::gui
{
    class SurfaceFrame;
    class IBatch;
    class IBatcher
    {
    public:
        virtual ~IBatcher() = default;

        virtual void Run(SurfaceFrame * frame,IBatch * batch, const Shared<rhi::IDeviceBuffer>& view) = 0;
        virtual IBatch * CreateBatch() = 0;
        virtual void FreeBatch(IBatch * batch) = 0;
    };
}
