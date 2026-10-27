#pragma once
#include <cstdint>
#include <exception>
#include <new>

// Mirrored by Native.CoreResult in Engine/Rin.Core/Native.cs. Keep the two in sync.
enum class CoreResult : std::int32_t
{
    Ok = 0,
    InvalidArgument,
    OutOfMemory,
    PlatformError,
};

struct ResultError final : std::exception
{
    CoreResult result;

    explicit ResultError(const CoreResult inResult) : result(inResult)
    {
    }
};

inline void require(const bool condition, const CoreResult onFailure)
{
    if (!condition) throw ResultError(onFailure);
}

// Every fallible export runs its body through this so no exception reaches the managed caller.
template <typename F>
CoreResult guarded(F&& body) noexcept
{
    try
    {
        body();
        return CoreResult::Ok;
    }
    catch (const ResultError& e)
    {
        return e.result;
    }
    catch (const std::bad_alloc&)
    {
        return CoreResult::OutOfMemory;
    }
    catch (...)
    {
        return CoreResult::PlatformError;
    }
}
