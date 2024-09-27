#pragma once
#include "WidgetDrawCommand.hpp"

class WidgetCustomDrawCommand : public WidgetDrawCommand
{
public:
    Type GetType() const override;
    virtual void Draw(SurfaceFrame * frame) = 0;
};
