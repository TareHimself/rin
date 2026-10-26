#include "memory.hpp"

#include <cstdlib>
#include <cstring>
#include <new>
void* memoryAllocate(size_t size)
{
    return new(std::nothrow) char[size];
}
void memorySet(void* ptr, int value, size_t size)
{
    std::memset(ptr, value, size);
}

void memoryFree(void* ptr)
{
    delete[] static_cast<char*>(ptr);
}
