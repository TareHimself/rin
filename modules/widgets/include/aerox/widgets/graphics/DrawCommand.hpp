#pragma once
#include "SurfaceFrame.hpp"
#include "aerox/core/Disposable.hpp"

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