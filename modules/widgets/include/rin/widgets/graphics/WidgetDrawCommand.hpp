#pragma once
#include "SurfaceFrame.hpp"
#include "rin/core/Disposable.hpp"

class WidgetDrawCommand : public Disposable
{
public:
    enum class Type
    {
        Batched,
        Custom
    };


    virtual Type GetType() const = 0;

    virtual bool CombineWith(const Shared<WidgetDrawCommand>& other);
};
