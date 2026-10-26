#pragma once
#include <cstdint>
#include <exception>
#include <new>

// Mirrored by Native.AudioResult in Engine/Rin.Audio.Miniaudio/Native.cs. Keep the two in sync.
enum class AudioResult : std::int32_t
{
    Ok = 0,
    InvalidArgument,
    InvalidHandle,
    EngineInitFailed,
    ListenerLimitReached,
    MiniaudioError,
    OutOfMemory,
    PlatformError,
};

struct ResultError final : std::exception
{
    AudioResult result;

    explicit ResultError(const AudioResult inResult) : result(inResult)
    {
    }
};

inline void require(const bool condition, const AudioResult onFailure)
{
    if (!condition) throw ResultError(onFailure);
}

// Every fallible export runs its body through this so no exception reaches the managed caller.
template <typename F>
AudioResult guarded(F&& body) noexcept
{
    try
    {
        body();
        return AudioResult::Ok;
    }
    catch (const ResultError& e)
    {
        return e.result;
    }
    catch (const std::bad_alloc&)
    {
        return AudioResult::OutOfMemory;
    }
    catch (...)
    {
        return AudioResult::PlatformError;
    }
}
