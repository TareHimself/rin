#ifndef RIN_MACROS
#define RIN_MACROS

    #if defined(_MSC_VER)
        //  Microsoft 
        #define EXPORT "C" __declspec(dllexport)
        #define EXPORT_DECL "C" __declspec(dllexport)
        #define EXPORT_IMPL "C" __declspec(dllexport)
    #elif defined(__GNUC__)
        //  GCC
        #define EXPORT extern "C" __attribute__((visibility("default")))
        #define EXPORT_DECL extern "C" __attribute__((visibility("default")))
        #define EXPORT_IMPL
    #else
        //  do nothing and hope for the best?
        #define EXPORT
        #define EXPORT_IMPL
        #pragma warning Unknown dynamic link export semantics.
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