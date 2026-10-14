#pragma once
#include <cstdint>

struct Extent2D
{
    std::uint32_t width;
    std::uint32_t height;
};

#include "macro.hpp"

using AudioCallback = void(RIN_CALLBACK_CONVENTION *)(float* data,int count,double time,void*userData);
using SourceReadCallback = void(RIN_CALLBACK_CONVENTION *)(std::uint64_t pos,std::uint64_t size, unsigned char *data,void*userData);
using SourceAvailableCallback = std::uint64_t(RIN_CALLBACK_CONVENTION *)(void*userData);
using SourceLengthCallback = std::uint64_t(RIN_CALLBACK_CONVENTION *)(void*userData);
RIN_NATIVE_API void * videoContextCreate();
RIN_NATIVE_API int videoContextHasVideo(void *context);
RIN_NATIVE_API Extent2D videoContextGetVideoExtent(void *context);
RIN_NATIVE_API void videoContextSeek(void *context, double time);
RIN_NATIVE_API int videoContextHasAudio(void *context);
RIN_NATIVE_API void videoContextSetAudioCallback(void *context,AudioCallback audioCallback,void*userData);
RIN_NATIVE_API int videoContextGetAudioSampleRate(void *context);
RIN_NATIVE_API int videoContextGetAudioChannels(void *context);
RIN_NATIVE_API int videoContextGetAudioTrackCount(void *context);
RIN_NATIVE_API void videoContextSetAudioTrack(void *context,int track);
RIN_NATIVE_API double videoContextGetDuration(void *context);
RIN_NATIVE_API double videoContextGetPosition(void *context);
// Returns 1 when there is more to decode right now, 0 at the end of the stream or when the source needs more data.
RIN_NATIVE_API int videoContextDecode(void *context, double delta);
RIN_NATIVE_API int videoContextEnded(void *context);
RIN_NATIVE_API int videoContextGetBufferedFrameCount(void *context);
RIN_NATIVE_API void videoContextDropFramesBefore(void *context, double timestamp);
// Writes the RGBA frame for `timestamp` and returns 1, or returns 0 when it is the frame the last call wrote.
RIN_NATIVE_API int videoContextCopyRecentFrame(void *context, double timestamp, std::uint8_t *destination, std::uint64_t size);
RIN_NATIVE_API void videoContextSetSource(void *context,void * source);
RIN_NATIVE_API void videoContextFree(void* context);
RIN_NATIVE_API void * videoSourceCreate(SourceReadCallback readCallback,SourceAvailableCallback availableCallback,SourceLengthCallback lengthCallback,void*userData);
RIN_NATIVE_API void videoSourceFree(void * source);
