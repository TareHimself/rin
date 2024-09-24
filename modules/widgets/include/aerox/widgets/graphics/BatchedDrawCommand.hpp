#pragma once
#include "DrawCommand.hpp"
#include "QuadInfo.hpp"

class BatchedDrawCommand : public DrawCommand
{
public:
    Type GetType() const override;
    virtual std::vector<QuadInfo> ComputeQuads() const = 0;
};
