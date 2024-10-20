#pragma once
#include "WidgetDrawCommand.hpp"
#include "QuadInfo.hpp"

class WidgetBatchedDrawCommand : public WidgetDrawCommand
{
public:
    Type GetType() const override;
    virtual std::vector<QuadInfo> ComputeQuads() const = 0;
};
