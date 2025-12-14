#pragma once
#include "macro.hpp"
#include <cstddef>
RIN_NATIVE_API void* memoryAllocate(size_t size);
RIN_NATIVE_API void memorySet(void* ptr,int value,size_t size);
RIN_NATIVE_API void* memoryReAllocate(void* ptr,size_t size);
RIN_NATIVE_API void memoryFree(void* ptr);

