#pragma once
#include "macro.hpp"

enum EPlatform
{
    Windows,
    Linux,
    Mac
};

#ifdef _WIN32
#define AEROX_PLATFORM_WIN
#endif

#ifdef __APPLE__
#define AEROX_PLATFORM_MAC
#endif

#ifdef __linux__
#define AEROX_PLATFORM_LINUX
#endif

EXPORT int platformGet();

EXPORT void platformInit();

using PathReceivedCallback = void(__stdcall *)(const char* text);

EXPORT void platformSelectFile(const char * title,bool multiple, const char * filter,PathReceivedCallback callback);

EXPORT void platformSelectPath(const char * title,bool multiple,PathReceivedCallback callback);

