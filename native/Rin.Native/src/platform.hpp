#pragma once
#include "flags.hpp"
#include "macro.hpp"
#include "result.hpp"

enum class EPlatform
{
    Windows,
    Linux,
    Mac
};

RIN_NATIVE_API CoreResult platformInit();

RIN_NATIVE_API void platformShutdown();

using PathReceivedCallback = void(RIN_CALLBACK_CONVENTION *)(const char* text,void*userData);

RIN_NATIVE_API CoreResult platformSelectFile(const char * title,bool multiple, const char * filter,PathReceivedCallback callback,void*userData);

RIN_NATIVE_API CoreResult platformSelectPath(const char * title,bool multiple,PathReceivedCallback callback,void*userData);
