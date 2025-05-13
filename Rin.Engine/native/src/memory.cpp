#include "memory.hpp"
#include <memory>
EXPORT_IMPL void* memoryAllocate(size_t size)
{
    return new char[size];
}
EXPORT_IMPL void memorySet(void* ptr, int value, size_t size)
{
    std::memset(ptr, value, size);
}
EXPORT_IMPL void memoryFree(void* ptr)
{
    
    delete static_cast<char*>(ptr);
}
