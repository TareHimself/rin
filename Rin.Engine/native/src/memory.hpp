#pragma once
#include "macro.hpp"

EXPORT_DECL void* memoryAllocate(size_t size);

EXPORT_DECL void memoryFree(void* ptr);

