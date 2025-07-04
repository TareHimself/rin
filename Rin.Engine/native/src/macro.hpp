#ifndef RIN_MACROS
#define RIN_MACROS

#if defined(_MSC_VER)
    //  Microsoft
    #ifdef RIN_NATIVE_PRODUCER
        #define RIN_NATIVE_API extern "C" __declspec(dllexport)
    #else
        #define RIN_NATIVE_API extern "C" __declspec(dllimport)
    #endif
#elif defined(__GNUC__)
        //  GCC
        #define RIN_NATIVE_API extern "C" __attribute__((visibility("default")))
#else
        //  do nothing and hope for the best?
        #define RIN_NATIVE_API
        #pragma warning Unknown dynamic link RIN_NATIVE_API semantics.
#endif

#ifdef _WIN32
    #define RIN_PLATFORM_WIN
    #define RIN_CALLBACK_CONVENTION __stdcall
#endif

#ifdef __APPLE__
    #define RIN_PLATFORM_MAC
    #define RIN_CALLBACK_CONVENTION
#endif

#ifdef __linux__
    #define RIN_PLATFORM_LINUX
    #define RIN_CALLBACK_CONVENTION
#endif

#endif
