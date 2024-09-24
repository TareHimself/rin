#pragma once
#include "DrawCommand.hpp"

class CustomDrawCommand : public DrawCommand
{
public:
    Type GetType() const override;
    virtual void Draw(SurfaceFrame * frame) = 0;
};
