#pragma once
#include "aerox/core/Disposable.hpp"
#include "aerox/core/memory.hpp"

class WidgetSurface;

class Event : public Disposable
{
public:
    Shared<WidgetSurface> surface{};

    explicit Event(const Shared<WidgetSurface>& inSurface);
};
