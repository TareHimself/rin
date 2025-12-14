#pragma once
#include "flags.hpp"
#include "macro.hpp"

enum class EPlatform
{
    Windows,
    Linux,
    Mac
};

RIN_NATIVE_API int platformGet();

RIN_NATIVE_API void platformInit();

RIN_NATIVE_API void platformShutdown();

using PathReceivedCallback = void(RIN_CALLBACK_CONVENTION *)(const char* text);

RIN_NATIVE_API void platformSelectFile(const char * title,bool multiple, const char * filter,PathReceivedCallback callback);

RIN_NATIVE_API void platformSelectPath(const char * title,bool multiple,PathReceivedCallback callback);
