#pragma once
#include "macro.hpp"
#include <cstddef>
EXPORT_DECL void* memoryAllocate(size_t size);
EXPORT_DECL void memorySet(void* ptr,int value,size_t size);
EXPORT_DECL void memoryFree(void* ptr);

