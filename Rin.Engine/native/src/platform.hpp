#pragma once
#include "macro.hpp"

enum EPlatform
{
    Windows,
    Linux,
    Mac
};

EXPORT int platformGet();

EXPORT void platformInit();

using PathReceivedCallback = void(RIN_CALLBACK_CONVENTION *)(const char* text);

EXPORT void platformSelectFile(const char * title,bool multiple, const char * filter,PathReceivedCallback callback);

EXPORT void platformSelectPath(const char * title,bool multiple,PathReceivedCallback callback);

