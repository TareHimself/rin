#include "memory.hpp"

EXPORT_IMPL void* memoryAllocate(size_t size)
{
    return new char[size];
}
EXPORT_IMPL void memoryFree(void* ptr)
{
    delete static_cast<char*>(ptr);
}
