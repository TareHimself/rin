#pragma once
#include "DrawCommand.hpp"

namespace aerox::widgets
{
    class CustomDrawCommand : public DrawCommand
    {
    public:
        Type GetType() const override;
        virtual void Draw(SurfaceFrame * frame) = 0;
    };
}
