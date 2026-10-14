#pragma once
#include <cstdint>

struct Extent2D
{
    std::uint32_t width;
    std::uint32_t height;
};

// Bytes [start, end) of a source. Both zero when nothing is needed.
struct VideoByteRange
{
    std::int64_t start;
    std::int64_t end;
};

// What a demux, seek or header read came back with. Mirrors wdx::DemuxResult, with Error for data the decoder rejects.
enum VideoDemuxResult : int
{
    VIDEO_DEMUX_ERROR = -1,
    VIDEO_DEMUX_INCOMPLETE_HEADER = 0,
    VIDEO_DEMUX_INCOMPLETE_CLUSTER = 1,
    VIDEO_DEMUX_SUCCESS = 2,
    VIDEO_DEMUX_FINISHED = 3
};

#include "macro.hpp"

using AudioCallback = void(RIN_CALLBACK_CONVENTION *)(float* data,int count,double time,void*userData);
using SourceReadCallback = void(RIN_CALLBACK_CONVENTION *)(std::int64_t pos,std::uint64_t size, unsigned char *data,void*userData);
using SourceAvailableCallback = std::int64_t(RIN_CALLBACK_CONVENTION *)(void*userData);
using SourceLengthCallback = std::int64_t(RIN_CALLBACK_CONVENTION *)(void*userData);
using SourceAvailableFromCallback = std::int64_t(RIN_CALLBACK_CONVENTION *)(std::int64_t pos,void*userData);
using SourceMakeRangeAvailableCallback = void(RIN_CALLBACK_CONVENTION *)(std::int64_t pos,std::uint64_t size,void*userData);
using SourceReleaseBeforeCallback = void(RIN_CALLBACK_CONVENTION *)(std::int64_t pos,void*userData);

// The managed side of a video source. Every callback is required, see wdx::ISource for what each one means.
struct VideoSourceCallbacks
{
    SourceReadCallback read;
    SourceAvailableCallback available;
    SourceLengthCallback length;
    SourceAvailableFromCallback availableFrom;
    SourceMakeRangeAvailableCallback makeRangeAvailable;
    SourceReleaseBeforeCallback releaseBefore;
    void* userData;
};

RIN_NATIVE_API void * videoContextCreate();
// Reads the headers if they are not read yet. Tracks, extent and duration are unknown until this returns success.
RIN_NATIVE_API int videoContextEnsureHeader(void *context);
RIN_NATIVE_API int videoContextIsReady(void *context);
RIN_NATIVE_API int videoContextHasVideo(void *context);
RIN_NATIVE_API Extent2D videoContextGetVideoExtent(void *context);
// Changes nothing unless it returns success.
RIN_NATIVE_API int videoContextSeek(void *context, double time);
RIN_NATIVE_API int videoContextHasAudio(void *context);
RIN_NATIVE_API void videoContextSetAudioCallback(void *context,AudioCallback audioCallback,void*userData);
RIN_NATIVE_API int videoContextGetAudioSampleRate(void *context);
RIN_NATIVE_API int videoContextGetAudioChannels(void *context);
RIN_NATIVE_API int videoContextGetAudioTrackCount(void *context);
RIN_NATIVE_API void videoContextSetAudioTrack(void *context,int track);
// Seconds, 0 while unknown as with a live stream.
RIN_NATIVE_API double videoContextGetDuration(void *context);
RIN_NATIVE_API double videoContextGetPosition(void *context);
// Advances by delta seconds. After an incomplete result, call again once the source holds more.
RIN_NATIVE_API int videoContextDecode(void *context, double delta);
// After an incomplete result, the bytes the source is missing for the next step.
RIN_NATIVE_API VideoByteRange videoContextGetNeededRange(void *context);
RIN_NATIVE_API int videoContextEnded(void *context);
RIN_NATIVE_API int videoContextGetBufferedFrameCount(void *context);
RIN_NATIVE_API void videoContextDropFramesBefore(void *context, double timestamp);
// Writes the RGBA frame for `timestamp` and returns 1, or returns 0 when it is the frame the last call wrote.
RIN_NATIVE_API int videoContextCopyRecentFrame(void *context, double timestamp, std::uint8_t *destination, std::uint64_t size);
// Reads nothing, videoContextEnsureHeader and videoContextDecode do.
RIN_NATIVE_API void videoContextSetSource(void *context,void * source);
RIN_NATIVE_API void videoContextFree(void* context);
RIN_NATIVE_API void * videoSourceCreate(const VideoSourceCallbacks* callbacks);
RIN_NATIVE_API void videoSourceFree(void * source);
