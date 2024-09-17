#pragma once
#include "SurfaceFrame.hpp"
#include "aerox/core/Disposable.hpp"

namespace aerox::widgets
{
    class DrawCommand : public Disposable
    {
    public:
        enum class Type
        {
            Batched,
            Custom
        };


        virtual Type GetType() const = 0;

        
    };
}
