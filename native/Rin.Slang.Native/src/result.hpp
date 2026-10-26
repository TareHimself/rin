#pragma once
#include <cstdint>
#include <exception>
#include <new>

// Mirrored by Native.SlangNativeResult in Slang/Rin.Slang.Compiler/Native.cs. Keep the two in sync.
// The Compile*, EntryPointNotFound and LinkFailed values describe the shader, with the details in the
// diagnostics blob the call filled; the rest describe the wrapper or Slang itself.
enum class SlangNativeResult : std::int32_t
{
    Ok = 0,
    InvalidArgument,
    CompileFailed,
    EntryPointNotFound,
    CompositionFailed,
    LinkFailed,
    SlangError,
    OutOfMemory,
    PlatformError,
};

struct ResultError final : std::exception
{
    SlangNativeResult result;

    explicit ResultError(const SlangNativeResult inResult) : result(inResult)
    {
    }
};

inline void require(const bool condition, const SlangNativeResult onFailure)
{
    if (!condition) throw ResultError(onFailure);
}

// Every fallible export runs its body through this so no exception reaches the managed caller.
template <typename F>
SlangNativeResult guarded(F&& body) noexcept
{
    try
    {
        body();
        return SlangNativeResult::Ok;
    }
    catch (const ResultError& e)
    {
        return e.result;
    }
    catch (const std::bad_alloc&)
    {
        return SlangNativeResult::OutOfMemory;
    }
    catch (...)
    {
        return SlangNativeResult::PlatformError;
    }
}
