#pragma once
#include "rin/core/Disposable.hpp"
#include "rin/core/memory.hpp"

class WidgetSurface;

class Event : public Disposable
{
public:
    Shared<WidgetSurface> surface{};

    explicit Event(const Shared<WidgetSurface>& inSurface);
};
